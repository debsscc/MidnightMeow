# Reviver por zona (área cooperativa)

Última revisão: 2026-07-18

## Máquina de estados

| Estado | Gatilho (servidor) | UI (visualização local) | Rede |
|--------|-------------------|-------------------------|------|
| **Caído** | HP = 0 → `EnterUnconsciousOnServer()` | Dono (`IsOwner`): *"Aguarde ser revivido"*. Aliado próximo: *"Aproxime-se para revive-lo"* | `NetworkVariable` `_networkIsUnconscious` |
| **Proximidade** | Aliado vivo dentro de `revivePromptRadius` | Aliado: *"Aperte E para revive-lo"* | Detecção local no cliente (sem RPC) |
| **Revivendo** | `RequestStartReviveRpc` + ocupação nas zonas | Todos: `{0}%` (`reviveProgressTextFormat`) | `NetworkList<DownedReviveSession>`, `_networkReviveProgress`, `_networkReviveZoneActive` |
| **Revivido** | Progresso = 100% → `ServerReviveFromUnconscious()` | Label e círculos ocultos; animação idle | HP = `reviveHealthFraction` × max |

### Separação servidor / cliente

- **Servidor:** dano, inconsciência, sessões (`NetworkDownedReviveManager`), progresso (`DownedReviveZoneSystem.TickSession`), revive final.
- **Cliente:** `DownedPlayerWorldUI` resolve texto por observador local (`IsOwner` vs aliado `CanFight`); `DownedReviveZoneVisualHost` desenha círculos a partir da `NetworkList` + `ClientRpc` de fim de sessão.

## Comportamento (transplante do selamento de buracos)

1. Jogador A cai **inconsciente** — animação de morte trava no último frame; inputs desabilitados no dono.
2. Aliado vivo se aproxima → label muda conforme distância (`DownedPlayerWorldUI`).
3. Ao pressionar **E** dentro de `revivePromptRadius`, o servidor coloca 1–2 círculos cooperativos (`CooperativeZonePlacementUtility`), evitando sobreposição com colisões **Wall** / **DashableWall** (mesma regra do selamento).
4. Aliado(s) permanecem dentro do(s) círculo(s) → progresso sobe (`reviveZoneFillDuration` no SO).
5. Ninguém nas áreas por `reviveAbandonTimeout` → sessão cancelada; `NotifyReviveSessionEndedClientRpc` limpa círculos em todos os clientes.
6. Progresso = 100% → `ServerReviveFromUnconscious()` restaura `reviveHealthFraction` da vida máxima.

## Configuração (`DownedPlayerConfig`)

Asset: `Assets/Data/Multiplayer/DownedPlayerConfig.asset`

| Campo | Uso |
|-------|-----|
| `unconsciousDuration` | Tempo até bleed-out |
| `reviveHealthFraction` | HP ao reviver (0.5 = 50%) |
| `revivePromptRadius` | Raio para ver "Aperte E" e validar RPC |
| `reviveZoneFillDuration` | Segundos para 100% com 1 jogador em 1 zona |
| `reviveZoneRadius` | Hitbox de cada círculo |
| `reviveAbandonTimeout` | Cancelamento sem ocupação |
| `reviveLabelVisibilityRadiusMultiplier` | Multiplicador do raio de *"Aproxime-se"* |
| `ownerWaitingText` / `allyApproachText` / `allyPressEText` | Strings do label |
| `reviveProgressTextFormat` | Ex.: `{0}%` |
| `revivePromptPrefab` | Prefab world-space (`DownedRevivePromptUI`) |
| `downedHeartbeatClip` | SFX de batida durante downed (`Coracao Batida.wav`) — **só MP** |
| `reviveCompleteClip` | SFX ao concluir revive (`Reviver.wav`) — **só MP** |
| `downedScreenPulseIntensity` | Intensidade da vinheta pulsante enquanto há caído |

## Feedback de tela (só multiplayer)

| Momento | Áudio | Visual |
|---------|-------|--------|
| Aliado inconsciente (janela de revive) | Batidas `downedHeartbeatClip` | Vinheta pulsante + timer HUD |
| Revive concluído | `reviveCompleteClip` (`Reviver.wav`) via `GameplayInteractAudio.PlayReviveComplete` | Pulso breve de vinheta (`TriggerReviveSuccessPulse`) |
| Início da interação (E) | `Interacao.wav` via `GameplayInteractAudio.PlayConfirm` | — |

O mesmo `PlayReviveComplete` toca ao concluir o conserto da carruagem (`NetworkCarriageRepairManager`).

Singleplayer não entra em downed cooperativo (`CanUseCooperativeRevive` = false), então esse feedback não roda.

## Código

| Script | Papel |
|--------|--------|
| `NetworkPlayerHealth` | Vida, inconsciência, `ServerReviveFromUnconscious`, NetworkVariables |
| `NetworkDownedReviveManager` | Sessões replicadas, `RequestStartReviveRpc`, tick servidor |
| `DownedReviveZoneSystem` | Progresso/abandono |
| `DownedReviveZoneVisualHost` | Círculos com `SealZoneRingVisual` |
| `PlayerDownedReviveInteraction` | Interact (E) no aliado vivo |
| `DownedPlayerWorldUI` | Label world-space no caído — máquina de estados por observador |
| `DownedReviveUILabelView` | Referência TMP no prefab (sem UI procedural) |
| `DownedReviveScreenFeedback` | Heartbeat/pulso downed + SFX/pulso de sucesso no revive |
| `PlayerDeathPresentation` | Animação caída / freeze no último frame |
| `RevivePromptWorldUI` | **Obsoleto** — substituído por `DownedPlayerWorldUI` |

## Edição visual do prompt (Editor)

O prompt usa **prefab world-space** (`DownedRevivePromptUI.prefab`).

1. Canvas **World Space**, filho `Label` com **TextMeshProUGUI** (texto padrão ignorado em runtime).
2. Opcional: componente `DownedReviveUILabelView` no root do prefab apontando para o TMP.
3. Conectar em `DownedPlayerConfig.revivePromptPrefab` ou no campo **Revive UI Prefab** de `DownedPlayerWorldUI` no prefab do personagem.

## Teste

Host + cliente na Fase-1:

1. Derrubar um jogador → dono vê *"Aguarde ser revivido"*.
2. Aliado se aproxima → *"Aproxime-se"* → dentro do raio → *"Aperte E"*.
3. E → círculo verde; label mostra `0%`…`100%` nos dois clientes.
4. Completar → jogador levanta com 50% HP; UI e círculo somem.

## Troubleshooting (UI invisível)

| Causa | Selamento (funciona) | Reviver (antes do fix) |
|-------|---------------------|------------------------|
| SO em runtime | `Resources.Load<RatHoleSealConfig>("RatHoleSealConfig")` | `NetworkPlayerHealth.downedConfig` **null** no prefab |
| UI procedural | `RatHoleSealPromptUI.BuildUI()` com layout fixo | Prefab com Label `200×50` centralizado (layout incorreto) |
| Parent scale | Filho do player (`0.4`) — layout correto compensa | Mesmo parent, mas `SetActive` nunca `true` (config null) |
| Física | `Vector2.Distance` — sem `OverlapSphere` | Idem — sem física |
| Rede | `SetActive` local, sem `IsServer` | Idem — sem `NetworkObject` na UI |

**Resolução:** `DownedPlayerConfigUtility.Resolve()` + `downedConfig` ligado em `NetworkPlayerHealth` nos prefabs `Nixie`/`Cora`; canvas instanciado na raiz da cena (fora da escala `0.4` do player).
