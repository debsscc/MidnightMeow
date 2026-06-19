/// <summary>
/// CameraConfig.cs
/// ScriptableObject com todas as configurações ajustáveis do sistema de câmera multiplayer.
/// Segue o padrão Data-Driven: nenhum valor fica hardcoded nos controladores.
/// Permite que designers configurem zoom, shake e comportamento de cutscene sem alterar código.
/// Caminho de criação: Assets > Create > Scriptable Objects > Camera > CameraConfig
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "Scriptable Objects/Camera/CameraConfig")]
public class CameraConfig : ScriptableObject
{
    [Header("Seguimento do Jogador")]
    [Tooltip("Tamanho ortográfico padrão da câmera (distância do jogador).")]
    [Range(3f, 30f)]
    public float defaultOrthographicSize = 8f;

    [Tooltip("Velocidade de suavização horizontal do seguimento (damping X do Cinemachine).")]
    [Range(0f, 5f)]
    public float followDampingX = 0.2f;

    [Tooltip("Velocidade de suavização vertical do seguimento (damping Y do Cinemachine).")]
    [Range(0f, 5f)]
    public float followDampingY = 0.2f;

    [Tooltip("Offset fixo da câmera em relação ao jogador (apenas Z importa em 2D top-down).")]
    public Vector3 followOffset = new Vector3(0f, 0f, -10f);

    [Tooltip("Lookahead em segundos — câmera se adianta na direção do movimento.")]
    [Range(0f, 2f)]
    public float lookaheadTime = 0.3f;

    [Tooltip("Suavização do lookahead.")]
    [Range(0f, 1f)]
    public float lookaheadSmoothing = 0.5f;

    [Header("Zoom")]
    [Tooltip("Tamanho ortográfico mínimo (máximo zoom in).")]
    [Range(2f, 10f)]
    public float minOrthographicSize = 4f;

    [Tooltip("Tamanho ortográfico máximo (máximo zoom out).")]
    [Range(5f, 30f)]
    public float maxOrthographicSize = 15f;

    [Tooltip("Velocidade de interpolação do zoom quando alterado por código.")]
    [Range(1f, 20f)]
    public float zoomLerpSpeed = 5f;

    [Header("Zoom Inicial da Fase")]
    [Tooltip("Zoom in suave ao entrar em Fase-1/Fase-2 (como o FollowCamera legado).")]
    public bool playIntroZoom = true;

    [Tooltip("Tamanho ortográfico extra no frame 0 (mais aberto); anima até defaultOrthographicSize.")]
    [Range(0f, 8f)]
    public float introZoomInAmount = 2f;

    [Tooltip("Duração do zoom in inicial em segundos.")]
    [Range(0.1f, 5f)]
    public float introZoomDuration = 2.5f;

    [Header("Shake — Preset: Leve")]
    [Tooltip("Intensidade do shake leve (dano pequeno, UI feedback).")]
    [Range(0f, 1f)]
    public float shakeLightIntensity = 0.08f;

    [Tooltip("Duração em segundos do shake leve.")]
    [Range(0f, 1f)]
    public float shakeLightDuration = 0.12f;

    [Header("Shake — Preset: Médio")]
    [Tooltip("Intensidade do shake médio (dano significativo, explosão próxima).")]
    [Range(0f, 1f)]
    public float shakeMediumIntensity = 0.2f;

    [Tooltip("Duração em segundos do shake médio.")]
    [Range(0f, 1f)]
    public float shakeMediumDuration = 0.25f;

    [Header("Shake — Preset: Pesado")]
    [Tooltip("Intensidade do shake pesado (morte, explosão grande, boss ability).")]
    [Range(0f, 2f)]
    public float shakeHeavyIntensity = 0.45f;

    [Tooltip("Duração em segundos do shake pesado.")]
    [Range(0f, 1f)]
    public float shakeHeavyDuration = 0.5f;

    [Header("Cutscene")]
    [Tooltip("Velocidade de deslocamento da câmera ao se mover para um ponto de cutscene.")]
    [Range(1f, 20f)]
    public float cutscenePanSpeed = 6f;

    [Tooltip("Velocidade de retorno da câmera ao jogador após a cutscene.")]
    [Range(1f, 20f)]
    public float cutsceneReturnSpeed = 10f;

    [Tooltip("Curva de animação da câmera durante o pan de cutscene.")]
    public AnimationCurve cutscenePanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Tempo de espera em segundos no destino antes de retornar ao jogador.")]
    [Range(0f, 10f)]
    public float cutsceneHoldDuration = 1.5f;

    [Header("Efeitos de Pós-processamento (Futuro)")]
    [Tooltip("Cor de vinheta ao tomar dano (alpha = intensidade). Requer URP Volume.")]
    public Color damageVignetteColor = new Color(0.8f, 0f, 0f, 0.4f);

    [Tooltip("Duração do flash de dano em segundos.")]
    [Range(0f, 1f)]
    public float damageFlashDuration = 0.2f;

    [Header("Dead Zone — Pan suave nas bordas")]
    [Tooltip("Fração do viewport (0–0.5) reservada ao centro. Valores MAIORES fazem a câmera reagir antes (jogador não precisa encostar na borda).")]
    [Range(0f, 0.45f)]
    public float edgeDeadZoneX = 0.42f;

    [Tooltip("Fração vertical do viewport (0–0.5) reservada ao centro. Valores MAIORES = pan nas bordas mais cedo.")]
    [Range(0f, 0.45f)]
    public float edgeDeadZoneY = 0.40f;

    [Tooltip("Suavização do deslocamento da câmera ao sair da dead zone. Valores maiores = resposta mais rápida.")]
    [Range(1f, 35f)]
    public float edgePanSmoothing = 28f;
}

/// <summary>
/// Enumeração dos presets de shake disponíveis.
/// Permite selecionar intensidade/duração sem precisar passar valores manuais.
/// </summary>
public enum CameraShakePreset
{
    Light,
    Medium,
    Heavy
}
