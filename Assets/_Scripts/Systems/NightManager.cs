///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia o ciclo da noite, iniciando e terminando as ondas de inimigos.
// ---------------------------------------------------------------- */

using UnityEngine;

public class NightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Config")]
    [SerializeField] private WaveSettings[] nightsConfigurations; 

    public event System.Action OnNightEnded;
    private int _currentNightIndex = 0;

    private void OnEnable()
    {
        waveGenerator.OnAllWavesCleared += HandleVictory;
    }

    private void OnDisable()
    {
        waveGenerator.OnAllWavesCleared -= HandleVictory;
    }

    public void StartNight()
    {
        Debug.Log($"--- INÍCIO DA NOITE {_currentNightIndex + 1} ---");
        Debug.Log($"Tamanho da configuração : {nightsConfigurations.Length}");
        // Verifica se temos configuração para essa noite
        if (_currentNightIndex < nightsConfigurations.Length)
        {
            waveGenerator.Initialize(nightsConfigurations[_currentNightIndex]);
            waveGenerator.StartSpawning();
        }
        else
        {
            Debug.Log("Fim do conteúdo configurado (Loop ou Vitória final)");
            // Loop fallback ou lógica de zerar o jogo
        }
    }

    public void ForceStop()
    {
        waveGenerator.StopSpawning();
    }

    private void HandleVictory()
    {
        Debug.Log("Noite Vencida!");
        _currentNightIndex++; // Prepara para a próxima noite
        OnNightEnded?.Invoke();
    }
}