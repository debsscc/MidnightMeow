using UnityEngine;

[CreateAssetMenu(fileName = "TelegraphVisualStyle", menuName = "MidnightMeow/Combat/Telegraph Visual Style")]
public class EnemyTelegraphVisualStyle : ScriptableObject
{
    [Header("Cores")]
    [Tooltip("Interior estático da zona (amarelo) até o preenchimento chegar.")]
    public Color backgroundColor = new Color(1f, 0.92f, 0.22f, 0.55f);
    [Tooltip("Preenchimento que cresce do centro para a borda (vermelho).")]
    public Color fillColor = new Color(0.9f, 0.12f, 0.08f, 0.85f);
    [Tooltip("Anel vermelho na borda externa da zona.")]
    public Color outlineColor = new Color(0.95f, 0.15f, 0.1f, 1f);

    [Header("Shader")]
    [Range(0.01f, 0.25f)] public float outlineWidth = 0.06f;
    [Tooltip("Sorting order do sprite de telegraph (acima do chão).")]
    public int sortingOrder = 50;
}
