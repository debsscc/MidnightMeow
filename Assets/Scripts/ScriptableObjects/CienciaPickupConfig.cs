using UnityEngine;

/// <summary>
/// Configuração de coleta da moeda Ciência (data-driven).
/// </summary>
[CreateAssetMenu(fileName = "CienciaPickupConfig", menuName = "Config/Ciencia Pickup Config")]
public class CienciaPickupConfig : ScriptableObject
{
    [Header("Atração ao jogador")]
    [Tooltip("Raio em que a Ciência passa a se mover em direção ao jogador mais próximo.")]
    public float homingRadius = 4f;

    [Tooltip("Velocidade de aproximação ao jogador (unidades/segundo).")]
    public float homingSpeed = 6f;

    [Tooltip("Intervalo entre buscas do jogador mais próximo (segundos).")]
    public float playerScanInterval = 0.15f;

    [Header("Coleta")]
    [Tooltip("Raio em que a moeda é consumida (servidor). 0 = raio do CircleCollider2D do prefab.")]
    public float collectRadius = 0.55f;
}
