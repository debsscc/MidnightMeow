# Ataques inimigos com telegraph (estilo Hades)

Última revisão: 2026-05-22

## Objetivo

Dar ao jogador (especialmente **melee**) tempo de reação: a área de dano aparece antes do impacto e **preenche gradualmente** até aplicar dano ou disparar projétil.

## Arquitetura

| Peça | Caminho | Responsabilidade |
|------|---------|------------------|
| `EnemyAttackPatternDefinition` | SO `MidnightMeow/Combat/Enemy Attack Pattern` | Alcance, cooldown, lista de strikes |
| `EnemyTelegraphVisualStyle` | SO `MidnightMeow/Combat/Telegraph Visual Style` | Cores, outline, sorting |
| `EnemyTelegraphedAttacker` | `Components/Enemy/` | IA: quando atacar, executa padrão |
| `EnemyTelegraphZoneFactory` | `Combat/Telegraph/` | Instancia zonas |
| `EnemyTelegraphZoneInstance` | `Combat/Telegraph/` | Timer de fill + dano/projétil |
| `EnemyTelegraphZoneView` | `Combat/Telegraph/` | Material + shader |
| `NetworkEnemyTelegraphRelay` | `Multiplayer/Enemy/` | Encaminha para `NetworkEnemyController` (ClientRpc no prefab) |
| `NetworkEnemyController` | `Multiplayer/Enemy/` | `PlayTelegraphVisualClientRpc` — visual nos clientes |
| `EnemyTelegraphEvents` | `Combat/Telegraph/` | Eventos globais (SFX/VFX) |
| Shader | `Assets/Art/Shaders/TelegraphFill.shader` | Círculo ou retângulo preenchível |

## Configurar um inimigo

1. No prefab do rato, **adicione**:
   - `EnemyTelegraphedAttacker`
   - `EnemyTelegraphZoneFactory`
   - `NetworkEnemyTelegraphRelay` (se já tem `NetworkObject`)
2. Crie um asset **Enemy Attack Pattern** em `Assets/Data/Combat/Patterns/`.
3. Atribua o pattern em `EnemyTelegraphedAttacker.pattern`.
4. Desative ou deixe `disableLegacyAttackComponents` ligado (desliga `EnemyAttack_Ranged` / `Melee` automaticamente).
5. Opcional: `EnemyTelegraphVisualStyle` no pattern ou em `fallbackVisualStyle`.

## Strikes (múltiplas zonas)

Cada entrada em `strikes[]` é independente:

| Campo | Uso |
|-------|-----|
| `delayBeforeStart` | Rajada / destroços em sequência |
| `fillDuration` | Janela de esquiva |
| `shape` + `size` | Círculo (raio = X) ou retângulo (largura × comprimento) |
| `resolution` | `AreaDamage` (dano + `effectPrefab` na zona) ou `ProjectileToZone` (visual do inimigo → zona, dano só na zona) |
| `travelVisualPrefab` / `travelSpeed` | Só em `ProjectileToZone` |
| `effectPrefab` | Só em `AreaDamage` (ex.: pedras caindo, sem projétil) |
| `fillMode` | Retângulo estreito na direção do tiro: `AlongLengthTowardOrigin` |
| `anchorToTargetOnStart` | Círculo sob o jogador |
| `aimAtTarget` | Eixo do retângulo aponta para o alvo |

## Eventos

```csharp
EnemyTelegraphEvents.OnTelegraphStarted += data => { };
EnemyTelegraphEvents.OnTelegraphFillComplete += data => { }; // momento do impacto
EnemyTelegraphEvents.OnTelegraphResolved += data => { };    // após dano/projétil
```

Exemplo: `EnemyTelegraphFeedbackListener` (áudio).

## Multiplayer

- **Servidor:** zona autoritativa (`visualOnly: false`) — aplica dano e spawna projétil NGO.
- **Clientes:** `NetworkEnemyTelegraphRelay` envia snapshot visual (sem dano).
- Projéteis: `NetworkEnemyController` define `ProjectileSpawnDelegate` → `NetworkObject.Spawn`.

## Padrões sugeridos

### Ranged — círculo no jogador

- `ProjectileToZone`: visual sai do `firePoint` em direção à zona; ao chegar, aplica `damage` na área.
- `AreaDamage`: sem projétil; opcional `effectPrefab` na zona (animação/VFX).

### Melee — círculo pequeno no inimigo

- Círculo raio ~1,2, `anchorToTarget` false, offset local, `AreaDamage`, `fillDuration` ~0,5s.

### Boss — vários destroços

- Vários strikes com `delayBeforeStart` escalonado (0, 0,3, 0,6…) e posições `localOffset` aleatórias (via script custom chamando `TriggerPattern`).

## Padrões em Fase-1 (pré-configurados)

| Inimigo | Asset | Comportamento |
|---------|--------|----------------|
| Rato_Padrao_Base | `Data/Combat/Patterns/Rato_Base_RangedCircle` | Círculo no alvo → projétil |
| Rato_Padrao_Veloz | `Rato_Veloz_FastCircle` | Círculo rápido (0,45s) |
| Rato_Padrao_Resistente | `Rato_Resistente_Debris` | 3 círculos em sequência (dano em área) |
| Rato_Acido | `Rato_Acido_Lane` | Faixa retangular → projétil |
| Rato_Eletrico | `Rato_Eletrico_MeleeCircle` | Círculo no inimigo → dano melee |

Instalador: `EnemyTelegraphModuleInstaller` em cada prefab `Rato_*`.

## Visual do shader (`TelegraphFill`)

1. **Borda vermelha** fixa no limite da zona.
2. **Interior amarelo** estático (área ainda não “liberada”).
3. **`_FillAmount` 0→1:** vermelho cresce **do centro para a borda** até a zona ficar toda vermelha.

Cores em `EnemyTelegraphVisualStyle`: `backgroundColor` = amarelo, `fillColor` / `outlineColor` = vermelho.

## Rotação do visual em `ProjectileToZone`

Usa `ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees` (-180°): o PNG do projétil inimigo (`Untitled_Artwork 13`) tem a **cabeça à esquerda**; o jogador usa offset -90° (frente no +Y).

## Legado

Sem `EnemyAttackPatternDefinition`, `EnemyAttack_Ranged` / `EnemyAttack_Melee` continuam funcionando como antes.

## Referências

- [01-data-driven.md](../practices/01-data-driven.md)
- [02-event-driven.md](../practices/02-event-driven.md)
- [09-otimizacao-rede.md](../practices/09-otimizacao-rede.md)
- [Enemy.md](../editor/prefabs/Enemy.md)
