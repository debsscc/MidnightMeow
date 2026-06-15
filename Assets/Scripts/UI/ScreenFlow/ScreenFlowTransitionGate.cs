using System.Collections;
using UnityEngine;

/// <summary>
/// Evita pedidos de rota enquanto fade/transição anterior ou lock do orquestrador ainda está ativo.
/// </summary>
public static class ScreenFlowTransitionGate
{
    public static bool CanRequestNow()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.IsTransitioning)
            return false;

        GameFlowOrchestrator orchestrator = GameFlowOrchestrator.Instance;
        if (orchestrator != null && !orchestrator.CanRequestTransition())
            return false;

        return true;
    }

    public static IEnumerator WaitUntilReady(float timeoutSeconds = 5f)
    {
        float timer = 0f;
        while (!CanRequestNow() && timer < timeoutSeconds)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
