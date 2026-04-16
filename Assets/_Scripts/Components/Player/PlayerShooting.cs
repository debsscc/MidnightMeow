///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Controla o disparo de proj�teis pelo jogador quando o input de 'Fire' � acionado.
// ---------------------------------------------------------------- */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerAmmo))]
public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private PlayerInputHandler _input;
    private PlayerAmmo _ammo;
    private PlayerAdrenaline _adrenaline;
    private Camera _mainCamera;

    public event Action OnShoot;
    // Evento emitido quando um projétil é instanciado (recebe o GameObject do projétil)
    public event Action<GameObject> OnProjectileInstantiated;
    public event Action OnOutOfAmmo;

    [Header("Shooting")]
    [Tooltip("Shots per second (can be modified by upgrades)")]
    [SerializeField] private float baseFireRate = 3f;

    public float CurrentFireRate;
    public float DamageMultiplier = 1f;
    private Coroutine _fireCoroutine;

    public float BaseFireRate => baseFireRate;
    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _ammo = GetComponent<PlayerAmmo>();
        _adrenaline = GetComponent<PlayerAdrenaline>();
        _mainCamera = Camera.main;
        CurrentFireRate = baseFireRate;
    }

    // Assina e desassina eventos de input
    private void OnEnable()
    {
        _input.OnFireInput += HandleFireInput;
    }
    private void OnDisable()
    {
        _input.OnFireInput -= HandleFireInput;
        StopFiring();
    }

    // Lida com o input de disparo (pressed = true, released = false)
    private void HandleFireInput(bool pressed)
    {
        if (pressed)
        {
            if (_fireCoroutine == null)
                _fireCoroutine = StartCoroutine(FireContinuously());
        }
        else
        {
            StopFiring();
        }
//        Debug.Log($"Fire input: {(pressed ? "Pressed" : "Released")}. Fire Rate: {CurrentFireRate}, Damage Multiplier: {DamageMultiplier}");
    }

    private void StopFiring()
    {
        if (_fireCoroutine != null)
        {
            StopCoroutine(_fireCoroutine);
            _fireCoroutine = null;
        }
    }

    private IEnumerator FireContinuously()
    {
        while (true)
        {
            if (_ammo.HasAmmo())
            {
                _ammo.UseAmmo(1);
                Vector2 fireDirection = GetFireDirection();
                float fireAngle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg - 90f;
                Quaternion fireRotation = Quaternion.Euler(0f, 0f, fireAngle);

                firePoint.rotation = fireRotation;

                GameObject projectileInstance = Instantiate(projectilePrefab, firePoint.position, fireRotation);

                if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
                {
                    projectile.InitializeDirection(fireDirection);
                    projectile.SetDamageMultiplier(DamageMultiplier);
                    if (_adrenaline != null && _adrenaline.IsFrenzyActive)
                    {
                        projectile.AddBonusBounces(_adrenaline.GetBonusBounces());
                    }
                }

                // Notifica listeners sobre a instância do projétil
                OnProjectileInstantiated?.Invoke(projectileInstance);

                OnShoot?.Invoke();
            }
            else
            {
                OnOutOfAmmo?.Invoke();  // Emitir som de clique vazio ou similar
                Debug.Log("Sem Munição!");
                yield break;
            }

            float delay = CurrentFireRate > 0f ? 1f / CurrentFireRate : 0.2f;
            yield return new WaitForSeconds(delay);
        }
    }

    private Vector2 GetFireDirection()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (Mouse.current == null || _mainCamera == null)
        {
            return firePoint != null ? (Vector2)firePoint.up : Vector2.up;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 fireOrigin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 fireDirection = (Vector2)(mouseWorldPosition - (Vector3)fireOrigin);

        if (fireDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return firePoint != null ? (Vector2)firePoint.up : Vector2.up;
        }

        return fireDirection.normalized;
    }

    // API: allow external systems (upgrades) to change fire rate and damage
    public void SetFireRate(float shotsPerSecond)
    {
        CurrentFireRate = Mathf.Max(0.1f, shotsPerSecond);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        DamageMultiplier = Mathf.Max(0f, multiplier);
    }
}