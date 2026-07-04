# Tarefas multiplayer pendentes

_Última revisão: 2026-07-04_

## Concluído nesta sessão

- [x] Vitória/derrota em MP: rotas `gameplay_victory` / `gameplay_defeat` carregam `VictoryScene`/`GameOver` localmente (`SinglePlayer`) mantendo Relay ativo
- [x] Transição unificada via `MultiplayerGameManager.BeginEndGameScreenTransitionClientRpc`
- [x] Sincronização visual de ataques/habilidades reforçada em `NetworkPlayerAbilityRelay`

---

### MULTIPLAYER - Mecânica de Reviver quebrada (Desconexão/Dessincronização)
- **Comportamento Atual vs Desejado:** Cliente caído fica estático; ao se aproximar o Host trava; após alguns segundos o Cliente some da rede e a câmera foca no Host. Desejado: zona circular com opacidade baixa ao redor do caído, aliado permanece na área para progresso de reviver (raio via `DownedPlayerConfig`), círculo interno expandindo até a borda (padrão selamento de buracos).
- **Arquivos Investigados:** `Assets/Scripts/Multiplayer/Player/NetworkPlayerHealth.cs`, `Assets/Scripts/Multiplayer/Player/DownedReviveZoneSystem.cs`, `Assets/Scripts/UI/World/DownedReviveZoneVisual.cs`, `Assets/Scripts/Components/Player/PlayerDeathPresentation.cs`, `Assets/Scripts/Components/Player/DeathHordePresentation.cs`, `Assets/Scripts/Multiplayer/Camera/MultiplayerCameraController.cs`, `Assets/Scripts/Multiplayer/Player/NetworkPlayerSpectator.cs`, `Assets/Scripts/ScriptableObjects/DownedPlayerConfig.cs`
- **Causas Prováveis Identificadas:**
  1. **`PlayerDeathPresentation` + `DeathHordePresentation` desviam do fluxo de downed:** ao cair, `ApplyUnconsciousLocal()` dispara sequência de morte com `FreezeDeathPose()`, `BeginDeathFocus()` na câmera e, após hold + dissolve, `TryRebindCameraToAliveTeammate()` — reproduz o Cliente sumindo e câmera indo para o Host; o modo espectador (`NetworkPlayerSpectator.EnterSpectatorMode`) nunca é invocado no downed, apenas `ExitSpectatorMode` no revive.
  2. **Timer de inconsciência não decrementa no servidor:** em `NetworkPlayerHealth.Update()` o tick de bleed-out está comentado/pausado (`// Bleed-out/revive pausado até existir animação de down`); `_networkUnconsciousTimeRemaining` é setado em `EnterUnconsciousOnServer()` mas não é consumido — revive por zona pode nunca concluir ou conflitar com derrota por timeout ausente.
  3. **Colisão física entre jogadores no downed:** `CombatLayerCollision` ignora Player×Enemy, mas **não** Player×Player; corpo do caído com `FinalizeDeathPhysics()` e collider ativo pode bloquear o Host ao entrar no raio de reviver (`reviveZoneRadius`), causando sensação de “travamento” na zona de morte.
- **Plano de Ação Recomendado:**
  1. Em `ApplyUnconsciousLocal()`, pular dissolve/rebind de câmera quando `CanBeRevived` e existir aliado vivo (`ShouldDissolveAfterDeathHold()` já cobre parte disso — validar que Cliente remoto não executa `TryRebindCameraToAliveTeammate` no owner do caído).
  2. Reativar tick server de `_networkUnconsciousTimeRemaining` (pausar via `_networkRevivePaused` quando aliado na zona, conforme doc `docs/multiplayer/revive-zone.md`).
  3. No downed: desativar ou tornar trigger o collider do jogador caído; manter `DownedReviveZoneVisual` + `DownedReviveZoneSystem.TickServer` como única lógica de progresso (sem novos sistemas).
  4. Teste manual Host+Cliente Fase-1: derrubar Cliente → verificar círculo verde + barra; Host permanece na zona ~3s → Cliente revive sem despawn de `NetworkObject`.

---

