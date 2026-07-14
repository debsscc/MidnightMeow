Documento de Requisitos: Mecânicas e Personagens
1. Mecânicas Globais do Sistema
1.1 Mecânica de Vida (Regeneração Passiva)
Regra Base: O jogador recupera vida gradualmente ao longo da fase.

Taxa de Regeneração: +2 Pontos de Vida (HP) a cada 5 segundos.

1.2 Mecânica de Reviver (Multiplayer)
Estado de Nocaute (0 HP): Quando um jogador chega a 0 pontos de vida, uma área de colisão circular (Trigger) é instanciada ao redor de seu corpo.

Ação de Reviver: Se o jogador aliado permanecer dentro dessa área circular de forma ininterrupta por 5 segundos, o jogador caído é revivido.

Condição de Derrota (Game Over): Se ambos os jogadores chegarem a 0 pontos de vida, a partida é encerrada e o fluxo de tela retorna para a "Seleção de Fases".

2. Personagens e Habilidades
Nota de Contexto: Os nomes das habilidades listados abaixo são marcadores provisórios (Work In Progress).

Personagem 1: Nix (Foco em Melee/Corpo-a-Corpo)
Ataque Normal (LMB): Ataque corpo-a-corpo frontal (espadada) em trapézio. Causa dano a **todos** os inimigos dentro da zona de colisão.

Passiva: Após o knockback do ataque normal, aplica **Stun** nos inimigos atingidos (duração em `NixPassiveConfig.stunDuration`). O raio do ataque permanece o do ataque normal (sem cleave/área aumentada).

Habilidade 1 - Empurrão (Q): Habilidade de controle de grupo (CC). Aplica força de repulsão (Knockback) aos inimigos e aplica um debuff de lentidão (Slow) com duração de 3 segundos.

Habilidade 2 - Investida (R): Ataque em área direcional. Gera uma zona de dano em formato retangular à frente de Nix, causando dano a todos os inimigos que colidirem com o Hitbox.

Personagem 2: Cora (Foco em Ranged/Distância e Zona)
Ataque Normal (LMB): Ataque à distância (projétil). Dispara penas em direção ao alvo (comportamento balístico similar a um novelo de lã). Destrói ao tocar inimigo/parede.

Passiva: No impacto com inimigo, destrói o projétil e instancia **respingos** (sub-projéteis teleguiados) que perseguem inimigos próximos (`splashCount` / `splashRange` / `splashDamagePercentage` em `CoraPassiveConfig`).

Habilidade 1 - Barreira (Q): Instancia um obstáculo físico.

Comportamento: Bloqueia a passagem/pathfinding de inimigos. Inimigos que tocarem na barreira recebem o status de Atordoamento (Stun).

Exceção de Colisão: Os ataques normais de Cora ignoram o colisor da barreira e passam direto.

Habilidade 2 - Investida (R): Ataque em área (AoE) estático. Instancia uma "poça" circular no chão que aplica dano contínuo (ou instantâneo, a definir) aos inimigos sobrepostos a essa área.

3. Progressão do Jogo
3.1 Desbloqueio por Fases
A arquitetura do jogo deve suportar o bloqueio/desbloqueio de inputs baseado no índice da fase atual:

Fase 1: Estado inicial. Apenas Ataque Normal e Passiva estão habilitados.

Fase 2: Ponto de escolha. O jogador pode optar por desbloquear a Habilidade (Q) OU a Habilidade (R).

Fase 3: Nova escolha de progressão. O jogador pode desbloquear a habilidade pendente OU aprimorar o nível de uma habilidade já desbloqueada.

3.2 Escalamento de Atributos por Nível
Os atributos de range (alcance), dano e efetividade das habilidades escalam em 3 níveis (Tiers):

Escalonamento da Nix:

Nível 1: Ataque (Range Baixo, Dano Alto) | Empurrão (Range Baixo, Slow Baixo) | Investida (Range Médio, Dano Baixo).

Nível 2: Ataque (Range Médio, Dano Alto) | Empurrão (Range Médio, Slow Médio) | Investida (Range Médio, Dano Médio).

Nível 3: Ataque (Range Alto, Dano Alto) | Empurrão (Range Alto, Slow Alto) | Investida (Range Alto, Dano Alto).

Nota: O range/dano da passiva acompanha o nível do Ataque Normal.

Escalonamento da Cora:

Nível 1: Ataque (Range Alto, Dano Baixo) | Barreira (Range Baixo, Stun Baixo) | Investida (Range Médio, Dano Baixo).

Nível 2: Ataque (Range Alto, Dano Médio) | Barreira (Range Médio, Stun Médio) | Investida (Range Médio, Dano Médio).

Nível 3: Ataque (Range Alto, Dano Alto) | Barreira (Range Alto, Stun Alto) | Investida (Range Alto, Dano Alto).

Nota: O range/dano da passiva acompanha o nível do Ataque Normal.

4. Notas de Design e Pendências a Resolver (Backlog)

Condição de Ativação da Passiva: Existe uma anotação de lógica específica: A passiva só é ativada após o jogador matar 5 inimigos seguidos. Uma vez ativa, ela dura 5 segundos. Se acabar, reseta o contador de abates. (Nota para IA: É necessário criar um kill_counter e um passive_timer no script do jogador).

Sistema de Input (Cora R / Q): Como a "poça" e a "barreira" são instanciadas? Elas surgem automaticamente na posição atual do mouse (Raycast da câmera para o chão) ou em frente ao personagem? Elas surgem na posição do mouse, contudo o range máximo deve ser ajustável.

Nomenclatura e Semântica: A habilidade da Nix de "Empurrão" seria realizada com um escudo? Além disso, validar se o nome da habilidade R será mantido como "Investida". Sim a habilidade Empurrão é realizada com o escudo e o nome da habilidade R será mantida como Investida

