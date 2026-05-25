using UnityEngine;

/// <summary>
/// Exemplo de listener para VFX/SFX via <see cref="EnemyTelegraphEvents"/>.
/// Adicione na cena ou no prefab de managers.
/// </summary>
public class EnemyTelegraphFeedbackListener : MonoBehaviour
{
    [SerializeField] private AudioClip telegraphCompleteClip;
    [SerializeField] private AudioClip damageResolvedClip;

    private void OnEnable()
    {
        EnemyTelegraphEvents.OnTelegraphFillComplete += HandleFillComplete;
        EnemyTelegraphEvents.OnTelegraphResolved += HandleResolved;
    }

    private void OnDisable()
    {
        EnemyTelegraphEvents.OnTelegraphFillComplete -= HandleFillComplete;
        EnemyTelegraphEvents.OnTelegraphResolved -= HandleResolved;
    }

    private void HandleFillComplete(TelegraphEventData data)
    {
        if (telegraphCompleteClip != null)
            AudioSource.PlayClipAtPoint(telegraphCompleteClip, data.WorldPosition);
    }

    private void HandleResolved(TelegraphResolvedEventData data)
    {
        if (damageResolvedClip != null && data.TargetsHit > 0)
            AudioSource.PlayClipAtPoint(damageResolvedClip, data.Telegraph.WorldPosition);
    }
}
