using UnityEngine;

/// <summary>
/// Materiais de combate carregados via Resources (build-safe; evita Shader.Find stripped).
/// </summary>
public static class CombatVisualMaterials
{
    public static Material CreateAbilityZoneFillInstance()
    {
        Material template = Resources.Load<Material>("AbilityZoneFillMaterial");
        if (template != null)
            return new Material(template);

        Shader shader = Shader.Find("MidnightMeow/AbilityZoneFill")
                        ?? Shader.Find("MidnightMeow/TelegraphFill")
                        ?? Shader.Find("Sprites/Default");
        return new Material(shader);
    }

    public static Material CreateMeleeHitWaveInstance()
    {
        Material template = Resources.Load<Material>("MeleeHitWaveMaterial");
        if (template != null)
            return new Material(template);

        Shader shader = Shader.Find("MidnightMeow/MeleeHitWave")
                        ?? Shader.Find("Sprites/Default");
        return new Material(shader);
    }
}
