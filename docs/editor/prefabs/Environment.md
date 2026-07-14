# Prefabs: Ambiente (NavMesh, paredes)

Última revisão: 2026-05-22

## NavMesh.prefab

**Caminho:** `Assets/Prefabs/Environment/NavMesh.prefab`

| Componente | Função |
|------------|--------|
| `NavMeshSurface` (NavMeshPlus) | Bake 2D |
| `CollectSources2d` | Coleta geometria 2D |

| Confirmar no Editor | Valor atual |
|---------------------|-------------|
| Agent Type | |
| Default area | |
| Objeto pai na cena Fase-1 | |

---

## NavMesh Surface.prefab

**Caminho:** `Assets/Prefabs/Environment/NavMesh Surface.prefab`

| Componente | Função |
|------------|--------|
| `NavMeshSurface` (Unity AI Navigation) | Superfície alternativa |

*(Documentar quando usar vs NavMesh.prefab)*

---

## Wall.prefab

**Caminho:** `Assets/Prefabs/Environment/Wall.prefab`

| Propriedade | Valor YAML |
|-------------|------------|
| Layer | **Wall** (6) |
| Tag | Untagged |

| Componente | Função |
|------------|--------|
| Collider2D | Bloqueio |
| `NavMeshModifier` | Corta / marca navmesh |

| Confirmar | Valor atual |
|-------------|-------------|
| Layer | `Wall` |
| Composite collider? | |

---

## Props com profundidade (flor, pedra, árvore)

Colisão **não** controla quem fica na frente. Players/inimigos recalculam `sortingOrder` pelo Y dos pés; props estáticos ficam com order fixo (geralmente 0) → sempre atrás ou sempre na frente.

**Fix:** no prefab/objeto do prop:

1. `CircleCollider2D` na base (layer `Wall`)
2. Add Component → **`StaticSpriteYSort`** (mesma fórmula: `5000 - Y*100`)

O personagem passa **atrás** da flor se os pés estiverem “mais baixo” na tela (Y maior no mundo top-down do projeto) e **na frente** no caso contrário.

---

## Walkable.prefab

**Caminho:** `Assets/Prefabs/Environment/Walkable.prefab`

| Componente | Função |
|------------|--------|
| `NavMeshModifier` | Área caminhável |

| Confirmar | Valor atual |
|-------------|-------------|
| Area type / override | |
