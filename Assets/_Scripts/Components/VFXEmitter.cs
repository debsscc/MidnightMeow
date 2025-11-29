// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-09 19:05
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/

using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class VXFEmitter : MonoBehaviour
{
    [Header("Shader Settings")]
    public SpriteRenderer targetRenderer;
    public string shaderParameter = "_Intensity";
    public float maxIntensity = 1f;
    public float fadeSpeed = 1f;
    public float intensityDuration = 0.5f;

    private bool fadingOut = false;
    private float durationTimer = 0f;

    [Header("Particle Settings")]
    public bool playParticles = true;
    public float destroyDelay = 2f;
    public float particleDuration = 1f;
    public bool destroyAfter = false;
    public bool stopParticlesOnFade = true;

    [Header("Debug Autoplay")]
    public bool playOnStart = false;

    private ParticleSystem particles;
    private Material materialInstance;
    private float currentValue = 0f;
    private bool animating = false;

    // --------------------------
    //  Lifecycle
    // --------------------------

    void Awake()
    {
        particles = GetComponent<ParticleSystem>();

        if (targetRenderer != null)
        {
            // Cria uma cópia do material pra não alterar o original
            materialInstance = Instantiate(targetRenderer.material);
            targetRenderer.material = materialInstance;
        }
    }

    void Start()
    {
        if (playOnStart)
            PlayVFX();
    }

    void Update()
    {
        if (materialInstance != null && animating)
        {
            if (!fadingOut)
            {
                // Aumenta intensidade
                currentValue = Mathf.MoveTowards(currentValue, maxIntensity, Time.deltaTime * fadeSpeed);

                if (Mathf.Approximately(currentValue, maxIntensity))
                {
                    fadingOut = true;
                    durationTimer = 0f;
                }
            }
            else
            {
                // Espera antes de começar o fade-out
                durationTimer += Time.deltaTime;
                if (durationTimer >= intensityDuration)
                {
                    currentValue = Mathf.MoveTowards(currentValue, 0f, Time.deltaTime * fadeSpeed);
                    if (Mathf.Approximately(currentValue, 0f))
                        animating = false;
                }
            }

            materialInstance.SetFloat(shaderParameter, currentValue);
        }
    }

    // --------------------------
    // Public Controls
    // --------------------------

    public void PlayVFX()
    {
        // Inicia partículas
        if (playParticles && particles != null)
            particles.Play();

        // Inicia animação de shader
        if (materialInstance != null)
        {
            animating = true;
            currentValue = 0f;
            fadingOut = false;
        }

        // Destroi o objeto quando terminar (se for marcado)
        if (destroyAfter)
            Destroy(gameObject, particles.main.duration + 0.5f + destroyDelay);
    }

    public void StopVFX()
    {
        if (particles != null && stopParticlesOnFade)
            particles.Stop();

        if (materialInstance != null)
            materialInstance.SetFloat(shaderParameter, 0f);

        animating = false;
        fadingOut = false;
    }
}

// Particles for = Fumaça, fogo, faíscas, poeira, impacto físico
// Shaders for = Energia, aura, campo de força, dissolver, brilho, piscada, distortion, glitch, brilho pulsante
