// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: Por enquanto só faz o botão tremer se clica.
// ---------------------------------------------------------------- */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Lock_Button : MonoBehaviour, IPointerDownHandler
{
    [Header("Shake Settings")]
    [SerializeField] private bool shakeOnDisabledClick = true;
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private int shakeVibrations = 5;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip errorSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

    private Vector3 originalPosition;
    private Button button;
    private bool isShaking = false;
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalPosition = transform.localPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Se o botão não estiver interagível E a opção estiver ativa, treme
        if (!button.interactable && shakeOnDisabledClick)
        {
            Shake();
        }
    }
    public void Shake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Intensidade decrescente (começa forte e vai diminuindo)
            float strength = Mathf.Lerp(shakeIntensity, 0, elapsed / shakeDuration);

            float offsetX = Random.Range(-strength, strength);
            float offsetY = Random.Range(-strength, strength);

            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isShaking = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        transform.localPosition = originalPosition;
        isShaking = false;
    }
}