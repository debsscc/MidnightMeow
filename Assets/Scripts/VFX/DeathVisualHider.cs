using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Esconde apresentação de morte: desativa visual root quando possível;
/// em objetos de rede (sprite no mesmo GO), desativa filhos visuais e componentes de render.
/// </summary>
public static class DeathVisualHider
{
    public static void Hide(Transform owner, Transform visualRoot = null)
    {
        if (owner == null)
            return;

        if (visualRoot != null && CanDeactivateWholeObject(visualRoot))
            visualRoot.gameObject.SetActive(false);

        HideAllSpriteRenderers(owner, visualRoot);
        HideAllCanvases(owner, visualRoot);
        DisableAnimators(owner);
    }

    private static void HideAllSpriteRenderers(Transform owner, Transform visualRoot)
    {
        SpriteRenderer[] renderers = owner.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (visualRoot != null
                && visualRoot.gameObject.activeSelf == false
                && (renderer.transform == visualRoot || renderer.transform.IsChildOf(visualRoot)))
                continue;

            renderer.sprite = null;
            renderer.forceRenderingOff = true;
            renderer.enabled = false;

            if (renderer.transform != owner && CanDeactivateWholeObject(renderer.transform))
                renderer.gameObject.SetActive(false);
        }
    }

    private static void HideAllCanvases(Transform owner, Transform visualRoot)
    {
        Canvas[] canvases = owner.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            if (visualRoot != null
                && visualRoot.gameObject.activeSelf == false
                && (canvas.transform == visualRoot || canvas.transform.IsChildOf(visualRoot)))
                continue;

            canvas.enabled = false;
        }
    }

    private static void DisableAnimators(Transform owner)
    {
        Animator[] animators = owner.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
                continue;

            animator.speed = 0f;
            animator.enabled = false;
        }
    }

    private static bool CanDeactivateWholeObject(Transform target)
    {
        if (target.GetComponent<NetworkObject>() != null)
            return false;

        if (target.GetComponent<NetworkBehaviour>() != null)
            return false;

        return true;
    }

}
