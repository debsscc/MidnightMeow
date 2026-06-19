# Erros e avisos comuns

Última revisão: 2026-06-19

Referência rápida para mensagens frequentes no Console do Unity / compilador C# neste projeto.

---

## Unity Netcode (NGO)

### CS0618 — `ServerRpcAttribute.RequireOwnership` is obsolete

**Mensagem típica:**

```
warning CS0618: 'ServerRpcAttribute.RequireOwnership' is obsolete. Use [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] ...
```

**Causa:** NGO 2.x unificou RPCs no atributo `[Rpc]`; `RequireOwnership` em `[ServerRpc]` foi descontinuado.

**Solução:**

| Antes (obsoleto) | Depois |
|------------------|--------|
| `[ServerRpc(RequireOwnership = false)]` | `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]` |
| `[ServerRpc]` (só owner) | `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` |

**Exemplo no projeto:** `NetworkEnemyController.NotifyDeathPresentationFinishedServerRpc` — qualquer cliente pode avisar o servidor que a apresentação de morte terminou.

---

## Unity Netcode — tipos de coleção

### CS0266 — `NetworkList<T>` não converte para `IReadOnlyList<T>`

**Mensagem típica:**

```
error CS0266: Cannot implicitly convert type 'Unity.Netcode.NetworkList<RatHoleSealSession>' to 'System.Collections.Generic.IReadOnlyList<RatHoleSealSession>'
```

**Causa:** `NetworkList<T>` não implementa `IReadOnlyList<T>`; expor a lista de rede com esse tipo quebra a compilação.

**Solução:** expor o tipo concreto ou iterar sem converter:

```csharp
// Correto — propriedade pública
public NetworkList<RatHoleSealSession> Sessions => _sessions;

// Consumo (foreach funciona em NetworkList)
foreach (RatHoleSealSession session in manager.Sessions) { ... }
```

**Exemplo no projeto:** `NetworkRatHoleSealManager.Sessions`.

---

## TextMesh Pro

### CS0618 — `TMP_Text.enableWordWrapping` is obsolete

**Mensagem típica:**

```
warning CS0618: 'TMP_Text.enableWordWrapping' is obsolete. Please use the textWrappingMode property instead.
```

**Causa:** TMP passou a usar `textWrappingMode` (enum) em vez do bool legado.

**Solução:**

| Antes | Depois |
|-------|--------|
| `tmp.enableWordWrapping = true` | `tmp.textWrappingMode = TextWrappingModes.Normal` |
| `tmp.enableWordWrapping = false` | `tmp.textWrappingMode = TextWrappingModes.NoWrap` |

**Exemplo no projeto:** `CreditsOverlayController` (corpo dos créditos).

---

## C# / Unity — vetores

### CS0034 — operador `+` ambíguo entre `Vector2` e `Vector3`

**Mensagem típica:**

```
error CS0034: Operator '+' is ambiguous on operands of type 'Vector2' and 'Vector3'
```

**Causa:** em C#, `Vector2 + Vector3` não tem conversão implícita única; o compilador não sabe qual operando promover.

**Solução:** converter explicitamente para o tipo da posição world-space (geralmente `Vector3`):

```csharp
Vector3 anchor = hole.AnchorPosition; // Vector2 → Vector3 (z = 0)
_canvas.transform.position = anchor + offset;
```

Alternativa: manter tudo em 2D com `Vector2` e atribuir com `new Vector3(pos.x, pos.y, transform.position.z)`.

**Exemplo no projeto:** `RatHoleSealPromptUI` (prompt acima do buraco).

---

## Gameplay — câmera (dead zone invertida)

### Sintoma: jogador precisa encostar na borda para a câmera se mover

**Não é bug de smoothing lento apenas** — os campos `edgeDeadZoneX` / `edgeDeadZoneY` em `CameraConfig` controlam o tamanho da **zona morta central** (fração do viewport). A fórmula em `MultiplayerCameraController.ComputeEdgeFollowPosition()`:

```
margin = halfViewport * (1 - edgeDeadZone * 2)
```

| `edgeDeadZone` | Efeito |
|----------------|--------|
| **Baixo** (ex.: 0.10) | Margem grande → câmera só se move quando o jogador está muito perto da borda da tela |
| **Alto** (ex.: 0.40) | Margem pequena → câmera reage bem antes do jogador chegar na borda |

**Erro comum:** reduzir `edgeDeadZone` achando que “libera” a câmera — o efeito é o **oposto**.

**Valores atuais (teste drástico):** `edgeDeadZoneX = 0.42`, `edgeDeadZoneY = 0.40`, `edgePanSmoothing = 28` em `Assets/Data/Multiplayer/CameraConfig.asset`.

**Ajuste fino:** depois de validar o comportamento, reduza gradualmente (ex.: 0.35 / 0.32) se a câmera ficar “nervosa”.

---

## Gameplay — dissolve de inimigos (VOiD1)

### Sintoma: rato fica parado e some sem animação de morte nem dissolve visível

**Causa principal (2026-06-19):** `DissolveEffect` congelava o animator (`speed = 0`) após **1 frame**, antes da transição `OnDie → Dying` terminar. O jogador via sprite estático e depois `HideVisuals`.

**Sequência correta no projeto:**

1. `NetworkEnemyController.PlayDeathVisuals` dispara `OnDie` (`EnemyAnimationHandler.PlayDeathAnimation`).
2. `DissolveEffect` mantém `animator.speed = 1` e aguarda o estado **Dying** chegar a ~98% (`WaitUntilDeathAnimationComplete`).
3. Congela o animator no último frame.
4. Aplica material de dissolve (VOiD1: Fade **0 → 50** = visível → sumido).
5. `HideVisuals` só no fim do dissolve.

**Erros anteriores (não repetir):**

| Erro | Efeito |
|------|--------|
| Congelar animator antes da animação | Sprite parado, sem `Dying` |
| Inverter Fade VOiD1 (50→0) | Dissolve invisível ou invertido |
| `void1HideAtLinearTime` muito baixo (0.62) | Some abrupto antes do dissolve terminar |
| Zerar `sharedMaterial` antes do swap | Flash / sprite “reconstruído” |

**Arquivos:** `DissolveEffect.cs`, `DissolveMaterialBinding.cs`, `NetworkEnemyController.PlayDeathVisuals`.

**Shaders:**

| Material | Propriedade | Progresso 0→1 |
|----------|-------------|---------------|
| `DissolveSprite` | `_DissolveAmount` | 0 visível → 1 sumido |
| VOiD1 Graph | `Vector1_51DDBE76` | 0 visível → 50 sumido |

---

## Para agentes

Ao corrigir um erro novo que possa repetir:

1. Aplique o fix no código.
2. Adicione uma entrada neste arquivo (mensagem, causa, solução, exemplo no projeto se houver).
3. Se for padrão de rede, alinhar com [09-otimizacao-rede.md](../practices/09-otimizacao-rede.md).
