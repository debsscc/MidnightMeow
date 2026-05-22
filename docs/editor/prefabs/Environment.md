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

| Componente | Função |
|------------|--------|
| Collider2D | Bloqueio |
| `NavMeshModifier` | Corta / marca navmesh |

| Confirmar | Valor atual |
|-------------|-------------|
| Layer | `Wall` |
| Composite collider? | |

---

## Walkable.prefab

**Caminho:** `Assets/Prefabs/Environment/Walkable.prefab`

| Componente | Função |
|------------|--------|
| `NavMeshModifier` | Área caminhável |

| Confirmar | Valor atual |
|-------------|-------------|
| Area type / override | |
