using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Combo de derrota (opção 5): para spawn, slow-mo leve, vinheta + zoom no corpo.
/// </summary>
public class DeathHordePresentation : MonoBehaviour
{
    [Header("Timing (segundos reais)")]
    [SerializeField] private float slowMoDuration = 2f;
    [SerializeField] private float slowMoTimeScale = 0.65f;

    [Header("Spectator Death")]
    [SerializeField] private float spectatorAmbienceDuration = 2.5f;
    [SerializeField] private float spectatorEnemyDimStart = 0.35f;
    [SerializeField] private float spectatorEnemyDimEnd = 1.75f;
    [SerializeField] private float spectatorEnemyFadeRadius = 6f;
    [SerializeField] [Range(0.35f, 1f)] private float spectatorEnemyMinAlpha = 0.55f;

    [Header("Câmera")]
    [SerializeField] private float deathZoomOrthographicSize = 6.5f;

    private static bool _finalDefeatRunning;
    private Coroutine _routine;
    private readonly List<FadedEnemySprite> _fadedSprites = new List<FadedEnemySprite>();
    private float _focusZoomFrom = 8f;
    private float _ambienceDuration;
    private float _enemyFadeMinAlpha = 0f;
    private Transform _focusBody;

    public float AmbienceEndSeconds => _ambienceDuration > 0f ? _ambienceDuration : 6f;

    public static float DefaultAmbienceEndSeconds => 6f;

    private struct FadedEnemySprite
    {
        public SpriteRenderer Renderer;
        public float InitialAlpha;
    }

    public static void TryBeginFinalDefeat(
        MonoBehaviour runner,
        Transform focusBody,
        DeathHordePresentation settingsSource = null,
        float ambienceDuration = -1f)
    {
        if (_finalDefeatRunning || runner == null)
            return;

        DeathHordePresentation presentation = settingsSource ?? runner.GetComponent<DeathHordePresentation>();
        if (presentation == null)
            presentation = runner.gameObject.AddComponent<DeathHordePresentation>();

        presentation.BeginFinalDefeat(focusBody, ambienceDuration);
    }

    public static void TryBeginSpectatorDeath(MonoBehaviour runner, Transform focusBody, DeathHordePresentation settingsSource = null)
    {
        if (runner == null || focusBody == null)
            return;

        DeathHordePresentation presentation = settingsSource ?? runner.GetComponent<DeathHordePresentation>();
        if (presentation == null)
            presentation = runner.gameObject.AddComponent<DeathHordePresentation>();

        presentation.BeginSpectatorDeath(focusBody);
    }

    public void BeginFinalDefeat(Transform focusBody, float ambienceDuration = -1f)
    {
        if (_finalDefeatRunning)
            return;

        if (_routine != null)
            StopCoroutine(_routine);

        _finalDefeatRunning = true;
        _focusBody = focusBody;
        _ambienceDuration = ambienceDuration > 0f ? ambienceDuration : DefaultAmbienceEndSeconds;
        _enemyFadeMinAlpha = 0f;
        _focusZoomFrom = MultiplayerCameraController.Resolve()?.GetActiveOrthographicSize() ?? deathZoomOrthographicSize;
        RequestStopWaveSpawning();
        FocusCamera(focusBody);
        _routine = StartCoroutine(FinalDefeatRoutine());
    }