### MULTIPLAYER - Dessincronização da Carruagem (Fase 2)
- **Comportamento Atual vs Desejado:** Carruagem parada e HUD em 0% no Cliente; no Host funciona. Desejado: movimento e `Carruagem: X%` sincronizados em tempo real.
- **Arquivos Investigados:** `Assets/Scripts/Multiplayer/Carriage/NetworkCarriage.cs`, `Assets/Scripts/Multiplayer/Carriage/NetworkCarriageSpawner.cs`, `Assets/Scripts/Multiplayer/Core/PhaseGameplayContentInstaller.cs`, `Assets/Scripts/UI/PhaseObjectiveHud.cs`, `Assets/Prefabs/Gameplay/Carriage.prefab`, `docs/gameplay/carriage.md`
- **Causas Prováveis Identificadas:**
  1. **Ausência de `NetworkTransform`:** movimento ocorre só no servidor via `transform.position` em `Update()`; clientes dependem exclusivamente de `_pathProgress` (`NetworkVariable`, write Server) + `ApplyPathPosition()`. Se o `NetworkObject` não estiver `IsSpawned` no Cliente, a NV não replica e o objeto fica em 0%.
  2. **`CarriagePath` nulo ou incompleto no Cliente:** `ApplyPathPosition()` retorna cedo se `path == null`; `PhaseGameplayContentInstaller.ConfigureCarriage()` pode perder corrida com `OnNetworkSpawn` no peer remoto — progresso e posição permanecem no waypoint inicial.
  3. **`NetworkCarriage.Instance` / HUD desatualizado:** `PhaseObjectiveHud.RefreshFromLocalState()` lê `carriage.PathProgress` do singleton; se o Cliente não recebeu spawn do prefab registrado em `DefaultNetworkPrefabs` / `GameplayPrefabCatalog`, `Instance` fica null ou com NV zerada — HUD preso em 0%.
- **Plano de Ação Recomendado:**
  1. Confirmar no build que `Carriage.prefab` está em `DefaultNetworkPrefabs` (menu **MidnightMeow → Phases → Register Network Prefabs**).
  2. Em `NetworkCarriage.OnNetworkSpawn` (todos os peers): garantir `ConfigureCarriage` + `ApplyPathPosition` após path pronto; log diagnóstico se `path.WaypointCount < 2` no Cliente.
  3. Validar que `_pathProgress.OnValueChanged` dispara `GameEvents.InvokeCarriagePathProgressChanged` em clientes (hoje só no handler de NV — confirmar subscription pós-spawn).
  4. Opcional mínimo: adicionar `NetworkTransform` com autoridade Server e threshold baixo, **sem** substituir a NV de progresso já usada pela HUD.

---

### MULTIPLAYER - Pausa do Jogo e Sincronização Multiplayer
- **Comportamento Atual vs Desejado:** Pausa não congela o motor de fato e não replica na rede. Desejado: pausa global para ambos; qualquer jogador pode despausar com contagem regressiva 3→1 síncrona antes de retomar.
- **Arquivos Investigados:** `Assets/Scripts/Multiplayer/Core/MultiplayerGameManager.cs`, `Assets/Scripts/Core/GameFlowOrchestrator.cs`, `Assets/Scripts/GameManager2.cs`, `Assets/Scripts/Multiplayer/Lobby/PreparationSessionManager.cs` (referência de countdown existente)
- **Causas Prováveis Identificadas:**
  1. **`Time.timeScale = 0` não pausa NGO:** `ApplyPauseClientRpc` altera `Time.timeScale`, mas ticks de rede, `NetworkTransform` e coroutines com `WaitForSeconds` (não unscaled) continuam — inimigos/rede seguem em partes do pipeline.
  2. **Resume imediato sem countdown:** `RequestResumeRpc` restaura `GameState.Playing` e `timeScale = 1` sem fase de preparação; não há `NetworkVariable` de countdown compartilhado nem `ClientRpc` de UI.
  3. **Dessincronia local vs rede:** `GameManager2.TogglePause()` delega ao orchestrator em MP, mas `currentState` local pode divergir de `_networkGameState` se `ApplyPauseClientRpc` falhar (ex.: `GameManager2` ausente na cena e fallback para `SceneOverlayController` incompleto).
- **Plano de Ação Recomendado:**
  1. Estender `MultiplayerGameManager` com `NetworkVariable<byte> _pauseCountdown` (write Server) ou reutilizar padrão de `PreparationSessionManager.StartCountdownRoutine`.
  2. `RequestResumeRpc` → servidor inicia countdown 3→1 via coroutine **unscaled**; só após 0 definir `GameState.Playing` e `ApplyPauseClientRpc(false)`.
  3. Congelar gameplay via flag consultada em `PlayerInputHandler`, `EnemyMovement` e spawners (não depender só de `timeScale`); manter NGO ativo para RPCs de unpause.
  4. Exibir overlay de countdown no `PauseMenu` / `GameManager2.ShowPauseOverlay` em todos os clientes via `ClientRpc` existente.

---

## Verificação manual

- [ ] Host + cliente: selar último buraco (Fase-1) → ambos veem `VictoryScene` sem overlay de “aguardando host”
- [ ] Cliente Cora: Q/R disponíveis desde o spawn; VFX de tiro e habilidades visíveis no peer remoto
- [ ] “Prosseguir” na vitória retorna ao Preparation com lobby intacto
- [ ] Host + cliente Fase-2: carruagem move e HUD `%` sincronizados
- [ ] Host + cliente: pausa/unpause com countdown 3→1 em ambos os peers
