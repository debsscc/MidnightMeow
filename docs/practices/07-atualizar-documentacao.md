# Atualização de documentação

## Objetivo

Documentação desatualizada é pior que ausência: agentes e humanos tomam decisões erradas.

## O que atualizar e quando

| Alteração | Atualizar |
|-----------|-----------|
| Campo novo em prefab | `docs/editor/prefabs/<Nome>.md` |
| Nova cena de produção | `docs/editor/scenes.md` |
| Novo SO ou pasta em `Data/` | `docs/assets/STRUCTURE.md` ou prefab doc |
| Novo evento global | `docs/practices/02-event-driven.md` (lista) + comentário em `GameEvents.cs` |
| Movimentação de pasta | `docs/assets/STRUCTURE.md` + `08-movimentacao-arquivos.md` se mudar processo |
| Tags/Layers no Project Settings | `docs/editor/project-context.md` e `AGENTS.md` (bloco Code Assist) |

## Processo mínimo

1. Implementar mudança.
2. Abrir markdown atrelado (buscar por nome do prefab/script).
3. Ajustar tabela de componentes, referências SO e notas.
4. No cabeçalho do markdown, linha `Última revisão: AAAA-MM-DD`.

## Decisões de design não óbvias

Quando o pedido não detalhar comportamento (ex.: posição da carruagem, HP do boss, flag de teste), documente a decisão em:

- `docs/todo/<tarefa>.md` — tabela **Decisões de design**
- Ou seção **Notas** no markdown do prefab/cena afetado

Isso evita caixa preta para quem testa no Editor.

## Para agentes de IA

Inclua no resumo da tarefa: **“Docs atualizados: …”** com caminhos. Se não houver doc do prefab, crie a partir de `docs/editor/_template-prefab.md`.
