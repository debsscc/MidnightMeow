///* ----------------------------------------------------------------
// DESCRIÇÃO: Controlador de áudio do inimigo. 
// Ouve eventos de dano e morte para emitir feedback sonoro.
// ---------------------------------------------------------------- */

using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemyAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Referência ao componente de vida para escutar OnTakeDamage e OnDied")]
    [SerializeField] private HealthComponent healthComponent;

    [Header("Audio Source")]
    [Tooltip("AudioSource para sons gerais (dano, rugidos, etc.)")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private void Awake()
    {
        if (healthComponent == null) healthComponent = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            // Usa UnityEvent listeners (OnTakeDamage é UnityEvent<float, GameObject>)
            healthComponent.OnTakeDamage.AddListener(HandleTakeDamage);
            healthComponent.OnDied.AddListener(HandleDied);
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.RemoveListener(HandleTakeDamage);
            healthComponent.OnDied.RemoveListener(HandleDied);
        }
    }

    private void HandleTakeDamage()
    {
        if (audioSource != null && damageClip != null)
        {
            audioSource.PlayOneShot(damageClip, volume);
        }
    }

    private void HandleDied()
    {
        if (deathClip != null)
        {
            // Cria um emissor de áudio temporário na posição atual.
            // Garante que o som de morte não seja cortado se este GameObject for destruído em seguida.
            AudioSource.PlayClipAtPoint(deathClip, transform.position, volume);
        }
    }
}