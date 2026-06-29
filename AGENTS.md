# MidnightMeow — instruções para agentes

## Base de conhecimento (ler primeiro)

Documentação completa em **[docs/README.md](docs/README.md)**:

- Práticas: data-driven, eventos, SRP, testes, movimentação de `.meta`, rede, artes
- Editor: prefabs, cenas, tags/layers em [docs/editor/project-context.md](docs/editor/project-context.md)
- Estrutura de pastas: [docs/assets/STRUCTURE.md](docs/assets/STRUCTURE.md)
- Erros comuns do Console: [docs/troubleshooting/common-errors.md](docs/troubleshooting/common-errors.md)

**Regra:** ao alterar prefab, cena ou SO, atualize o markdown correspondente em `docs/editor/`.

## Estrutura rápida

| Área | Caminho |
|------|---------|
| Código C# | `Assets/Scripts/` |
| Dados (SO) | `Assets/Data/` |
| Prefabs | `Assets/Prefabs/{Characters,Enemies,Combat,...}/` |
| Arte | `Assets/Art/` |
| Testes | `Assets/Tests/` |
| Sandbox | `Assets/_Sandbox/` (não produção) |

<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: MidnightMeow
- Unity version: Unity 6000.3.13f1
- Active scene:
  - Name: Menu2
  - Tags:
    - Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController, Structure, Enemy, Drop
  - Layers:
    - Default, TransparentFX, Ignore Raycast, Player, Water, UI, Wall, Projectile, ProjectileEnemy, Structure, Enemy, Collectable, DashableWall, Shadow
- Active game object:
  - Name: BookmartsButtons
  - Tag: Untagged
  - Layer: UI
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->