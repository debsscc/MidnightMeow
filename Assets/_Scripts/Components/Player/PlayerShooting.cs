///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Controla o disparo de proj�teis pelo jogador quando o input de 'Fire' � acionado.
// ---------------------------------------------------------------- */
using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerAmmo))]
public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private PlayerInputHandler _input;
    private PlayerAmmo _ammo;
    private PlayerAdrenaline _adrenaline;

    public event Action OnShoot;
    public event Action OnOutOfAmmo;

    [Header("Shooting")]
    [Tooltip("Shots per second (can be modified by upgrades)")]
    [SerializeField] private float baseFireRate = 3f;

    private float _fireRate;
    private float _damageMultiplier = 1f;
    private Coroutine _fireCoroutine;

    public float BaseFireRate => baseFireRate;
    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _ammo = GetComponent<PlayerAmmo>();
        _adrenaline = GetComponent<PlayerAdrenaline>();
        _fireRate = baseFireRate;
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
        Debug.Log($"Fire input: {(pressed ? "Pressed" : "Released")}. Fire Rate: {_fireRate}, Damage Multiplier: {_damageMultiplier}");
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
                GameObject projectileInstance = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

                if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
                {
                    projectile.SetDamageMultiplier(_damageMultiplier);
                    if (_adrenaline != null && _adrenaline.IsFrenzyActive)
                    {
                        projectile.AddBonusBounces(_adrenaline.GetBonusBounces());
                    }
                }

                OnShoot?.Invoke();
            }
            else
            {
                OnOutOfAmmo?.Invoke();  // Emitir som de clique vazio ou similar
                Debug.Log("Sem Munição!");
                yield break;
            }

            float delay = _fireRate > 0f ? 1f / _fireRate : 0.2f;
            yield return new WaitForSeconds(delay);
        }
    }

    // API: allow external systems (upgrades) to change fire rate and damage
    public void SetFireRate(float shotsPerSecond)
    {
        _fireRate = Mathf.Max(0.1f, shotsPerSecond);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = Mathf.Max(0f, multiplier);
    }
}