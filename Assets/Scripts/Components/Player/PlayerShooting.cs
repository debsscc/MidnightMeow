///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla o disparo de projéteis pelo jogador quando o input de 'Fire' é acionado.
// ---------------------------------------------------------------- */
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerAmmo))]
public class PlayerShooting : MonoBehaviour
{
    public readonly struct ShootingPipelineSnapshot
    {
        public readonly string Stage;
        public readonly bool HasAmmo;
        public readonly bool ConsumedAmmoLocally;
        public readonly int Ammo;
        public readonly Vector3 SpawnPosition;
        public readonly Vector3 FirePointPosition;
        public readonly Vector3 FirePointEuler;
        public readonly Vector3 ProjectilePosition;
        public readonly Vector3 ProjectileEuler;
        public readonly Vector2 Direction;
        public readonly float RotationZ;
        public readonly string ProjectilePrefabName;

        public ShootingPipelineSnapshot(
            string stage,
            bool hasAmmo,
            bool consumedAmmoLocally,
            int ammo,
            Vector3 spawnPosition,
            Vector3 firePointPosition,
            Vector3 firePointEuler,
            Vector3 projectilePosition,
            Vector3 projectileEuler,
            Vector2 direction,
            float rotationZ,
            string projectilePrefabName)
        {
            Stage = stage;
            HasAmmo = hasAmmo;
            ConsumedAmmoLocally = consumedAmmoLocally;
            Ammo = ammo;
            SpawnPosition = spawnPosition;
            FirePointPosition = firePointPosition;
            FirePointEuler = firePointEuler;
            ProjectilePosition = projectilePosition;
            ProjectileEuler = projectileEuler;
            Direction = direction;
            RotationZ = rotationZ;
            ProjectilePrefabName = projectilePrefabName;
        }
    }

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private PlayerInputHandler _input;
    private PlayerAmmo _ammo;
    private PlayerAdrenaline _adrenaline;
    private PlayerAim _aim;
    private PlayerAbilityHandler _abilityHandler;
    private PlayerPassiveHandler _passiveHandler;
    private PlayerAnimationHandler _animationHandler;
    private Camera _mainCamera;

    private float _nextShotAllowedTime;
    private bool _wantsContinuousFire;
    private bool _tapShotQueued;

    public event Action OnShoot;
    public event Action<GameObject, Vector3, Quaternion, Vector2> OnProjectileInstantiated;
    public event Action OnOutOfAmmo;
    public event Action<Vector2, bool, int> OnFireDirectionComputed;
    public event Action<ShootingPipelineSnapshot> OnShootingPipelineSampled;

    [Header("Shooting")]
    [Tooltip("Shots per second (can be modified by upgrades)")]
    [SerializeField] private float baseFireRate = 3f;

    public float CurrentFireRate;
    public float DamageMultiplier = 1f;
    private Coroutine _fireCoroutine;

    public float BaseFireRate => baseFireRate;
    public bool IsFiring => _fireCoroutine != null;

    public void ApplyRuntimeStats(RangedCombatStats rangedStats)
    {
        if (rangedStats == null)
            return;

        baseFireRate = rangedStats.fireRate;
        CurrentFireRate = rangedStats.fireRate;
        DamageMultiplier = rangedStats.damageMultiplier;
    }

