# Prefabs: Variantes de Rato

Última revisão: 2026-05-22

| Prefab | Caminho | Uso esperado |
|--------|---------|--------------|
| Rato_Padrao_Base | `Assets/Prefabs/Enemies/Rato_Padrao_Base.prefab` | Inimigo padrão |
| Rato_Padrao_Veloz | `Assets/Prefabs/Enemies/Rato_Padrao_Veloz.prefab` | Mais velocidade |
| Rato_Padrao_Resistente | `Assets/Prefabs/Enemies/Rato_Padrao_Resistente.prefab` | Mais vida |
| Rato_Eletrico | `Assets/Prefabs/Enemies/Rato_Eletrico.prefab` | Tipo elétrico |
| Rato_Acido | `Assets/Prefabs/Enemies/Rato_Acido.prefab` | Tipo ácido |
| Enemy 1 | `Assets/Prefabs/Enemies/Enemy 1.prefab` | *(legado ou variante?)* |

## Estrutura comum (confirmar)

Todos devem referenciar um asset **`EnemyStats`** diferente em `Assets/Data/Stats/Enemies/`.

## Tabela para preencher no Editor

| Prefab | EnemyStats asset | Speed | HP | Projétil | Notas |
|--------|------------------|-------|-----|----------|-------|
| Rato_Padrao_Base | | | | | |
| Rato_Padrao_Veloz | | | | | |
| Rato_Padrao_Resistente | | | | | |
| Rato_Eletrico | | | | | |
| Rato_Acido | | | | | |
| Enemy 1 | | | | | |

## WaveSystem

Listar quais entradas em `WaveSettings` / `NetworkWaveManager` usam cada prefab.
