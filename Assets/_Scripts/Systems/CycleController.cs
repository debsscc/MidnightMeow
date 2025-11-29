///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla o ciclo dia/noite do jogo, alternando entre DayManager e NightManager.
// ---------------------------------------------------------------- */

using UnityEngine;

public class CycleController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private NightManager nightManager;

    //[Header("Global")]
    // Opcional: Referência ao GameManager se precisar checar estado
    // Mas aqui usaremos eventos.

    private void Start()
    {
        // O jogo começa de dia
        dayManager.StartDay();
    }

    private void OnEnable()
    {
        // Ciclo: Dia acaba -> Começa Noite
        dayManager.OnDayEnded += StartNightPhase;

        // Ciclo: Noite acaba -> Começa Dia
        nightManager.OnNightEnded += StartDayPhase;

        // Game Over: Se a casa cair, para tudo
        HouseController.OnHouseDestroyed += HandleGameOver;
    }

    private void OnDisable()
    {
        dayManager.OnDayEnded -= StartNightPhase;
        nightManager.OnNightEnded -= StartDayPhase;
        HouseController.OnHouseDestroyed -= HandleGameOver;
    }

    private void StartNightPhase()
    {
        nightManager.StartNight();
        // Aqui você pode chamar ScreenManager.ShowNightHUD();
    }

    private void StartDayPhase()
    {
        dayManager.StartDay();
        // Aqui você pode chamar ScreenManager.ShowDayHUD();
        // Talvez curar a casa um pouco?
    }

    private void HandleGameOver()
    {
        // Para o spawn de inimigos imediatamente
        nightManager.ForceStop();
        this.enabled = false; // Desliga o ciclo
    }
}