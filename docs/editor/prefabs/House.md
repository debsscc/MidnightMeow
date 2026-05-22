# Prefab: House

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Environment/House.prefab`

## Resumo

Estrutura defendida (casa) — alvo dos ratos. Tag `Structure`.

## Scripts

| Script | Função |
|--------|--------|
| `HealthComponent` | Vida da casa |
| `HouseController` | Estado, dano, game over? |
| `VFXEmitter` / `VXFEmitter` | Efeitos *(verificar nome no Inspector)* |
| `NavMeshModifier` | NavMesh 2D |

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| Tag | `Structure` | |
| maxHealth | SO ou serialize | |
| Colliders | Hitbox vs ambiente | |
| Referência em `EnemyTargetFinder` | Prioridade alvo | |
| Sons de dano | Clips em `Assets/Audio/` | |
