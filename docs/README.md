# MidnightMeow — Base de Conhecimento

Documentação para desenvolvedores e **agentes de IA** que trabalham neste projeto Unity.

## Projeto

| Item | Valor |
|------|--------|
| Nome | MidnightMeow |
| Unity | 6000.3.13f1 |
| Gênero | Survivor / horde (gato vs ratos), com modo multiplayer |

## Índice

### Práticas de arquitetura

1. [Data-driven](practices/01-data-driven.md) — ScriptableObjects, sem números mágicos
2. [Event-driven](practices/02-event-driven.md) — Baixo acoplamento via eventos
3. [Orientação a objetos](practices/03-orientacao-objetos.md) — POO no gameplay
4. [Single Responsibility](practices/04-single-responsibility.md) — Uma responsabilidade por classe
5. [Documentação de código](practices/05-documentacao-codigo.md) — Cabeçalhos, XML docs, Tooltips
6. [Testes unitários](practices/06-testes-unitarios.md) — NUnit e critérios de cobertura
7. [Manter docs atualizadas](practices/07-atualizar-documentacao.md) — Obrigatório em cada alteração
8. [Movimentação de arquivos](practices/08-movimentacao-arquivos.md) — `.meta` sempre junto
9. [Otimização de rede](practices/09-otimizacao-rede.md) — NGO, bandwidth, RPCs, Relay
10. [Artes e visual](practices/10-artes-e-visual.md) — Pipeline para artistas no Editor
11. [Perfis de personagem e animação](practices/11-character-profiles-animation.md) — GameplayProfile + AnimatorOverride via SO

### Combate

- [Habilidades de personagem (design)](combat/character-abilities.md) — Nix / Cora, tiers, passivas
- [Habilidades de personagem (implementação)](combat/character-abilities-implementation.md) — arquitetura, rede, setup
- [Ataques inimigos com telegraph](combat/enemy-telegraph-attacks.md) — zonas preenchíveis (estilo Hades), SOs, rede

### Multiplayer

- [Reviver por zona](multiplayer/revive-zone.md) — área ao redor do jogador caído (sem botão Interact)

### Gameplay (fases)

- [Selamento de buracos](gameplay/rat-hole-sealing.md)
- [Carruagem (Fase 2)](gameplay/carriage.md)
- [Boss (Fase 3)](gameplay/boss-phase.md)
- [Guia: setup Rei Rato (Editor)](editor/guides/rat-king-boss-setup.md)
- [Guia: Rei Rato anti-tunneling (fuga/dash)](editor/guides/rat-king-anti-tunneling.md)
- [Guia: escolta carruagem + aggro + telegraph Structure](editor/guides/carriage-phase2-aggro-setup.md)
- [Guia: conserto carruagem (fix E / zonas)](editor/guides/carriage-repair-fix.md)
- [Plano de implementação Fases 1–3](todo/phases-implementation.md)

### Editor (para agentes sem acesso ao Unity)

- [Contexto do projeto](editor/project-context.md) — Tags, layers, pacotes
- [Cenas](editor/scenes.md) — Cenas de produção e bootstrap
- [Diagnóstico modular](editor/diagnostics.md) — Hub de logs para combate/rede
- [Prefabs](editor/README.md) — Inventário e template de documentação
- [Guia: passiva Nix stun + Cora splash](editor/guides/nixie-cora-passive-refactor.md) — setup Editor pós-refatoração
- [Guia: knockback anti-tunneling (inimigos)](editor/guides/enemy-knockback-anti-tunneling.md) — Rigidbody Continuous + setup paredes
- [Guia: limite Rat Holes + Poça Cora](editor/guides/rat-holes-and-cora-puddle-balance.md) — `maxRatsAlive`, `castRange` / `puddleRadius`
- [Estrutura de Assets](assets/STRUCTURE.md) — Pastas e convenções
- [Erros comuns](troubleshooting/common-errors.md) — Console/compilador: causas e correções

### Raiz do repositório

- [AGENTS.md](../AGENTS.md) — Instruções rápidas injetadas pelo Unity Code Assist (estado do editor)

## Regra de ouro para agentes

> Antes de alterar código, prefab ou cena: leia a doc atrelada. Depois de alterar: **atualize a doc no mesmo PR/commit**.

## Checklist rápido (PR / tarefa)

- [ ] Valores de balanceamento em ScriptableObjects (`Assets/Data/`)
- [ ] Comunicação entre sistemas via eventos (`GameEvents`, UnityEvents ou SO events)
- [ ] Classe com responsabilidade única e nome claro
- [ ] Cabeçalho + XML docs + `[Tooltip]` em campos serializados
- [ ] Testes NUnit adicionados ou atualizados
- [ ] Markdown de prefab/cena atualizado em `docs/editor/`
- [ ] Arquivos movidos com `.meta` correspondente
- [ ] Alteração multiplayer revisada contra [09-otimizacao-rede](practices/09-otimizacao-rede.md)
- [ ] Arte nova em `Assets/Art/` conforme [10-artes-e-visual](practices/10-artes-e-visual.md)
