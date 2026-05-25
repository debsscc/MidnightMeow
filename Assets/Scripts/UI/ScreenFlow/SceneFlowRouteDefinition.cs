using UnityEngine;

/// <summary>
/// Uma rota nomeada (evento → cena). Designers criam assets e referenciam em botões ou missões.
/// </summary>
[CreateAssetMenu(fileName = "Route_", menuName = "MidnightMeow/Screen Flow/Scene Route")]
public class SceneFlowRouteDefinition : ScriptableObject
{
    [Tooltip("ID único. Ex.: menu_lobby. Use SceneFlowRouteIds para valores padrão.")]
    public string routeId;

    [Tooltip("Nome da cena no Build Settings.")]
    public string sceneName;

    public ScreenTransitionMode transitionMode = ScreenTransitionMode.Fade;
    public SceneLoadKind loadKind = SceneLoadKind.SinglePlayer;

    [Tooltip("Fade-out/in quando o modo usa Fade ou LoadingScreen.")]
    [Min(0f)] public float fadeTime = 1f;

    [Tooltip("Tempo mínimo com tela de loading visível (modo LoadingScreen).")]
    [Min(0f)] public float minLoadingTime = 2f;
}
