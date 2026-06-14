using UnityEngine;

/// <summary>
/// Aplica <see cref="CharacterAnimationProfile"/> ao Animator na inicialização.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class AnimatorProfileBinder : MonoBehaviour
{
    [SerializeField] private CharacterAnimationProfile profile;
    [SerializeField] private Animator animator;

    public CharacterAnimationProfile Profile => profile;
    public Animator Animator => animator;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        ApplyProfile();
    }

    public void SetProfile(CharacterAnimationProfile newProfile)
    {
        profile = newProfile;
        ApplyProfile();
    }

    public void ApplyProfile()
    {
        if (profile == null || animator == null)
            return;

        RuntimeAnimatorController runtime = profile.BuildRuntimeController();
        if (runtime != null)
            animator.runtimeAnimatorController = runtime;
    }

    public int GetMoveSpeedHash() => profile != null ? profile.GetParameterHash(profile.moveSpeedParameter) : Animator.StringToHash("MoveSpeed");
    public int GetAttackSpeedHash() => profile != null ? profile.GetParameterHash(profile.attackSpeedParameter) : Animator.StringToHash("AttackSpeed");
    public int GetOnShootHash() => profile != null ? profile.GetParameterHash(profile.onShootTrigger) : Animator.StringToHash("OnShoot");
    public int GetOnAbility1Hash() => profile != null ? profile.GetParameterHash(profile.onAbility1Trigger) : Animator.StringToHash("OnAbility1");
    public int GetOnAbility2Hash() => profile != null ? profile.GetParameterHash(profile.onAbility2Trigger) : Animator.StringToHash("OnAbility2");
    public int GetOnDashHash() => profile != null ? profile.GetParameterHash(profile.onDashTrigger) : Animator.StringToHash("OnDash");
    public int GetOnDashAttackHash() => profile != null ? profile.GetParameterHash(profile.onDashAttackTrigger) : Animator.StringToHash("OnDashAttack");
    public int GetIsDashingHash() => profile != null ? profile.GetParameterHash(profile.isDashingParameter) : Animator.StringToHash("IsDashing");
    public int GetIsAttackingHash() => profile != null ? profile.GetParameterHash(profile.isAttackingParameter) : Animator.StringToHash("IsAttacking");
    public int GetOnTakeDamageHash() => profile != null ? profile.GetParameterHash(profile.onTakeDamageTrigger) : Animator.StringToHash("OnDamage");
    public int GetOnDieHash() => profile != null ? profile.GetParameterHash(profile.onDieTrigger) : Animator.StringToHash("OnDie");

    public float AttackAnimClipLength =>
        profile != null && profile.attackAnimClipLength > 0f ? profile.attackAnimClipLength : 0.333f;

    public float DeathDestroyDelay =>
        profile != null && profile.deathDestroyDelay > 0f ? profile.deathDestroyDelay : 4f;
}
