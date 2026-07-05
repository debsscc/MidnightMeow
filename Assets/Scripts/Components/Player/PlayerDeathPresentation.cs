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

    /// <summary>
    /// Inconsciente revivível (MP): animação de queda sem dissolve, foco de câmera ou ambience de derrota.
    /// </summary>
    public void BeginDownedPresentation(Action onComplete = null)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        PrepareForDownedPresentation();
        _routine = StartCoroutine(DownedPresentationRoutine(onComplete));
    }

    public void CancelPresentation()
    {
        if (_routine == null)
            return;

        StopCoroutine(_routine);
        _routine = null;
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

    private void PrepareForDownedPresentation()
    {
        if (healthComponent != null)
        {
            healthComponent.SetAllowDestroyOnDeath(false);
            float hold = postDeathHoldSeconds > 0f ? postDeathHoldSeconds : 10f;
            healthComponent.SetDestroyDelay(hold + 60f);
        }

        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private IEnumerator DownedPresentationRoutine(Action onComplete)
    {
        animationHandler?.HandleDeath();

        yield return null;
        yield return null;

        yield return WaitUntilDeathAnimationCompletes();
        FreezeDeathPose();
        animationHandler?.FinalizeDeathPhysics();

        onComplete?.Invoke();
        _routine = null;
    }

    private IEnumerator DeathPresentationRoutine(bool dissolveAfterHold, Action onComplete)
    {
        BeginDeathAmbience(dissolveAfterHold);

        animationHandler?.HandleDeath();

        yield return null;
        yield return null;

        yield return WaitUntilDeathAnimationCompletes();
        FreezeDeathPose();
        animationHandler?.FinalizeDeathPhysics();

        yield return new WaitForSecondsRealtime(postDeathHoldSeconds);

        if (!dissolveAfterHold)
            yield return new WaitForSecondsRealtime(DefeatPresentationTiming.DefeatUiBufferSeconds);

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
        CharacterAnimationProfile profile = animationBinder != null ? animationBinder.Profile : null;
        float defeatUiDelay = DefeatPresentationTiming.ResolveDefeatUiDelay(profile, this);

        if (!dissolveAfterHold)
            GameplayVignetteController.PlayDeathSequence(defeatUiDelay, ResolveDeathVignettePeak());

        if (dissolveAfterHold)
            DeathHordePresentation.TryBeginSpectatorDeath(this, transform, settings);
        else
            DeathHordePresentation.TryBeginFinalDefeat(this, transform, settings, defeatUiDelay);
    }

    private static float ResolveDeathVignettePeak() =>
        GameSessionContext.IsSinglePlayer ? 0.58f : 0.5f;

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
