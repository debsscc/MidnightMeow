# Otimização de rede (Unity Netcode + Relay)

Última revisão: 2026-05-22

## Objetivo

Reduzir **bandwidth**, **latência percebida** e custo de infraestrutura no multiplayer do MidnightMeow (NGO + Unity Relay + UTP), sem sacrificar a sensação de jogo de ação 2D.

## Princípio base

> **Nunca envie o que o cliente pode deduzir. Nunca envie para quem não precisa saber.**

---

## 1. Otimização de tráfego (bandwidth)

### Interest Management (gerenciamento de interesse)

Por padrão, o NGO sincroniza **todos** os `NetworkObject` com **todos** os clientes. Em mapas grandes ou com muitos inimigos/projéteis, isso satura a banda.

**O que fazer:**

- Sobrescrever `CheckObjectVisibility` nas classes de interesse (ex.: inimigos distantes, pickups, VFX de rede).
- Se o Jogador A não pode ver nem interagir com o Jogador B, o servidor **não** envia updates do B para o A.
- Usar **Network Proximity** no servidor:
  - Triggers ou distância (ex.: raio da câmera + margem).
  - `NetworkObject.NetworkShow(clientId)` / `NetworkHide(clientId)` dinamicamente.

**Prioridade no MidnightMeow:** inimigos de horda, projéteis longe da câmera, coletáveis já coletados.

### Serialização eficiente

| Evitar | Preferir |
|--------|----------|
| `string` para estados/tipos | `enum`, `byte`, `ushort` |
| `float` para ângulos quando precisão total não importa | `short` / `byte` (compressão de ângulo) |
| RPC com muitos campos reflexivos | `INetworkSerializable` + `FastBufferWriter` / `FastBufferReader` |

**`INetworkSerializable`:** para structs de spawn, hit, wave status, etc. Serialização em nível de bit — mais leve que reflexão padrão do NGO.

Exemplo de direção (pseudo):

```csharp
public struct ProjectileSpawnData : INetworkSerializable
{
    public byte OwnerId;
    public short PosX; // posição quantizada
    public ushort Angle;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref OwnerId);
        serializer.SerializeValue(ref PosX);
        serializer.SerializeValue(ref Angle);
    }
}
```

### Tick rate (taxa de atualização)

O tick rate define quantas vezes por segundo o servidor processa lógica e envia estado.

| Gênero | Tick rate típico |
|--------|------------------|
| Estratégia / card | 10–20/s |
| Ação / survivor / shooter 2D | 30–60/s |

**Não** suba o tick rate só para “resolver lag”. Tick alto sem otimizar bandwidth → perda de pacotes → **mais** lag.

**Projeto:** validar tick em `NetworkManager` / `MultiplayerConfig` e alinhar com `OwnerNetworkTransform` (thresholds de posição/rotação).

---

## 2. NetworkVariable vs RPC — estado vs evento

| Característica | NetworkVariable (estado) | RPC (evento) |
|----------------|--------------------------|--------------|
| Uso ideal | Vida, munição, posição, fase da wave | Tiro, som, explosão, UI pontual |
| Comportamento | Sincroniza contínuo; late joiners recebem valor atual | Dispara uma vez (fire-and-forget) |
| Gargalo comum | Atualizar todo frame sem necessidade | RPC em loop ou payloads enormes |

### Regra de ouro

- **Estado** → `NetworkVariable` (servidor como autoridade).
- **Evento pontual** → `ServerRpc` / `ClientRpc` (com parâmetros mínimos).

### Permissões (anti-cheat)

Use `NetworkVariableReadPermission` e `NetworkVariableWritePermission`:

- Dados críticos (vida, munição, join code): **write no servidor**.
- Leitura `Everyone` só quando UI de todos precisa (ex.: vida de colegas no HUD).

**Já no projeto:**

- `NetworkEnemyController` — `_networkHealth`, `_networkIsDead` (write: Server).
- `LobbySessionManager` — `_joinCode` (write: Server).
- `NetworkProjectileSpawner` — `SyncAmmoToOwnerClientRpc` com `ClientRpcParams` (só para o owner).

### Anti-padrões

```csharp
// Ruim: RPC todo frame para posição
[ServerRpc] void SyncPositionRpc(Vector3 pos) { }

// Bom: NetworkTransform / NetworkVariable com threshold
```

