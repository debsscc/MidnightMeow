# Shadder de Zona de Ataque
- ~~Zona de ataque no host aparecia quadrado branco na build~~ **Feito:** `TelegraphFill.shader` em Always Included Shaders (`GraphicsSettings`); material `Resources/TelegraphZoneMaterial.mat`; `EnemyTelegraphZoneView` carrega shader via Resources (evita `Shader.Find` falhar no host).
