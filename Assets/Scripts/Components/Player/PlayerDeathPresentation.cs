using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sequência de morte: animação completa, espera mínima, dissolve opcional (MP com aliado vivo).
/// </summary>
[DisallowMultipleComponent]
public class PlayerDeathPresentation : MonoBehaviour
{
    [SerializeField] private PlayerAnimationHandler animationHandler;
    [SerializeField] private AnimatorProfileBinder animationBinder;
    [SerializeField] private Animator animator;
    [SerializeField] private DissolveEffect dissolveEffect;
    [SerializeField] private HealthComponent healthComponent;

    [Header("Animator")]
    [SerializeField] private string deathStateName = "Dying";

    [Header("Timing")]
    [SerializeField] private float postDeathHoldSeconds = 5f;

    private Coroutine _routine;

    public float PostDeathHoldSeconds => postDeathHoldSeconds;

    private void Awake()
    {
        if (animationHandler == null)
            animationHandler = GetComponent<PlayerAnimationHandler>();
        if (animationBinder == null)
            animationBinder = GetComponent<AnimatorProfileBinder>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (dissolveEffect == null)
            dissolveEffect = GetComponent<DissolveEffect>();
        if (healthComponent == null)
            healthComponent = GetComponent<HealthComponent>();

        if (animationBinder != null && animationBinder.Profile != null)
        {
            if (animationBinder.Profile.postDeathHoldSeconds > 0f)
                postDeathHoldSeconds = animationBinder.Profile.postDeathHoldSeconds;

            if (!string.IsNullOrEmpty(animationBinder.Profile.deathAnimatorStateName))
                deathStateName = animationBinder.Profile.deathAnimatorStateName;
        }
    }

    public static float EstimatePresentationDuration(
        CharacterAnimationProfile profile,
        DissolveEffect dissolve,
        bool includeDissolve)
    {
        float hold = profile != null && profile.postDeathHoldSeconds > 0f
            ? profile.postDeathHoldSeconds
            : 5f;

        float clip = AnimatorDeathTimingUtility.ResolveConfiguredClipLength(profile, 1f);
        float total = Mathf.Max(clip, hold);

        if (includeDissolve && dissolve != null)
            total += dissolve.Duration;

        return total;
    }

    public void BeginDeathPresentation(bool dissolveAfterHold, Action onComplete = null)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(DeathPresentationRoutine(dissolveAfterHold, onComplete));
    }

    private IEnumerator DeathPresentationRoutine(bool dissolveAfterHold, Action onComplete)
    {
        if (healthComponent != null)
            healthComponent.SetAllowDestroyOnDeath(false);

        BeginDeathAmbience(dissolveAfterHold);

        animationHandler?.HandleDeath();

        yield return null;
        yield return null;

        float presentationStart = Time.unscaledTime;

        float clipLength = AnimatorDeathTimingUtility.MeasureCurrentStateLength(
            animator,
            fallbackSeconds: AnimatorDeathTimingUtility.ResolveConfiguredClipLength(
                animationBinder != null ? animationBinder.Profile : null,
                1f));

        float holdDuration = Mathf.Max(clipLength, postDeathHoldSeconds);
        yield return new WaitForSeconds(holdDuration);

        if (!dissolveAfterHold)
            yield return WaitForRemainingAmbience(presentationStart);

        if (dissolveAfterHold && dissolveEffect != null)
            dissolveEffect.HandleDeath();

        if (dissolveAfterHold)
            MultiplayerCameraController.Resolve()?.EndDeathFocus();

        TryRebindCameraToAliveTeammate();

        onComplete?.Invoke();
        _routine = null;
    }

    private void TryRebindCameraToAliveTeammate()
    {
        NetworkObject netObject = GetComponent<NetworkObject>();
        if (netObject == null || !netObject.IsSpawned || !netObject.IsOwner)
            return;

        if (!NetworkPlayerController.TryGetFirstAliveTeammateFollowTarget(out Transform followTarget))
            return;

        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        controller?.SetTarget(followTarget);
        controller?.EndDeathFocus();
    }

    private void BeginDeathAmbience(bool dissolveAfterHold)
    {
        if (!ShouldRunLocalDeathAmbience())
            return;

        DeathHordePresentation settings = GetComponent<DeathHordePresentation>();

        if (dissolveAfterHold)
            DeathHordePresentation.TryBeginSpectatorDeath(this, transform, settings);
        else
            DeathHordePresentation.TryBeginFinalDefeat(this, transform, settings);
    }

    private bool ShouldRunLocalDeathAmbience()
    {
        NetworkObject netObject = GetComponent<NetworkObject>();
        if (netObject == null || !netObject.IsSpawned)
            return true;

        return netObject.IsOwner;
    }

    private IEnumerator WaitForRemainingAmbience(float presentationStartUnscaled)
    {
        DeathHordePresentation horde = GetComponent<DeathHordePresentation>();
        float ambienceEnd = horde != null ? horde.AmbienceEndSeconds : DeathHordePresentation.DefaultAmbienceEndSeconds;
        float remaining = ambienceEnd - (Time.unscaledTime - presentationStartUnscaled);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);
    }
}