```csharp
// Ruim: ClientRpc broadcast para som local
PlayShootSoundClientRpc(); // em cadência alta

// Melhor: RPC só no spawn; som local predito no cliente que atirou
```

---

## 3. Mascaramento de latência (ilusão de fluidez)

Ping físico sempre existe. O objetivo é **parecer** responsivo.

### Interpolação e extrapolação

- Tick 30 Hz (33 ms) vs render 60 FPS (16 ms) → movimento “engasgado” sem smoothing.
- **Interpolação:** atraso visual de poucos ms e deslize entre snapshots — `NetworkTransform` / `OwnerNetworkTransform` no Player (ver prefab doc).
- **Extrapolação:** em movimento muito rápido, prever próximo passo e corrigir quando o servidor discordar.

**Inspector:** `PositionLerpSmoothing`, `PositionMaxInterpolationTime`, thresholds em `OwnerNetworkTransform`.

### Client-side prediction (previsão no cliente)

1. Jogador aperta Dash → cliente executa **na hora**.
2. Input vai ao servidor.
3. **Server reconciliation:** servidor manda estado oficial; se divergir (parede, stun), cliente corrige (rubberbanding).

**Projeto:** movimento/disparo no `NetworkPlayerController` (owner); validar que input local não espera round-trip para feedback visual.

### Lag compensation (tiros / hitscan)

No cliente o inimigo está onde você vê; no servidor ele já se moveu.

**Técnica:** ao receber tiro, servidor usa ping do atirador, **rebobina** hitboxes ao instante do clique, testa acerto, restaura timeline.

**Projeto:** pipeline de mira em `NetworkPlayerController` / `NetworkProjectileSpawner` — candidato a lag comp no servidor se hit feeling for inconsistente.

---

## 4. Infraestrutura: Unity Relay + UTP

O Relay contorna NAT/firewall roteando por servidores Unity.

### QoS (Quality of Service)

Antes de alocar Relay:

1. Rodar **QoS** do UGS contra regiões (ex.: `sa-east-1`, `us-east-1`).
2. Escolher data center de **menor latência** para o grupo.
3. Passar região ao `RelayManager` / `MultiplayerConfig`.

**Projeto:** `MultiplayerConfig` tem campo de região Relay (`any` = automático). Preferir QoS explícito em builds de produção no Brasil.

### Transporte

- Usar **UnityTransport (UTP)** — UDP otimizado + DTLS com Relay.
- Confirmar em `NetworkManager.prefab`: transport = UTP, não legacy.

**Scripts:** `RelayManager`, `ConnectionManager`, `MultiplayerBootstrapper` em `Assets/Scripts/Multiplayer/Core/`.

---

## Checklist de revisão (PR multiplayer)

- [ ] Novo `NetworkObject` precisa de interest management?
- [ ] Estado contínuo usa `NetworkVariable` com write no servidor?
- [ ] Eventos pontuais usam RPC com payload mínimo (enum/byte)?
- [ ] Nenhum RPC em `Update` / loop de tiro?
- [ ] Structs grandes implementam `INetworkSerializable`?
- [ ] Tick rate justificado e testado com 4 jogadores?
- [ ] Relay: QoS + região documentados em `MultiplayerConfig`?
- [ ] Doc de prefab rede atualizada em `docs/editor/prefabs/`

## Referências no código

| Área | Caminho |
|------|---------|
| Spawn projétil + ammo sync | `Scripts/Multiplayer/Projectile/NetworkProjectileSpawner.cs` |
| Vida inimigo rede | `Scripts/Multiplayer/Enemy/NetworkEnemyController.cs` |
| Ondas | `Scripts/Multiplayer/Wave/NetworkWaveManager.cs` |
| Bootstrap Relay | `Scripts/Multiplayer/Core/MultiplayerBootstrapper.cs` |
| Config | `Scripts/Multiplayer/ScriptableObjects/MultiplayerConfig.cs` |

## Para agentes de IA

Antes de adicionar sync: classifique como **estado** ou **evento**. Se bandwidth for concern, proponha interest management ou quantização. Atualize este doc se introduzir padrão novo (ex.: lag comp no tiro).
