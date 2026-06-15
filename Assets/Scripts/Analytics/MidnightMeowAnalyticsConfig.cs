using UnityEngine;

/// <summary>
/// Chaves e opções do GameAnalytics para MidnightMeow.
/// Arraste em <see cref="MidnightMeowAnalyticsBootstrap"/> ou deixe em Resources/.
/// </summary>
[CreateAssetMenu(menuName = "MidnightMeow/Analytics Config", fileName = "MidnightMeowAnalyticsConfig")]
public class MidnightMeowAnalyticsConfig : ScriptableObject
{
    [Header("GameAnalytics — portal: https://tool.gameanalytics.com")]
    [Tooltip("Game Key do dashboard (Settings → Setup).")]
    public string gameKey = string.Empty;

    [Tooltip("Secret Key do dashboard (Settings → Setup).")]
    public string secretKey = string.Empty;

    [Header("Comportamento")]
    [Tooltip("Se falso, não inicializa no Editor (GA só envia de build mesmo assim).")]
    public bool enableInEditor = true;

    [Tooltip("Loga eventos no Console para debug local.")]
    public bool logEventsInConsole = true;
}
