# Erros e avisos comuns

Última revisão: 2026-06-28

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

**Exemplo no projeto:** `CreditsOverlayController` (corpo dos créditos), `RatHoleSealPromptUI`, `RatHoleSealStatusUI`.

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

**Exemplo no projeto:** `RatHoleSealPromptUI`, `RatHoleSealStatusUI` (`(Vector3)hole.AnchorPosition + offset`).

### CS0103 — nome não existe no contexto atual

**Mensagem típica:**

```
error CS0103: The name '_hole' does not exist in the current context
```

**Causa:** variável ou campo com nome errado — por exemplo, copiar código de `RatHoleSealStatusUI` (que usa o campo `_hole`) para `RatHoleSealPromptUI`, onde o buraco é uma variável local `hole` em `LateUpdate`.

**Solução:** usar o identificador correto no escopo:

```csharp
// RatHoleSealPromptUI — variável local
RatHoleSpawnPoint hole = _interaction?.CurrentTargetHole;
_canvas.transform.position = (Vector3)hole.AnchorPosition + offset;

// RatHoleSealStatusUI — campo de instância
_canvas.transform.position = (Vector3)_hole.AnchorPosition + offset;
```

**Exemplo no projeto:** `RatHoleSealPromptUI.cs` (variável local `hole` vs campo `_hole`), `EnemyHealthBarDisplay.cs` (campo `sortingOrder` removido acidentalmente mas ainda usado em `BuildBar`).

---

## TextMesh Pro (UI world-space selamento)

### CS0618 — `enableWordWrapping` em `RatHoleSealPromptUI` / `RatHoleSealStatusUI`

