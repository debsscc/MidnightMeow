# Implementação Fases 1–3 — plano de trabalho

Última revisão: 2026-06-25

## Decisões de design (não especificadas pelo pedido)

| Decisão | Escolha | Motivo |
|---------|---------|--------|
| Desbloqueio de contratos (teste) | `ContractProgressionConfig.unlockAllContractsForTesting = true` em `Resources/` | Permite testar Fase 2/3 sem vitória prévia; desligar flag para progressão linear |
| Progressão linear | Contrato N exige contrato N−1 concluído (`completedContractMask` no save) | Preparado para produção |
| Fase 3 (cena) | Cópia da stack MP da Fase-1 com `Fase3.asset` (1 onda, 1 boss) | Reuso do que já funciona em MP; arte do mapa pode evoluir depois |
| Fase 2 (carruagem) | Trajeto horizontal: centro esquerda → centro direita do `CameraBounds` | Pedido explícito; waypoints criados pelo menu de Editor |
| Fase 2 (selamento) | Mesma mecânica da Fase-1 nos spawn points de inimigos | Código já existia; faltava cena/SO |
| Boss | `Rato_Boss` baseado em `Rato_Padrao_Resistente`, escala 2×, 200 HP | Placeholder até habilidades de boss |
| Barras de vida inimigos | `EnemyHealthBarDisplay` (já no projeto) + `BossEnemyMarker` para boss sempre visível | Evita duplicar sistema; boss precisa barra permanente |
| Setup de cena | Menu **MidnightMeow/Phases/Setup Active Phase Scene** | Cenas Unity são difíceis de editar só via texto; script de Editor aplica MP + mecânicas |

## Checklist por fase

### Fase 1 — Contrato 1 (`Fase-1.unity`)

- [x] Stack multiplayer (`NetworkWaveManager`, `MultiplayerGameManager`, etc.)
- [ ] `RatHoleSealConfig.asset` em `Assets/Data/Gameplay/`
- [ ] `RatHoleSpawnPoint` + sprite de buraco em cada spawn de inimigo (via Editor)
- [ ] `NetworkRatHoleSealManager` com config atribuída no `_GameLoop`
- [ ] Teste: prompt F, zonas cooperativas, buraco deixa de spawnar

### Fase 2 — Contrato 2 (`Fase-2.unity`)

- [ ] Portar stack MP (remover dependência só de `NightManager` em MP)
- [ ] `Fase2.asset` no `NetworkWaveManager`
- [ ] Selamento nos spawn points
- [ ] Prefab `Carriage` + `CarriageConfig` + trajeto horizontal
- [ ] Registrar `Carriage` em `DefaultNetworkPrefabs`
- [ ] Teste solo + host/cliente (sem Nixie/Cora na cena — só spawn NGO)

### Fase 3 — Contrato 3 (`Fase-3.unity`)

- [ ] Cena criada e no Build Settings
- [ ] `Contract_3` → `Fase-3`
- [ ] `Fase3.asset` — 1 onda, 1× `Rato_Boss`
- [ ] Sem selamento nem carruagem
- [ ] Prefab `Rato_Boss` em `DefaultNetworkPrefabs`
- [ ] Teste vitória ao matar boss

### Fluxo / contratos

- [x] `ContractProgressionConfig` + utilitário de desbloqueio
- [x] `ContractCatalog` em `Resources/` (referências diretas Contract_1/2/3)
- [x] `ContractSceneResolver.ResolveActiveContractIndex()` (save + sessão + `GameSessionContext`)
- [x] Remover bloqueio hardcoded `index > 0` na preparação
- [x] Marcar contrato concluído na vitória (SP + MP)
- [ ] Teste: Contrato 2 → Fase-2, Contrato 3 → Fase-3 após Characters → Ready

### Barras de vida (inimigos)

- [x] `EnemyHealthBarDisplay` em inimigos com tag `Enemy`
- [x] `BossEnemyMarker` força barra sempre visível e maior

## Correções selamento E + spawn Fase 2 + carruagem (2026-06-25 — quarta rodada)

| Tarefa | Diagnóstico | Status |
|--------|-------------|--------|
| Prompt "Aperte E para selar" + input | Texto ainda em F; ação Interact com `Hold` e `_interactAction` nulo se resolvida só no `Awake` | concluído |
| Fase 2 solo: 2 Coras + 1 Nixie | Prefabs `Nixie` e `Cora` colocados na cena `Fase-2.unity` além do spawn NGO | concluído |
| Carruagem pequena + HP + HUD percurso | Sprite `Visual` 2.4×1.6; progresso só no servidor; sem barra de vida | concluído |

### O que mudou (código)

- `PlayerInputHandler` — resolve/subscribe no `OnEnable`; `performed` na ação Interact; removido `Hold` do Input System.
- `PlayerRatHoleSealInteraction` — fallback direto na tecla **E** (`Keyboard.current.eKey`).
- Textos **E** em `RatHoleSealPromptUI` e `CarriageRepairPromptUI`.
- `GameplayScenePlayerCleanup` + remoção no Editor (`PhaseSceneSetupEditor.RemoveScenePlacedPlayerCharacters`).
- `CarriageConfig.visualScale`, `NetworkCarriage.EnsureRuntimePresentation()`, sync de vida, `PhaseObjectiveHud` com `Carruagem: X%`.

### Setup manual

1. **MidnightMeow → Phases → Setup All Phase Scenes** (remove Nixie/Cora da cena e reaplica stack).
2. Salvar cenas após o setup.

---

## Correções UX + selamento + personagem (2026-06-25 — terceira rodada)

