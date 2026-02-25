///* ----------------------------------------------------------------
// DESCRIÇÃO: Controla a habilidade de Dash do jogador, incluindo cooldown,
// movimento físico e travessia temporária de layers específicos.
// ---------------------------------------------------------------- */

using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class PlayerDash : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerStats stats;

    [Header("Collision Bypass")]
    [Tooltip("A Layer que o jogador PODE atravessar DURANTE o dash (ex: 'Obstacle_Low'). As outras paredes continuarão sólidas.")]
    [SerializeField] private LayerMask passThroughLayer;

    // Eventos para futuros sistemas (VFX, SFX, Animação)
    public event Action OnDashStarted;
    public event Action OnDashEnded;

    private Rigidbody2D _rb;
    private Vector2 _currentMoveDirection = Vector2.up; // Padrão caso tente dar dash parado
    private bool _isDashing = false;
    private float _lastDashTime = -Mathf.Infinity;

    public bool IsDashing => _isDashing;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        inputHandler.OnMoveInput += UpdateMoveDirection;
        inputHandler.OnDashInput += HandleDashInput;
    }

    private void OnDisable()
    {
        inputHandler.OnMoveInput -= UpdateMoveDirection;
        inputHandler.OnDashInput -= HandleDashInput;
    }

    private void UpdateMoveDirection(Vector2 direction)
    {
        // Só atualiza a direção se o jogador estiver pressionando alguma tecla WASD
        if (direction != Vector2.zero)
        {
            _currentMoveDirection = direction.normalized;
        }
    }

    private void HandleDashInput()
    {
        if (_isDashing || stats == null) return;

        // Verifica Cooldown
        if (Time.time >= _lastDashTime + stats.dashCooldown)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        _lastDashTime = Time.time;
        OnDashStarted?.Invoke();

        // 1. Lógica de Colisão: Ignorar colisão com a layer específica
        int playerLayer = gameObject.layer;
        int targetLayer = Mathf.RoundToInt(Mathf.Log(passThroughLayer.value, 2)); // Converte LayerMask para Layer Index
        
        if (passThroughLayer != 0) // Só altera se uma layer foi selecionada
            Physics2D.IgnoreLayerCollision(playerLayer, targetLayer, true);

        // 2. Execução do Movimento
        float elapsedTime = 0f;
        while (elapsedTime < stats.dashDuration)
        {
            // Substitui a velocidade do Rigidbody diretamente na direção do dash
            _rb.linearVelocity = _currentMoveDirection * stats.dashSpeed;
            
            elapsedTime += Time.deltaTime;
            yield return null; // Espera o próximo frame
        }

        // 3. Finalização
        _rb.linearVelocity = Vector2.zero; // Opcional: Para o jogador ao final do dash
        
        if (passThroughLayer != 0)
            Physics2D.IgnoreLayerCollision(playerLayer, targetLayer, false);

        _isDashing = false;
        OnDashEnded?.Invoke();
    }
    // Variáveis internas para receber a injeção (substituem a leitura direta do 'stats')
    private float _currentDashSpeed;
    private float _currentDashCooldown;
    private float _currentDashDuration;

    // Chame este método no Awake/Start do PlayerDash para pegar os valores iniciais do SO
    public void InitializeBaseStats()
    {
        if (stats != null)
        {
            _currentDashSpeed = stats.dashSpeed;
            _currentDashCooldown = stats.dashCooldown;
            _currentDashDuration = stats.dashDuration;
        }
    }

    // API para o PlayerInitializer injetar upgrades
    public void SetDashUpgrades(float extraSpeed, float cooldownReduction)
    {
        _currentDashSpeed += extraSpeed;
        _currentDashCooldown = Mathf.Max(0.1f, _currentDashCooldown - cooldownReduction);
    }
}