using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Pulse : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float glowStrength = 2.5f;

    private SpriteRenderer sr;
    private Color baseColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    void Update()
    {
        float pulse = 0.8f + Mathf.Sin(Time.time * pulseSpeed) * 0.2f;

        // Cor verde intensa (acima de 1 pra ativar Bloom)
        Color glowColor = new Color(0.3f, 1.5f, 0.4f);

        sr.color = glowColor * pulse * glowStrength;
    }
}