// ----------------------------------------------------------------------------
// MADE BY: DEBS CARVALHO
// DATE: 13/07/2026
// DESCRIPTION: Afterimage curto do sprite no início do swing melee (1 ghost que some rápido).
// ----------------------------------------------------------------------------

using UnityEngine;

public static class MeleeSwingAfterimageVfx
{
    private const float DefaultLifetime = 0.16f;
    private const float DefaultOffset = 0.22f;

    public static void Play(
        SpriteRenderer source,
        Vector2 aimDirection,
        Color tint,
        float lifetime = DefaultLifetime,
        float behindOffset = DefaultOffset)
    {
        if (source == null || source.sprite == null)
            return;

        GameObject ghost = new GameObject("MeleeSwingAfterimage");
        ghost.transform.position = source.transform.position;
        ghost.transform.rotation = source.transform.rotation;
        ghost.transform.localScale = source.transform.lossyScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder - 1;
        sr.color = tint;

        Vector2 dir = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.left;
        ghost.transform.position -= (Vector3)(dir * behindOffset);

        MeleeSwingAfterimageFade fade = ghost.AddComponent<MeleeSwingAfterimageFade>();
        fade.Begin(sr, tint, lifetime);
    }

    private sealed class MeleeSwingAfterimageFade : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _startColor;
        private Vector3 _startScale;
        private float _lifetime;
        private float _elapsed;

        public void Begin(SpriteRenderer renderer, Color startColor, float lifetime)
        {
            _renderer = renderer;
            _startColor = startColor;
            _startScale = transform.localScale;
            _lifetime = Mathf.Max(0.05f, lifetime);
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_renderer == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);
            Color c = _startColor;
            c.a = Mathf.Lerp(_startColor.a, 0f, t * t);
            _renderer.color = c;
            transform.localScale = Vector3.Lerp(_startScale, _startScale * 0.88f, t);

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
