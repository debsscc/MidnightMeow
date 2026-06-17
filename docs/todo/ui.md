# Tela de Controles - TIME DE ARTE
- Adicionar Tela de controles, para os jogadores saberem quais botões apertar.

# Interface Visual - TIME DE ARTE
- Melhorar a interface do lobby, menu, preparação e escolha de personagens

# Projétil - TIME DE ARTE
- Melhorar a arte dos projéteis

# Feedback Forms
- ~~Deixar o botão de Feedback do Forms maior e mais em evidencia~~ **Feito:** `PlaytestFeedbackButton` no canto inferior esquerdo (menu + gameplay); menu com botão maior (220×80).
- ~~Colocar ele no canto inferior esquerdo da tela e maior~~ **Feito:** ver acima.

# Cooldown das Skills
- ~~Aparecer cooldown restante (Passiva, Q, R, Dash)~~ **Feito:** `PlayerAbilityHud` com timers/fill; tema via `PlayerAbilityHudTheme` SO (placeholders + override); bootstrap em `GameplaySceneBootstrap`.

# Contador Fase 1
- ~~Mostrar Wave atual e ratos restantes~~ **Feito:** `HordeIndicator` escuta `GameEvents.OnWaveStatusChanged`.
- ~~Indicador na borda da direção dos inimigos~~ **Feito:** `OffscreenEnemyIndicator` com setas na borda do canvas.
