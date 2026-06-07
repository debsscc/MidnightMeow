
Documento de Requisitos: Fluxo de Telas e UI (State Machine)
1. Fluxo de Inicialização e Menu Principal
1.1 Tela: Menu Principal
Elementos na Tela:

Botão [Novo Jogo]: Inicia uma nova sessão.

Botão [Continuar]: Retoma uma sessão anterior.

Botão [Sair]: Encerra a aplicação (Application.Quit()).

Regras de Negócio e Transições:

Ao clicar em [Novo Jogo]: Transita para a tela Lobby (Seleção de Modo).

Ao clicar em [Continuar]: Transita diretamente para a tela de Lobby (Aguardando Jogador).

Condição: O botão só fica habilitado/funcional se o jogador já tiver jogado antes e se ele for o Host da partida anterior.

2. Fluxo de Conexão (Lobby & Relay)
2.1 Tela: Lobby (Seleção de Modo)
Elementos na Tela:

Botão [Hostear]: Cria uma sala atuando como Host/Server.

Botão [Entrar]: Acessa o fluxo de Client para entrar em uma sala existente.

Botão [Personagens]: Acessa a tela de gerenciamento de personagens/upgrades.

Transições:

Ao clicar em [Hostear]: Transita para Lobby (Host Aguardando).

Ao clicar em [Entrar]: Transita para Lobby (Client Inserindo Código).

Ao clicar em [Personagens]: Transita para a tela Personagens.

2.2 Tela: Lobby (Host Aguardando)
Elementos na Tela:

Texto [Código da Sala]: Exibe o código de junção (ex: IXZ489) gerado pelo serviço de Relay.

Texto [Status de Jogadores]: Exibe "Jogadores 1/2".

Texto [Feedback]: "Aguardando segundo jogador...".

Transições:

Quando o segundo jogador conectar: Transita automaticamente para a Tela de Carregamento 1.

2.3 Tela: Lobby (Client Inserindo Código)
Elementos na Tela:

Input Field [Insira o código]: Campo de texto para digitar o código do Relay.

Botão [Entrar]: Confirma o código e tenta a conexão.

Transições:

Ao confirmar código válido e conectar: Transita automaticamente para a Tela de Carregamento 1.

3. Fluxo de Preparação e Upgrades
3.1 Tela de Carregamento 1
Elementos: Arte da Nix e Cora.

Transição: Ao terminar de carregar -> Transita para a Tela de Preparação.

3.2 Tela: Preparação (Hub da Partida)
Elementos na Tela:

Cartões de Contrato [Contrato 1], [Contrato 2], [Contrato 3]: Representam missões ou fases disponíveis.

Interação Hover: Ao passar o mouse sobre um contrato, exibe um painel lateral/tooltip com "Descrição da fase, Nível de dificuldade, Recompensas".

Interação Click: Seleciona o contrato (destaque visual vermelho indicando "Selecionado").

Texto [Personagem Selecionado]: Mostra o personagem atualmente escolhido pelo jogador (ex: "Personagem: Nix").

Botão [Escolher Personagem]: Abre a tela de Personagens para seleção e upgrade.

Botão [Pronto]: Confirma que o jogador está preparado.

Transições:

Ao clicar em [Escolher Personagem]: Transita para a tela Personagens.

Quando OS DOIS jogadores clicarem em [Pronto]: Transita para a Tela de Carregamento 2.

3.3 Tela: Personagens (Upgrades e Seleção)
Elementos na Tela:

Contador [Magículas]: Exibe a moeda atual do jogador no canto superior direito (ex: "Magículas: 2").

Abas/Painéis de Personagem [Nix] e [Cora]:

Abaixo de cada personagem, há botões para [Skill 1], [Skill 2] e [Skill 3].

Abaixo de cada Skill, há indicadores visuais (níveis) do progresso da habilidade.

Painel de Detalhes da Skill (Aparece ao clicar em uma skill):

Nome da Skill (ex: "Skill 1: Empurrão").

Descrição.

Botão [Upgrade (X magículas)]: Consome a moeda para subir o nível da habilidade.

Botão [Voltar]: Retorna à tela anterior.

Regras de Negócio e Transições:

Lógica de Seleção de Personagem: Clicar no painel do personagem (Nix ou Cora) o define como o personagem jogável para a próxima fase.

Exceção Importante: Se o jogador tiver acessado esta tela a partir do Menu Principal/Lobby (passo 2.1), clicar no personagem não faz nada (apenas upgrades são permitidos, a seleção de personagem não é salva para uma partida que ainda não começou).

Ao clicar em [Voltar]: Transita de volta para a tela de origem (Lobby ou Tela de Preparação).

4. Loop de Gameplay
4.1 Tela de Carregamento 2
Transição: Ao terminar de carregar -> Transita para a Gameplay.

4.2 Gameplay
Estado do Jogo: Ação em andamento (mecânicas de vida, reviver, combate detalhadas no documento anterior).

Transição: Ao finalizar a gameplay (vitória ou derrota) -> Transita de volta para a Tela de Preparação (Passo 3.2), reiniciando o ciclo de contratos e upgrades.