    private bool ShouldConsumeAmmoLocally()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || (!nm.IsClient && !nm.IsServer))
            return true;

        var spawner = GetComponent<NetworkProjectileSpawner>();
        if (spawner == null || !spawner.IsSpawned || !spawner.IsOwner)
            return true;

        return false;
    }

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _ammo = GetComponent<PlayerAmmo>();
        _adrenaline = GetComponent<PlayerAdrenaline>();
        _aim = GetComponent<PlayerAim>();
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
        _passiveHandler = GetComponent<PlayerPassiveHandler>();
        _animationHandler = GetComponent<PlayerAnimationHandler>();
        _mainCamera = Camera.main;
        CurrentFireRate = baseFireRate;
    }

    private void OnEnable()
    {
        _input.OnFireInput += HandleFireInput;
    }

    private void OnDisable()
    {
        _input.OnFireInput -= HandleFireInput;
        StopFiringImmediate();
    }

    private void HandleFireInput(bool pressed)
    {
        if (TryGetComponent<NetworkPlayerRevive>(out var revive) && revive.IsReviving)
            return;

        if (pressed)
        {
            _wantsContinuousFire = true;
            _tapShotQueued = true;
            if (_fireCoroutine == null)
                _fireCoroutine = StartCoroutine(FireRoutine());
        }
        else
        {
            _wantsContinuousFire = false;
        }
    }

    private void StopFiringImmediate()
    {
        _wantsContinuousFire = false;
        _tapShotQueued = false;

        if (_fireCoroutine != null)
        {
            StopCoroutine(_fireCoroutine);
            _fireCoroutine = null;
        }
    }

    private IEnumerator FireRoutine()
    {
        while (_wantsContinuousFire || _tapShotQueued)
        {
            if (_abilityHandler != null && _abilityHandler.IsActionLocked)
            {
                if (!_wantsContinuousFire && !_tapShotQueued)
                    break;

                yield return null;
                continue;
            }

            if (!_ammo.HasAmmo())
            {
                EmitShootingPipeline(
                    "OutOfAmmo",
                    false,
                    false,
                    firePoint != null ? firePoint.position : transform.position,
                    Vector3.zero,
                    Vector3.zero,
                    firePoint != null ? (Vector2)firePoint.up : Vector2.up,
                    firePoint != null ? firePoint.eulerAngles.z : 0f
                );
                OnOutOfAmmo?.Invoke();
                break;
            }

            while (Time.time < _nextShotAllowedTime)
            {
                if (!_wantsContinuousFire && !_tapShotQueued)
                    yield break;

                yield return null;
            }

            if (!TryFireValidatedShot())
                break;

            _tapShotQueued = false;

            if (!_wantsContinuousFire)
                break;

            float wait = _nextShotAllowedTime - Time.time;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
        }

        _fireCoroutine = null;
    }

    private bool TryFireValidatedShot()
    {
        if (!TryConsumeFireCooldown())
            return false;

        if (!_ammo.HasAmmo())
            return false;

        ExecuteShot();
        return true;
    }

    private bool TryConsumeFireCooldown()
    {
        if (Time.time < _nextShotAllowedTime)
            return false;

        float interval = CurrentFireRate > 0f ? 1f / CurrentFireRate : 0.2f;
        _nextShotAllowedTime = Time.time + interval;
        return true;
    }

    private void ExecuteShot()
    {
        bool consumedAmmoLocally = ShouldConsumeAmmoLocally();
        if (consumedAmmoLocally)
            _ammo.UseAmmo(1);

        bool usedFirePointDirection;
        Vector3 spawnPosition;
        Quaternion fireRotation;
        Vector2 fireDirection = GetFirePose(out spawnPosition, out fireRotation, out usedFirePointDirection);
        EmitShootingPipeline(
            "AfterFirePose",
            true,
            consumedAmmoLocally,
            spawnPosition,
            Vector3.zero,
            Vector3.zero,
            fireDirection,
            fireRotation.eulerAngles.z
        );
        OnFireDirectionComputed?.Invoke(fireDirection, usedFirePointDirection, _ammo.CurrentAmmo);

        if (firePoint != null)
            firePoint.SetPositionAndRotation(spawnPosition, fireRotation);

        GameObject projectileInstance = Instantiate(projectilePrefab, spawnPosition, fireRotation);
        EmitShootingPipeline(
            "AfterLocalInstantiate",
            true,
            consumedAmmoLocally,
            spawnPosition,
            projectileInstance.transform.position,
            projectileInstance.transform.eulerAngles,
            fireDirection,
            fireRotation.eulerAngles.z
        );

        if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
        {
            projectile.InitializeDirection(fireDirection);
            projectile.SetDamageMultiplier(DamageMultiplier);
            int bonusBounces = 0;
            if (_adrenaline != null && _adrenaline.IsFrenzyActive)
                bonusBounces += _adrenaline.GetBonusBounces();
            if (_passiveHandler != null)
                bonusBounces += _passiveHandler.BonusProjectileBounces;
            if (bonusBounces > 0)
                projectile.AddBonusBounces(bonusBounces);
        }

        OnShoot?.Invoke();
        OnProjectileInstantiated?.Invoke(projectileInstance, spawnPosition, fireRotation, fireDirection);
    }

    private void EmitShootingPipeline(
        string stage,
        bool hasAmmo,
        bool consumedAmmoLocally,
        Vector3 spawnPosition,
        Vector3 projectilePosition,
        Vector3 projectileEuler,
        Vector2 direction,
        float rotationZ)
    {
        OnShootingPipelineSampled?.Invoke(new ShootingPipelineSnapshot(
            stage,
            hasAmmo,
            consumedAmmoLocally,
            _ammo != null ? _ammo.CurrentAmmo : -1,
            spawnPosition,
            firePoint != null ? firePoint.position : transform.position,
            firePoint != null ? firePoint.eulerAngles : Vector3.zero,
            projectilePosition,
            projectileEuler,
            direction,
            rotationZ,
            projectilePrefab != null ? projectilePrefab.name : "null"
        ));
    }

    private Vector2 GetFirePose(out Vector3 spawnPosition, out Quaternion fireRotation, out bool usedFirePointDirection)
    {
        if (_aim != null && _aim.TryGetFirePose(out spawnPosition, out fireRotation, out Vector2 aimDirection))
        {
            usedFirePointDirection = false;
            return aimDirection.normalized;
        }

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (Mouse.current == null || _mainCamera == null)
        {
            usedFirePointDirection = true;
            Vector2 fallbackDirection = firePoint != null ? (Vector2)firePoint.up : Vector2.up;
            spawnPosition = firePoint != null ? firePoint.position : transform.position;
            fireRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fallbackDirection.y, fallbackDirection.x) * Mathf.Rad2Deg - 90f);
            return fallbackDirection;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = _mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 fireOrigin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 fireDirection = (Vector2)(mouseWorldPosition - (Vector3)fireOrigin);

        if (fireDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            usedFirePointDirection = true;
            fireDirection = firePoint != null ? (Vector2)firePoint.up : Vector2.up;
        }

        usedFirePointDirection = false;
        fireDirection = fireDirection.normalized;
        spawnPosition = firePoint != null ? firePoint.position : transform.position;
        fireRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg - 90f);
        return fireDirection;
    }

    public void SetFireRate(float shotsPerSecond)
    {
        CurrentFireRate = Mathf.Max(0.1f, shotsPerSecond);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        DamageMultiplier = Mathf.Max(0f, multiplier);
    }
}
