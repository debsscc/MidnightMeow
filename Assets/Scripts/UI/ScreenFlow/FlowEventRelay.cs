using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bloco reutilizável para designers ligarem SFX, VFX e lógica customizada via Inspector.
/// </summary>
[Serializable]
public class FlowEventRelay
{
    [Tooltip("Disparado imediatamente antes da ação (troca de cena, abrir overlay, etc.).")]
    public UnityEvent onBefore;

    [Tooltip("Disparado quando a ação termina com sucesso.")]
    public UnityEvent onAfter;

    [Header("Áudio opcional")]
    public AudioClip clipOnBefore;
    public AudioClip clipOnAfter;

    [Tooltip("Se vazio, usa AudioSource na mesma hierarquia do componente que disparou.")]
    public AudioSource audioSourceOverride;

    public void InvokeBefore(Component context)
    {
        onBefore?.Invoke();
        PlayClip(clipOnBefore, context);
    }

    public void InvokeAfter(Component context)
    {
        onAfter?.Invoke();
        PlayClip(clipOnAfter, context);
    }

    private void PlayClip(AudioClip clip, Component context)
    {
        if (clip == null) return;

        AudioSource source = audioSourceOverride;
        if (source == null && context != null)
            source = context.GetComponentInParent<AudioSource>();

        if (source != null)
            source.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, context != null ? context.transform.position : Vector3.zero);
    }
}
