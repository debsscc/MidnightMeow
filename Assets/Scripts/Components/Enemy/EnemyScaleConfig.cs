///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que aplica a escala configurável do inimigo baseada no EnemyStats.
// ---------------------------------------------------------------- */

using UnityEngine;

public class EnemyScaleConfig : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private void Awake()
    {
        if (stats != null)
        {
            transform.localScale = Vector3.one * stats.scale;
        }
    }
}
