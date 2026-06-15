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
    [SerializeField] private float postDeathHoldSeconds = 10f;

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
            : 2f;

        float clip = AnimatorDeathTimingUtility.ResolveConfiguredClipLength(profile, 1f);
        float total = clip + hold;

        if (includeDissolve && dissolve != null)
            total += dissolve.Duration;

        return total;
    }

    public void BeginDeathPresentation(bool dissolveAfterHold, Action onComplete = null)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        PrepareForDeathPresentation();
        _routine = StartCoroutine(DeathPresentationRoutine(dissolveAfterHold, onComplete));
    }

    private void PrepareForDeathPresentation()
    {
        if (healthComponent != null)
        {
            healthComponent.SetAllowDestroyOnDeath(false);
            healthComponent.SetDestroyDelay(EstimatePresentationDuration(
                animationBinder != null ? animationBinder.Profile : null,
                dissolveEffect,
                includeDissolve: false) + 2f);
        }

        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private IEnumerator DeathPresentationRoutine(bool dissolveAfterHold, Action onComplete)
    {
        BeginDeathAmbience(dissolveAfterHold);

        animationHandler?.HandleDeath();

        yield return null;
        yield return null;

        yield return WaitUntilDeathAnimationCompletes();
        FreezeDeathPose();

        yield return new WaitForSecondsRealtime(postDeathHoldSeconds);

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

    private IEnumerator WaitUntilDeathAnimationCompletes()
    {
        float fallbackSeconds = AnimatorDeathTimingUtility.ResolveConfiguredClipLength(
            animationBinder != null ? animationBinder.Profile : null,
            1f);
        float deadline = Time.unscaledTime + fallbackSeconds + 1.5f;

        while (Time.unscaledTime < deadline)
        {
            if (animator == null)
                yield break;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(deathStateName) && state.normalizedTime >= 0.99f && !animator.IsInTransition(0))
                yield break;

            yield return null;
        }
    }

    private void FreezeDeathPose()
    {
        if (animator == null)
            return;

        animator.Play(deathStateName, 0, 1f);
        animator.Update(0f);
        animator.speed = 0f;
    }
}
