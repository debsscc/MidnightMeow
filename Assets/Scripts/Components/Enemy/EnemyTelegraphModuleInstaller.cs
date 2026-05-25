using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Instala telegraph de ataque no prefab do inimigo e atribui o pattern (por variante).
/// </summary>
[DefaultExecutionOrder(-150)]
public class EnemyTelegraphModuleInstaller : MonoBehaviour
{
    [SerializeField] private EnemyAttackPatternDefinition attackPattern;
    [SerializeField] private EnemyTelegraphVisualStyle visualStyle;
    [SerializeField] private Transform attackOrigin;

    private void Awake()
    {
        if (attackPattern == null) return;

        if (GetComponent<EnemyTelegraphZoneFactory>() == null)
            gameObject.AddComponent<EnemyTelegraphZoneFactory>();

        if (GetComponent<NetworkObject>() != null && GetComponent<NetworkEnemyTelegraphRelay>() == null)
            gameObject.AddComponent<NetworkEnemyTelegraphRelay>();

        var attacker = GetComponent<EnemyTelegraphedAttacker>();
        if (attacker == null)
            attacker = gameObject.AddComponent<EnemyTelegraphedAttacker>();

        attacker.ConfigureFromInstaller(attackPattern, visualStyle, attackOrigin);
    }
}
