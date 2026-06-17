

# Reduzir Zoom nos personagens
- ~~Atualmente o zoom nos personagens principais está muito alto~~ **Feito:** `CameraConfig.asset` — `defaultOrthographicSize` 5 → 8; `FollowCamera` lê o mesmo `CameraConfig` (SP e MP unificados).

# Fluxo de Preparação
- ~~Nova proposta de fluxo em fases~~ **Feito:**
  - Host escolhe contrato; cliente observa em tempo real (botões bloqueados para não-host).
  - Botão **"Confirmar Contrato!"** (host) → ambos vão para seleção de personagem (`PreparationSessionManager.RequestConfirmContractRpc`).
  - Na seleção, botão **"Pronto"** trava escolha; quando ambos prontos, contador 5→0 inicia a partida.
  - Reclicar no personagem escolhido **deseleciona** (`TrySetCharacter` toggle).

# Botões de Retorno
- ~~Voltar sem quebrar o ciclo~~ **Feito:** botões "Voltar ao Menu" e "Sair do Lobby" em `PreparationScreenController`; "Sair do Lobby" em `LobbyFlowController`; Characters mantém "Voltar".

# Câmera do Jogador
- ~~Efeito smooth nas bordas~~ **Feito:** `CameraConfig.edgeDeadZoneX/Y` + `edgePanSmoothing`; lógica em `MultiplayerCameraController.ComputeEdgeFollowPosition()`.

# Tutorial - Em análise
- Adicionar balões com "dicas" 
- Chamar a atenção do jogador com efeitos visuais
- Áudio que chama o jogador, narrador...