    public void BeginSpectatorDeath(Transform focusBody)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _focusBody = focusBody;
        _ambienceDuration = spectatorAmbienceDuration;
        _enemyFadeMinAlpha = spectatorEnemyMinAlpha;
        _focusZoomFrom = MultiplayerCameraController.Resolve()?.GetActiveOrthographicSize() ?? deathZoomOrthographicSize;
        FocusCamera(focusBody);
        _routine = StartCoroutine(SpectatorDeathRoutine());
    }

    private IEnumerator FinalDefeatRoutine()
    {
        float elapsed = 0f;
        ApplySlowMo(true);

        while (elapsed < _ambienceDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed <= slowMoDuration)
                ApplySlowMo(true);
            else
                ApplySlowMo(false);

            float focusT = Mathf.Clamp01(elapsed / slowMoDuration);
            UpdateDeathFocus(focusT);

            yield return null;
        }

        ApplySlowMo(false);
        _finalDefeatRunning = false;
        _routine = null;
    }

    private IEnumerator SpectatorDeathRoutine()
    {
        float elapsed = 0f;
        ApplySlowMo(true);
        CacheEnemySpritesNearFocus(_focusBody, spectatorEnemyFadeRadius);

        while (elapsed < spectatorAmbienceDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed <= slowMoDuration)
                ApplySlowMo(true);
            else
                ApplySlowMo(false);

            float focusT = Mathf.Clamp01(elapsed / slowMoDuration);
            UpdateDeathFocus(focusT);

            if (elapsed >= spectatorEnemyDimStart)
            {
                float fadeT = Mathf.InverseLerp(spectatorEnemyDimStart, spectatorEnemyDimEnd, elapsed);
                ApplyEnemyFade(fadeT);
            }

            yield return null;
        }

        RestoreEnemyFade();
        ApplySlowMo(false);
        ClearDeathFocus();
        _routine = null;
    }

    private void UpdateDeathFocus(float t)
    {
        float zoom = Mathf.Lerp(_focusZoomFrom, deathZoomOrthographicSize, t);
        MultiplayerCameraController.Resolve()?.UpdateDeathFocusZoom(zoom);
    }

    private void FocusCamera(Transform focusBody)
    {
        if (focusBody == null)
            return;

        MultiplayerCameraController camera = MultiplayerCameraController.Resolve();
        camera?.BeginDeathFocus(deathZoomOrthographicSize, focusBody);
    }

    private void ClearDeathFocus()
    {
        MultiplayerCameraController.Resolve()?.EndDeathFocus();
    }

    private void ApplySlowMo(bool active)
    {
        if (active && Time.timeScale > slowMoTimeScale)
            Time.timeScale = slowMoTimeScale;
        else if (!active && Time.timeScale < 1f)
            Time.timeScale = 1f;
    }

    private static void RequestStopWaveSpawning()
    {
        NetworkWaveManager waveManager = NetworkWaveManager.Instance;
        if (waveManager != null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                waveManager.StopSpawning();
            else
                waveManager.RequestStopSpawningRpc();
            return;
        }

        NightManager nightManager = Object.FindFirstObjectByType<NightManager>(FindObjectsInactive.Include);
        nightManager?.ForceStop();

        WaveGenerator waveGenerator = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        waveGenerator?.StopSpawning();
    }

    private void CacheEnemySprites() => CacheEnemySpritesNearFocus(null, 0f);

    private void CacheEnemySpritesNearFocus(Transform focus, float radius)
    {
        _fadedSprites.Clear();

        HealthComponent[] healthComponents = Object.FindObjectsByType<HealthComponent>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        Vector2 focusPosition = focus != null ? (Vector2)focus.position : Vector2.zero;
        float radiusSqr = radius > 0f ? radius * radius : 0f;
        bool filterByRadius = focus != null && radius > 0f;

        for (int i = 0; i < healthComponents.Length; i++)
        {
            HealthComponent health = healthComponents[i];
            if (health == null || !health.IsAlive)
                continue;

            if (!IsEnemy(health.gameObject))
                continue;

            if (filterByRadius)
            {
                float distSqr = ((Vector2)health.transform.position - focusPosition).sqrMagnitude;
                if (distSqr > radiusSqr)
                    continue;
            }

            SpriteRenderer[] renderers = health.GetComponentsInChildren<SpriteRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                SpriteRenderer renderer = renderers[r];
                if (renderer == null)
                    continue;

                _fadedSprites.Add(new FadedEnemySprite
                {
                    Renderer = renderer,
                    InitialAlpha = renderer.color.a
                });
            }
        }
    }

    private void ApplyEnemyFade(float t)
    {
        float fade = Mathf.Clamp01(t);
        float alphaMultiplier = Mathf.Lerp(1f, _enemyFadeMinAlpha, fade);

        for (int i = 0; i < _fadedSprites.Count; i++)
        {
            SpriteRenderer renderer = _fadedSprites[i].Renderer;
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = _fadedSprites[i].InitialAlpha * alphaMultiplier;
            renderer.color = color;
        }
    }

    private void RestoreEnemyFade()
    {
        for (int i = 0; i < _fadedSprites.Count; i++)
        {
            SpriteRenderer renderer = _fadedSprites[i].Renderer;
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = _fadedSprites[i].InitialAlpha;
            renderer.color = color;
        }

        _fadedSprites.Clear();
    }

    private static bool IsEnemy(GameObject go)
    {
        if (go.CompareTag("Enemy"))
            return true;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        return enemyLayer >= 0 && go.layer == enemyLayer;
    }

    private void OnDestroy()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        ApplySlowMo(false);
        if (_finalDefeatRunning)
        {
            _finalDefeatRunning = false;
            ClearDeathFocus();
        }
    }
}
