# Tarefas multiplayer pendentes

_Última revisão: 2026-07-05_

## Concluído nesta sessão

- [x] Vitória/derrota em MP: rotas `gameplay_victory` / `gameplay_defeat` carregam `VictoryScene`/`GameOver` localmente (`SinglePlayer`) mantendo Relay ativo
- [x] Transição unificada via `MultiplayerGameManager.BeginEndGameScreenTransitionClientRpc`
- [x] Sincronização visual de ataques/habilidades reforçada em `NetworkPlayerAbilityRelay`
- [x] Mecânica de Reviver quebrada (downed vs morte final, timer servidor, bleed-out)
- [x] Dessincronização da Carruagem (Fase 2)
- [x] Pausa multiplayer com countdown 3→1 e congelamento de gameplay no servidor

---

## [TASK CONCLUÍDA] Mecânica de Reviver quebrada (Desconexão/Dessincronização)

- **O que foi feito:** Separado fluxo **downed revivível** de **morte final** em MP. `PlayerDeathPresentation.BeginDownedPresentation()` toca animação de queda sem dissolve, foco de câmera ou `DeathHordePresentation`. `NetworkPlayerHealth` decrementa `_networkUnconsciousTimeRemaining` no servidor (pausa quando aliado na zona via `_networkRevivePaused`); ao esgotar, `_networkIsBleedingOut` dispara dissolve/spectator. Collider desligado no downed (`FinalizeDeathPhysics`); `PlayerAnimationHandler.RestoreFromDowned()` reativa no revive. Scripts: `NetworkPlayerHealth.cs`, `PlayerDeathPresentation.cs`, `PlayerAnimationHandler.cs`; doc `docs/multiplayer/revive-zone.md`.

- **Como testar (Singleplayer):** Abrir `Fase-1` direto (sem NGO) e morrer — deve usar `BeginDeathPresentation` (derrota final), sem círculo de reviver.

- **Como testar (Multiplayer/Netcode):**
  1. Host + Cliente via Preparation → Fase-1 (ParrelSync ou build + Editor).
  2. Derrubar o **Cliente** (deixar inimigos atacarem ou debug de dano).
  3. Verificar: Cliente fica no chão (sem sumir), **círculo verde** ao redor, câmera do Cliente **não** salta para o Host.
  4. Host entra no círculo e **permanece** ~3s (`reviveZoneFillDuration` em `DownedPlayerConfig`).
  5. Cliente revive com ~50% vida; Host não trava ao entrar na zona.
  6. (Opcional) Repetir sem reviver até `unconsciousDuration` (45s padrão) — bleed-out, dissolve se aliado ainda vivo.

- **Resultado Esperado:** Reviver cooperativo funcional; sem despawn prematuro do `NetworkObject`; sem travamento físico do Host no corpo caído; timer pausa com aliado na zona.

---

## [TASK CONCLUÍDA] Dessincronização da Carruagem (Fase 2)

- **O que foi feito:** `NetworkCarriage.OnNetworkSpawn` chama `PhaseGameplayContentInstaller.ConfigureCarriage` em **todos os peers** (não só no servidor), com coroutine de retry (`EnsurePathConfiguredRoutine`, timeout 15s) até `CarriagePath` local estar pronto. Posição ao configurar path usa `_pathProgress` replicado (não reseta sempre ao waypoint inicial). `SynchronizeTransform` desligado no prefab — sync de posição só via NV `_pathProgress` + `ApplyPathPosition` (sem `NetworkTransform`). HUD assina `PathProgressChanged` / `OnInstanceAvailable` em `PhaseObjectiveHud` além de `GameEvents`. `NetworkCarriageSpawner` só reconfigura path quando ainda não está pronto. Scripts: `NetworkCarriage.cs`, `NetworkCarriageSpawner.cs`, `PhaseGameplayContentInstaller.cs`, `PhaseObjectiveHud.cs`, `Carriage.prefab`; docs `docs/gameplay/carriage.md`.

