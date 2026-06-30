# Tarefas multiplayer pendentes

_Última revisão: 2026-06-30_

## Concluído nesta sessão

- [x] Vitória/derrota em MP: rotas `gameplay_victory` / `gameplay_defeat` carregam `VictoryScene`/`GameOver` localmente (`SinglePlayer`) mantendo Relay ativo
- [x] Transição unificada via `MultiplayerGameManager.BeginEndGameScreenTransitionClientRpc`
- [x] Sincronização visual de ataques/habilidades reforçada em `NetworkPlayerAbilityRelay`

## Verificação manual

- [ ] Host + cliente: selar último buraco (Fase-1) → ambos veem `VictoryScene` sem overlay de “aguardando host”
- [ ] Cliente Cora: Q/R disponíveis desde o spawn; VFX de tiro e habilidades visíveis no peer remoto
- [ ] “Prosseguir” na vitória retorna ao Preparation com lobby intacto