Mesma regra da secção [TMP_Text.enableWordWrapping](#cs0618---tmp_textenablewordwrapping-is-obsolete) acima: usar `textWrappingMode = TextWrappingModes.NoWrap` em prompts de uma linha no world-space.

---

## Zonas de selamento invisíveis (world-space)

### Sintoma: mecânica funciona (progresso sobe) mas o círculo não aparece

**Causa:** `Sprite.Create` com `Rect(0,0,1,1)` e **pixelsPerUnit padrão (100)** gera sprite de ~0,01 uu — invisível mesmo com `localScale` alto.

**Correção:** usar `CooperativeZoneSpriteFactory.GetUnitQuadSprite()` (ppu = largura da textura) ou `SealZoneRingVisual` com sprites procedurais. Tamanho visual: `RatHoleSealConfig.GetZoneVisualDiameter()` (`zoneVisualScaleMultiplier` não altera a hitbox).

**Pipeline:** `RequestStartSealRpc` → `NetworkList` sessão `IsActive` → `NotifySealZoneVisualClientRpc` + `RatHoleSealZoneVisual.ShowSession` → objetos em `_GameLoop/SealZoneVisuals/SealZonePool/SealZone_{holeId}_{n}`.

**Hierarquia:** não procurar sob o buraco — zonas ficam sob **`---- Sistemas ----/_GameLoop/SealZoneVisuals`**. Pool pré-criado (inativo até selar); ativo enquanto `IsActive` (até selar ou `abandonTimeout` sem jogador na área).

**Ordem de desenho:** `zoneSortingOrder` 250 (buracos/mapa ~0–16); Z = -2 nas zonas.

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
| `HideVisualsClientRpc` antes do dissolve local terminar | Cliente some e “reconstrói” com dissolve |

**Correção (2026-06-22):** removido `HideVisualsClientRpc` de `FinalizeDeathPresentation`. Cada peer conclui dissolve localmente; `HideAllVisualsLocal` no servidor ignora interrupção enquanto `DissolveEffect.IsPlaying`.

**Arquivos:** `DissolveEffect.cs`, `DissolveMaterialBinding.cs`, `NetworkEnemyController.PlayDeathVisuals`.

**Shaders:**

| Material | Propriedade | Progresso 0→1 |
|----------|-------------|---------------|
| `DissolveSprite` | `_DissolveAmount` | 0 visível → 1 sumido |
| VOiD1 Graph | `Vector1_51DDBE76` | 0 visível → 50 sumido |

---

## Unity — GUID inválido em `.meta` / referências YAML

### Sintoma

```
Could not extract GUID in text file Assets/.../Fase3.asset at line ...
Broken text PPtr. GUID 00000000000000000000000000000000 fileID ... is invalid!
```

### Causa

GUIDs no Unity têm **exatamente 32 caracteres hexadecimais** (0–9, a–f). Um `.meta` com 31 ou 33 caracteres quebra referências em SOs, prefabs e `DefaultNetworkPrefabs.asset`.

**Exemplo no projeto (2026-06-25):** `Rato_Boss.prefab.meta` foi criado manualmente com GUID de 31 ou 33 caracteres — inválido. GUID correto: `b8c9d0e1f2a34567901234567890abcd` (32 chars). Valide com `len(guid) == 32` antes de commitar.

### Solução

1. Corrija o `guid:` no `.meta` do asset (32 caracteres).
2. Atualize todas as referências `{fileID: ..., guid: ..., type: 3}` nos YAML que apontam para esse asset.
3. No Editor: clique direito na pasta → **Reimport**, ou reinicie o Unity.

**Não** invente GUIDs à mão sem contar os 32 caracteres; prefira deixar o Unity gerar ao criar o asset pelo Editor, ou copie o GUID de um `.meta` existente válido.

---

## C# — variável `out` não atribuída com `?.`

### CS0165 — Use of unassigned local variable

**Causa:** `catalog?.TryGetEntry(scene.name, out entry)` não atribui `entry` quando `catalog` é `null`.

**Solução:**

```csharp
PhaseEntry entry = null;
if (catalog != null)
    catalog.TryGetEntry(scene.name, out entry);
```

**Exemplo no projeto:** `PhaseSceneSetupEditor.SetupScene`.

---

## C# — CS0414 campo serializado não usado

### warning CS0414 — field is assigned but never used

**Causa:** campo `[SerializeField]` deixado de ser lido após refatoração (ex.: `gameplaySceneName` substituído por `GameplaySceneBootstrap.IsGameplayScene`).

**Solução:** reutilizar o campo como fallback para cenas legadas (`Game`, `Gameplay`) ou remover se não houver prefabs que dependam dele.

**Exemplo no projeto:** `MultiplayerGameManager.IsActiveGameplayScene`, `MultiplayerBootstrapper.IsActiveGameplayScene`.

**Exemplo no projeto:** `RatHoleSpawnPoint.Reset` durante `Undo.AddComponent` no `PhaseSceneSetupEditor` — adicionar `CircleCollider2D` **antes** do `RatHoleSpawnPoint` e usar null-check em `EnsureTriggerCollider`.

---

## Editor — MissingComponentException ao rodar Phase Setup

### Sintoma

```
MissingComponentException: There is no 'CircleCollider2D' attached to the "SP1" game object
RatHoleSpawnPoint.Reset () 
UnityEditor.Undo:AddComponent
PhaseSceneSetupEditor:InstallRatHoleSpawnPoints
```

### Causa

`Undo.AddComponent<RatHoleSpawnPoint>` dispara `Reset()` antes do collider existir; `AddComponent<CircleCollider2D>()` dentro de `Reset` pode falhar nesse contexto.

### Solução

1. Em `RatHoleSpawnPoint`, usar `EnsureTriggerCollider()` com null-check.
2. No `PhaseSceneSetupEditor`, adicionar `CircleCollider2D` **antes** de `RatHoleSpawnPoint`.
3. Ignorar spawn points de jogador (`---- Spawn Points Jogadores ----`).

---

## Gameplay — carruagem (Fase 2)

### Sintoma: carruagem bate na parede antes de completar o trajeto

**Causa:** `pathEndX` ultrapassava o limite walkable do mapa.

**Solução:** ajustar `CarriageConfig.pathEndX` (atual **18**, 2 uu à esquerda do valor anterior 20). Waypoints recriados em runtime por `PhaseGameplayContentInstaller`. O fim X é ainda limitado a `CameraBounds.max.x - 2`.

### Sintoma: carruagem trava em ~97% e não vence a fase

**Causa:** progresso era **temporal** (0→1 por tempo) enquanto a posição real parava antes do fim (parede/colisão). HUD mostrava ~97% mas `OnCarriageArrived` não disparava.

**Solução:** movimento por **distância** ao waypoint final; progresso = projeção no segmento (`CarriagePath.GetNormalizedProgress`). Vitória em `≥98%`, `arrivalZoneRadius`, ou snap no fim (`CompleteArrival`).

---

### Sintoma: carruagem enorme, parada, em (0,0) ou `CarriagePath` vazio no Inspector

**Causas comuns:**

| Causa | Efeito |
|-------|--------|
| Sprite de menu/UI no prefab + `visualScale = 3` | Carruagem gigante na tela |
| `NetworkCarriageSpawner` só configurava no servidor | Solo/host OK às vezes; clientes sem `path` local |
| Cena sem `CarriagePath` e setup abortado cedo | `path` nulo → `Update` não move |
| Abrir `Fase-2` direto no Editor sem host | Spawner expira; carruagem in-scene fica em (0,0) sem path |
| Inspector de `CarriagePath` vazio fora do Play | Normal — waypoints são filhos criados em runtime |

**Correções no projeto (2026-06-28):**

1. Placeholder **sempre** quadrado marrom (~2,4×1,6 uu); prefab `Visual` sem sprite.
2. `CarriageConfig.visualScale` padrão **1** (multiplicador opcional).
3. `PhaseGameplayContentInstaller.ConfigureCarriage` roda em **todos os peers**; spawner repete até path + `NetworkObject.Spawn`.
4. `NetworkCarriage.HandlePathProgressChanged` chama `ApplyPathPosition` nos clientes.
5. Solo: host em Loading2 via `GameplaySessionStarter` antes de `EnterGameplay`.

**Validação:** Menu → Contrato 2 → Characters → Loading2 → Fase-2. Console: `[NetworkCarriageSpawner] Carruagem pronta (path + spawn).` Hierarquia Play: `CarriagePath/Waypoint_Start`, `Waypoint_End`.

### Sintoma: carruagem chega ao fim mas não vai para a tela de vitória (solo/host)

**Causa principal (2026-06-28):** `PhaseGameplayContentInstaller` usava `RuntimeInitializeLoadType.AfterSceneLoad`, que só roda na **primeira** cena (Bootstrap). Ao entrar em Fase-2, `PhaseObjectiveManager` nunca era criado — sem logs `[PhaseObjectiveManager]` no Console.

**Solução:** hook em `SceneManager.sceneLoaded` + re-aplicação quando `NetworkWaveManager` faz spawn no servidor. A carruagem também reconfigura a fase ao chegar ao destino se o manager estiver ausente.

**Console esperado ao entrar na fase:** `[PhaseGameplayContentInstaller] PhaseObjectiveManager configurado para Fase-2 (vitória=CarriageReachEnd).`

**Ao vencer:** `[PhaseObjectiveManager] Vitória: Carruagem chegou ao destino.` → após ~2s, tela de vitória.

Outras causas já corrigidas: delay de vitória com `WaitForSecondsRealtime`; fallback de tela de vitória.

Ver também: [gameplay/carriage.md](../gameplay/carriage.md).

---

## Gameplay — magículas (ciência) paradas no chão

### Sintoma: alguns drops de ratos não são coletados (Fase 2)

**Causas comuns:**

| Causa | Efeito |
|-------|--------|
| Prefab `NetworkCiencia` sem collider/homing | Coleta só por overlap muito pequeno no servidor |
| `networkCienciaPrefab` apontando para prefab incompleto | Usar `Science.prefab` (com collider + homing) |
| Drop duplicado no cliente | Fantasma local sem coleta de rede |

**Solução (2026-06-28):** `NetworkCienciaController` garante collider + RB kinematic + homing em runtime; coleta também busca jogadores conectados; `EnemyDropHandler` só dropa no servidor em sessão NGO; `ResolveCienciaSpawnPrefab` prefere prefab completo (`Science`).

**Config:** `Assets/Resources/CienciaPickupConfig.asset` (`homingRadius`, `collectRadius`).

---

## Gameplay — dissolve / morte de inimigos

### Sintoma: rato morre, some, reaparece no último frame com shader e some de novo

**Causa:** material VOiD1 legado + `EnemySpawnPresentation` abortando sem restaurar materiais; `EnsureRenderersVisibleForDissolve` reativava sprites após um hide parcial.

**Solução (2026-06-28):** shader **`MidnightMeow/EnemyDeathFade`** (`Assets/Art/Materials/EnemyDeathFade.mat`) em todos os `Rato_*`. Sequência: animação `Dying` → fade alpha linear → hide. `EnemySpawnPresentation.CancelForDeath()` restaura materiais antes do fade.

---

## Gameplay — telegraph inimigo (forma errada)

### Sintoma: faixa estreita (retângulo) aparece como oval/círculo distorcido

**Causa:** `EnemyTelegraphZoneView.ApplyStyle` retornava cedo quando `visualStyle == null` **antes** de definir `_Shape` no material. O template `TelegraphZoneMaterial` usa `_Shape = 0` (círculo); escala não uniforme (ex.: 0,77×7) vira elipse.

**Padrão afetado:** `Rato_Acido_Lane` (`shape: Rectangle`, `visualStyle` nulo).

**Correção:** `_Shape` e `_FillMode` são aplicados **sempre**; cores padrão quando `visualStyle` é nulo. Retângulos usam contorno nas 4 bordas no shader.

**Validação:** ataque do Rato Ácido = faixa reta fina alinhada ao alvo, não oval.

### Sintoma: retângulo enche do lado errado (ex.: rato acima, fill sobe de baixo)

**Causa:** fill fixo em UV local Y; não considerava posição do atacante.

**Solução:** `EnemyTelegraphZoneView.ConfigureFillOrigin` + `_FillOriginSide` no shader — preenchimento começa no canto mais próximo do `attackOrigin` / spawn do projétil.

Ver: [enemy-telegraph-attacks.md](../combat/enemy-telegraph-attacks.md).

---

## Para agentes

Ao corrigir um erro novo que possa repetir:

1. Aplique o fix no código.
2. Adicione uma entrada neste arquivo (mensagem, causa, solução, exemplo no projeto se houver).
3. Se for padrão de rede, alinhar com [09-otimizacao-rede.md](../practices/09-otimizacao-rede.md).
