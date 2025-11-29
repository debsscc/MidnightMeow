///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla a mira do jogador com o mouse, posicionando e rotacionando o ponto de disparo (firePoint).
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    // Referências do Inspector
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerStats stats; 

    private Camera _mainCamera;
    private Vector2 _mousePosition;

    private void Awake()
    {
        /// Pega a referência para a câmera principal
        _mainCamera = Camera.main; 
    }

    private void Update()
    {
        // Lê a posição do mouse na tela e converte para coordenadas do mundo
        _mousePosition = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(_mousePosition);
        Vector2 lookDirection = mouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

        // Rotaciona e posiciona o firePoint para olhar na direção do mouse
        firePoint.rotation = Quaternion.Euler(0, 0, angle - 90f);
        Vector2 localOffset = lookDirection.normalized * stats.firePointRadius;
        firePoint.localPosition = localOffset;
    }
}