| Tarefa | Diagnóstico | Status |
|--------|-------------|--------|
| Fase 2 solo: duas Coras / Nixie vira Cora | NGO + `PlayerSpawnManager` spawn duplo; `GameSaveData.SelectedCharacter` ignorava Cora | concluído |
| Tooltips dos contratos no hover | Texto vinha de `description` desatualizada nos SOs | concluído |
| Áreas de selamento invisíveis | Zonas longe da câmera/opacidade baixa; shader telegraph falha em alguns casos | concluído |
| Fluxo texto selamento | Prompt não trocava para "Fique na Área..."; selado sem "Área selada" | concluído |
| Barra de vida desalinhada / fina | Âncora em `bounds.max`; altura pequena | concluído |

### O que mudou (código)

- `GameSaveData.lastSelectedCharacter` + despawn antes de respawn no `PlayerSpawnManager`
- `SealZoneRingVisual` + zonas posicionadas em direção à câmera
- Textos de contrato nos SOs + tooltip mostra `description`
- HUD selamento: "Fique na Área para selar" → "Área selada"
- `EnemyHealthBarDisplay` centralizado no sprite com altura maior

---

| Tarefa | Diagnóstico | Status |
|--------|-------------|--------|
| Selamento sem progresso visual / ratos continuam | Sessões criadas tarde; HUD só no servidor; zonas exigem jogador na área após F | concluído |
| HUD inimigos não atualiza (spawn por buracos) | `EnemiesAlive` só no servidor; clientes sem repoll/ClientRpc | concluído |
| Barras de vida inimigos invisíveis | Barra criada em `Start` (tarde); escala 0.01; `hideWhenFull` | concluído |
| Contrato 2/3 → Fase-1 | Índice -1 na Characters; `ContractCatalog` em Resources | concluído |

### O que mudou (código)

- `ContractCatalog` + `ResolveActiveContractIndex()` — contrato ativo unificado
- `NetworkRatHoleSealManager` — refresh de sessões, broadcast HUD ao selar
- `PhaseObjectiveHud` — repoll + cache de inimigos
- `EnemyHealthBarDisplay` — escala world-space, visível acima do sprite
- Menu: **Add Enemy Health Bars To Prefabs**

---

| Tarefa | Status |
|--------|--------|
| Prompt "Aperte F" — menor vertical, maior horizontal | concluído |
| Barra de progresso no selamento + texto "Buraco Selado" | concluído |
| Desabilitar waves; spawn só por buracos | concluído |
| Win Fase 1: selar todos os buracos + HUD buracos/inimigos | concluído |
| Win Fase 2: carruagem ao fim do percurso | concluído |
| Win Fase 3: matar boss | concluído |
| Fix Contrato 2/3 → Fase-2/3 (ResolveContracts + `ContractSceneResolver`) | concluído |

### Win conditions (runtime)

| Fase | Vitória | Spawn |
|------|---------|-------|
| Fase-1 | Todos buracos selados | Contínuo por buracos não selados |
| Fase-2 | `OnCarriageArrived` | Contínuo por buracos |
| Fase-3 | Morte do boss (`BossEnemyMarker`) | Boss único ao iniciar |

Config: `Assets/Resources/PhaseWaveSettingsCatalog.asset` + `PhaseObjectiveManager`.
HUD: `PhaseObjectiveHud` (substitui `HordeIndicator` quando waves desligadas).

## Estado da implementação (código)

- [x] Scripts de progressão, catálogo de ondas, instalador runtime
- [x] `PhaseSceneSetupEditor` (menu Editor)
- [x] SOs: `RatHoleSealConfig`, `CarriageConfig`, `Fase3`, catálogos em `Resources/`
- [x] `Rato_Boss.prefab`, `Fase-3.unity`, Build Settings, `Contract_2/3`
- [ ] **Executar no Unity:** `MidnightMeow → Phases → Setup All Phase Scenes` (feche Play Mode; se o Editor já estiver aberto, use o menu — batchmode não roda com projeto aberto)
- [ ] **Executar no Unity:** `Register Network Prefabs (Boss + Carriage)` se carruagem ainda não estiver na lista

## Ordem de execução no Editor (após pull)

1. Abrir Unity e aguardar reimport
2. **MidnightMeow/Phases/Setup All Phase Scenes** (configura Fase-1, Fase-2, Fase-3)
3. **MidnightMeow/Phases/Add Enemy Health Bars To Prefabs**
4. Confirmar `DefaultNetworkPrefabs` inclui `Carriage` e `Rato_Boss`
5. Play: Preparação → Contrato 1/2/3 → Characters → Ready → testar mecânicas

## Erros comuns evitados

- Não usar `NightManager` + `NetworkWaveManager` ativos ao mesmo tempo em MP (Editor desativa legado)
- Inimigos MP precisam estar em `DefaultNetworkPrefabs` antes de `Spawn()`
- `RatHoleSpawnPoint.holeId` deve ser único por buraco
- Carruagem precisa tag `Structure` para inimigos atacarem

## Arquivos principais

| Área | Caminho |
|------|---------|
| Instalador runtime | `Assets/Scripts/Multiplayer/Core/PhaseGameplayContentInstaller.cs` |
| Setup Editor | `Assets/Editor/PhaseSceneSetupEditor.cs` |
| Progressão | `Assets/Scripts/Core/Progression/ContractProgressionUtility.cs` |
| Catálogo ondas | `Assets/Scripts/ScriptableObjects/PhaseWaveSettingsCatalog.cs` |
| Boss | `Assets/Prefabs/Enemies/Rato_Boss.prefab` |
| Carruagem | `Assets/Prefabs/Gameplay/Carriage.prefab` |
