///* ----------------------------------------------------------------
// DESCRIÇÃO: Controlador de áudio do inimigo. 
// Ouve eventos de dano, morte e ataque para emitir feedback sonoro.
// ---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class EnemyAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private HealthComponent healthComponent;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Overrides (opcional — vazio usa EnemyCommonSfxConfig em Resources)")]
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private NetworkEnemyController _networkEnemy;
    private EnemyAttack_Melee _meleeAttack;
    private EnemyAttack_Ranged _rangedAttack;
    private EnemyTelegraphedAttacker _telegraphedAttacker;

    private void Awake()
    {
        if (healthComponent == null)
            healthComponent = GetComponent<HealthComponent>();

        _networkEnemy = GetComponent<NetworkEnemyController>();
        _telegraphedAttacker = GetComponent<EnemyTelegraphedAttacker>();
        _meleeAttack = GetComponent<EnemyAttack_Melee>();
        _rangedAttack = GetComponent<EnemyAttack_Ranged>();

        ResolveFallbackClips();
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.AddListener(HandleTakeDamage);
            healthComponent.OnDied.AddListener(HandleDied);
        }

        if (_telegraphedAttacker != null && _telegraphedAttacker.HasActivePattern)
            _telegraphedAttacker.OnAttackWindup += HandleAttack;
        else
        {
            if (_meleeAttack != null && _meleeAttack.enabled)
                _meleeAttack.OnAttack += HandleAttack;
            if (_rangedAttack != null && _rangedAttack.enabled)
                _rangedAttack.OnAttack += HandleAttack;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnTakeDamage.RemoveListener(HandleTakeDamage);
            healthComponent.OnDied.RemoveListener(HandleDied);
        }

        if (_telegraphedAttacker != null)
            _telegraphedAttacker.OnAttackWindup -= HandleAttack;
        if (_meleeAttack != null)
            _meleeAttack.OnAttack -= HandleAttack;
        if (_rangedAttack != null)
            _rangedAttack.OnAttack -= HandleAttack;
    }

    public void PlayAttackSfx() => PlaySfx(EnemySfxKind.Attack);

    public void PlayDamageSfx() => PlaySfx(EnemySfxKind.Damage);

    public void PlayDeathSfx() => PlaySfx(EnemySfxKind.Death);

    private void HandleAttack()
    {
        // Em rede o SFX de ataque sai por NetworkEnemyController (server + sync cliente).
        if (UsesNetworkSfxRelay())
            return;

        PlayAttackSfx();
    }

    private void HandleTakeDamage()
    {
        if (UsesNetworkSfxRelay())
            return;

        PlayDamageSfx();
    }

    private void HandleDied()
    {
        if (UsesNetworkSfxRelay())
            return;

        PlayDeathSfx();
    }

    private bool UsesNetworkSfxRelay()
    {
        if (_networkEnemy == null)
            _networkEnemy = GetComponent<NetworkEnemyController>();

        return _networkEnemy != null && _networkEnemy.IsSpawned;
    }

    private void PlaySfx(EnemySfxKind kind)
    {
        AudioClip clip = kind switch
        {
            EnemySfxKind.Attack => PickAttackClip(),
            EnemySfxKind.Damage => damageClip,
            EnemySfxKind.Death => deathClip,
            _ => null
        };

        EnemySfxBus.Play(kind, transform.position, clip, volume);
    }

    private AudioClip PickAttackClip()
    {
        if (attackClips != null && attackClips.Length > 0)
            return attackClips[Random.Range(0, attackClips.Length)];

        return EnemySfxBus.Config?.PickAttackClip();
    }

    private void ResolveFallbackClips()
    {
        EnemyCommonSfxConfig shared = EnemySfxBus.Config;
        if (shared == null)
            return;

        if (attackClips == null || attackClips.Length == 0)
            attackClips = shared.attackClips;

        if (damageClip == null)
            damageClip = shared.damageClip;
        if (deathClip == null)
            deathClip = shared.deathClip;
    }
}
