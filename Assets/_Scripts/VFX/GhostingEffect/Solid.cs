using UnityEngine;

public class Solid : MonoBehaviour
{
    private SpriteRenderer _myRenderer;
    private Shader myMaterial;
    public Color _color;

    [Tooltip("Tempo em segundos até a sombra desaparecer completamente")]
    public float fadeTime = 0.3f;

    private float _timer;
    private float _startAlpha;
    private Vector3 _initialScale;

    void OnEnable()
    {
        _myRenderer = GetComponent<SpriteRenderer>();
        myMaterial = Shader.Find("GUI/Text Shader");
        _timer = 0f;
        _startAlpha = _color.a;
        _initialScale = transform.localScale;
        if (_myRenderer != null)
        {
            _myRenderer.material.shader = myMaterial;
            _myRenderer.color = _color;
        }
    }

    public void Finished()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_myRenderer == null) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / fadeTime);
        Color c = _color;
        c.a = Mathf.Lerp(_startAlpha, 0f, t);
        _myRenderer.material.shader = myMaterial;
        _myRenderer.color = c;
        //a sombra encolhe enquanto desaparece (tipo evaporando)
        transform.localScale = Vector3.Lerp(_initialScale, _initialScale * 0.75f, t);

        if (t >= 1f)
            Finished();
    }
}