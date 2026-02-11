///* ----------------------------------------------------------------
// CRIADO EM: 21-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Gerencia o ciclo da noite, iniciando e terminando as ondas de inimigos.
// ---------------------------------------------------------------- */

using UnityEngine;

public class NightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGenerator waveGenerator;

    [Header("Config")]
    [SerializeField] private WaveSettings nightConfiguration;

    public event System.Action OnNightEnded;

    private void OnEnable()
    {
        waveGenerator.OnAllWavesCleared += HandleVictory;
    }

    private void OnDisable()
    {
        waveGenerator.OnAllWavesCleared -= HandleVictory;
    }

    private void Start()
    {
        StartNight();
    }

    public void StartNight()
    {
        if (nightConfiguration != null)
        {
            waveGenerator.Initialize(nightConfiguration);
            waveGenerator.StartSpawning();
        }
        else
        {
            Debug.LogError("Nenhuma configura��o de wave atribu�da ao NightManager!");
        }
    }

    public void ForceStop()
    {
        waveGenerator.StopSpawning();
    }

    private void HandleVictory()
    {
        Debug.Log("Todas as waves foram completadas!");
        OnNightEnded?.Invoke();
    }
}