using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Combo de derrota (opção 5): para spawn, slow-mo leve, fade dos ratos, vinheta + zoom no corpo.
/// </summary>
public class DeathHordePresentation : MonoBehaviour
{
    [Header("Timing (segundos reais)")]
    [SerializeField] private float slowMoDuration = 2f;
    [SerializeField] private float slowMoTimeScale = 0.65f;
    [SerializeField] private float enemyFadeStart = 3f;
    [SerializeField] private float enemyFadeEnd = 8f;

    [Header("Spectator Death")]
    [SerializeField] private float spectatorAmbienceDuration = 6f;
    [SerializeField] private float spectatorEnemyFadeStart = 1f;
    [SerializeField] private float spectatorEnemyFadeEnd = 5f;
    [SerializeField] private float spectatorEnemyFadeRadius = 6f;

    [Header("Câmera")]
    [SerializeField] private float deathZoomOrthographicSize = 7f;
    [SerializeField] private float vignettePeakIntensity = 0.42f;

    private static bool _finalDefeatRunning;
    private Coroutine _routine;
    private readonly List<FadedEnemySprite> _fadedSprites = new List<FadedEnemySprite>();
    private float _focusZoomFrom = 8f;
    private Transform _focusBody;

    public float AmbienceEndSeconds => enemyFadeEnd;

    public static float DefaultAmbienceEndSeconds => 8f;

    private struct FadedEnemySprite
    {
        public SpriteRenderer Renderer;
        public float InitialAlpha;
    }

    public static void TryBeginFinalDefeat(MonoBehaviour runner, Transform focusBody, DeathHordePresentation settingsSource = null)
    {
        if (_finalDefeatRunning || runner == null)
            return;

        DeathHordePresentation presentation = settingsSource ?? runner.GetComponent<DeathHordePresentation>();
        if (presentation == null)
            presentation = runner.gameObject.AddComponent<DeathHordePresentation>();

        presentation.BeginFinalDefeat(focusBody);
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

    public void BeginFinalDefeat(Transform focusBody)
    {
        if (_finalDefeatRunning)
            return;

        if (_routine != null)
            StopCoroutine(_routine);

        _finalDefeatRunning = true;
        _focusBody = focusBody;
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
        _focusZoomFrom = MultiplayerCameraController.Resolve()?.GetActiveOrthographicSize() ?? deathZoomOrthographicSize;
        FocusCamera(focusBody);
        _routine = StartCoroutine(SpectatorDeathRoutine());
    }

    private IEnumerator FinalDefeatRoutine()
    {
        float elapsed = 0f;
        ApplySlowMo(true);
        CacheEnemySprites();

        while (elapsed < enemyFadeEnd)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed <= slowMoDuration)
                ApplySlowMo(true);
            else
                ApplySlowMo(false);

            float focusT = Mathf.Clamp01(elapsed / slowMoDuration);
            UpdateDeathFocus(focusT);

            if (elapsed >= enemyFadeStart)
            {
                float fadeT = Mathf.InverseLerp(enemyFadeStart, enemyFadeEnd, elapsed);
                ApplyEnemyFade(fadeT);
            }

            yield return null;
        }

        ApplyEnemyFade(1f);
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

            if (elapsed >= spectatorEnemyFadeStart)
            {
                float fadeT = Mathf.InverseLerp(spectatorEnemyFadeStart, spectatorEnemyFadeEnd, elapsed);
                ApplyEnemyFade(fadeT);
            }

            yield return null;
        }

        ApplyEnemyFade(1f);
        ApplySlowMo(false);
        ClearDeathFocus();
        _routine = null;
    }

    private void UpdateDeathFocus(float t)
    {
        GameplayVignetteController vignette = GameplayVignetteController.Instance;
        vignette?.SetIntensity(Mathf.Lerp(0f, vignettePeakIntensity, t));

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
        GameplayVignetteController.ClearIfActive();
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
        float alphaMultiplier = 1f - Mathf.Clamp01(t);

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
