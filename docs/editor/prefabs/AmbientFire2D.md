# Prefab: AmbientFire2D

Última revisão: 2026-07-14  
**Caminho:** `Assets/Prefabs/VFX/AmbientFire2D.prefab`

## Resumo

Fogo ambiente **2D** em forma de **chama de vela** (gota + glow + faíscas leves). Feito pra velas/tochas da Fase-3.

## Como usar

1. Abra `Assets/Scenes/Fases/Fase-3.unity`
2. Arraste `Assets/Prefabs/VFX/AmbientFire2D.prefab` pra Hierarchy
3. Posicione no **pavio** da vela
4. No Inspector:
   - **Size Preset:** `Candle` (padrão) / `Torch` / `Bonfire`
   - **Size Multiplier** — afinar pro tamanho da arte
   - **Sorting Order** — se ficar atrás do prop

## Notas

- Visual = sprite procedural em gota (núcleo claro + borda laranja), não chuva de partículas.
- Em Play Mode as camadas são recriadas em runtime.
- Script: `Assets/Scripts/VFX/AmbientFire2D.cs`