- **Como testar (Singleplayer):** Menu → Contrato 2 → **Loading2** → Fase-2. Carruagem move; HUD superior mostra `Carruagem: X%` subindo.

- **Como testar (Multiplayer/Netcode):**
  1. Host + Cliente via Preparation → Contrato 2 → Loading2 → **Fase-2** (ParrelSync ou build + Editor).
  2. Aguardar início da fase; observar carruagem marrom no mapa.
  3. No **Host**: confirmar movimento e HUD `Carruagem: X%` > 0%.
  4. No **Cliente**: confirmar **mesmo** progresso na HUD e carruagem avançando (não parada em 0%).
  5. (Opcional) Cliente entra alguns segundos após o Host — progresso deve refletir valor atual, não resetar.

- **Resultado Esperado:** Movimento e percentual da carruagem sincronizados em tempo real entre Host e Cliente; `CarriagePath` presente na hierarquia de ambos os peers durante Play.

---

---

## [TASK CONCLUÍDA] Pausa do Jogo e Sincronização Multiplayer

- **O que foi feito:** `MultiplayerGameManager` agora usa `NetworkVariable<int> _resumeCountdown` e coroutine `ResumeCountdownRoutine` com `WaitForSecondsRealtime` (3→1) antes de retomar `GameState.Playing`. `GameEvents.IsPaused` consultado no servidor (`NetworkCarriage`, `EnemyMovement`, `RatHoleSpawnController`, `SpawnWaveRoutine`) para congelar gameplay sem depender só de `Time.timeScale`. `ApplyPauseClientRpc` + `GameEvents.InvokePauseChanged` no servidor garantem flag em host dedicado. UI: `GameManager2.ShowResumeCountdown` / `HideResumeCountdown` (TMP runtime se ausente no prefab); botão Resume desabilitado durante countdown (`PauseMenuActions.RefreshResumeInteractable`). `GameFlowOrchestrator` bloqueia pause durante countdown e delega resume ao servidor em MP. Scripts: `MultiplayerGameManager.cs`, `GameEvents.cs`, `GameFlowOrchestrator.cs`, `GameManager2.cs`, `PauseMenuActions.cs`, `NetworkWaveManager.cs`, `NetworkCarriage.cs`, `EnemyMovement.cs`, `RatHoleSpawnOrchestrator.cs`, `RatHoleSpawnController.cs`.

- **Como testar (Singleplayer):** Fase-1 solo → Esc/pause → Resume imediato (sem countdown); inimigos param com `timeScale = 0`.

- **Como testar (Multiplayer/Netcode):**
  1. Host + Cliente via Preparation → Fase-1 ou Fase-2.
  2. **Host** abre pause (Esc) → ambos veem overlay de pause; inimigos/carruagem param nos dois peers.
  3. **Cliente** clica Resume → contagem **3, 2, 1** visível nos dois; botão Resume desabilitado durante a contagem.
  4. Após **1**, jogo retoma em sincronia (movimento, spawn, carruagem).
  5. Repetir com **Cliente** pausando e **Host** despausando — mesmo comportamento.
  6. Durante countdown, tentar pausar de novo — deve ser ignorado.

- **Resultado Esperado:** Pausa global replicada; resume só após countdown síncrono; gameplay servidor congelado durante pause (não só `timeScale` no cliente).

---

## Verificação manual

- [ ] Host + cliente: selar último buraco (Fase-1) → ambos veem `VictoryScene` sem overlay de “aguardando host”
- [ ] Cliente Cora: Q/R disponíveis desde o spawn; VFX de tiro e habilidades visíveis no peer remoto
- [ ] “Prosseguir” na vitória retorna ao Preparation com lobby intacto
- [x] Host + cliente Fase-1: reviver por zona (círculo verde, sem despawn prematuro)
- [x] Host + cliente Fase-2: carruagem move e HUD `%` sincronizados (código — validar em Play)
- [x] Host + cliente: pausa/unpause com countdown 3→1 em ambos os peers (código — validar em Play)
