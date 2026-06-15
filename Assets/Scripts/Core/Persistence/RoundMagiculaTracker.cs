using System;
using UnityEngine;

/// <summary>
/// Saldo de magículas coletadas na fase atual (por jogador local).
/// Persiste no <see cref="SaveProfileStore"/> ao concluir ou perder a fase.
/// </summary>
[DisallowMultipleComponent]
public class RoundMagiculaTracker : MonoBehaviour
{
    public static RoundMagiculaTracker Instance { get; private set; }

    private int _roundTotal;

    public int RoundTotal => _roundTotal;
    public event Action<int> OnRoundTotalChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        RoundMagiculaTracker existing = FindFirstObjectByType<RoundMagiculaTracker>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var go = new GameObject(nameof(RoundMagiculaTracker));
        go.AddComponent<RoundMagiculaTracker>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.OnCienciaCollected += HandleCollected;
        MultiplayerGameManager.OnVictory += HandleRoundEnded;
        MultiplayerGameManager.OnDefeat += HandleRoundEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnCienciaCollected -= HandleCollected;
        MultiplayerGameManager.OnVictory -= HandleRoundEnded;
        MultiplayerGameManager.OnDefeat -= HandleRoundEnded;
    }

    public void ResetRound()
    {
        _roundTotal = 0;
        OnRoundTotalChanged?.Invoke(_roundTotal);
    }

    private void HandleCollected(int amount)
    {
        if (amount <= 0)
            return;

        _roundTotal += amount;
        OnRoundTotalChanged?.Invoke(_roundTotal);
    }

    private void HandleRoundEnded() => CommitToSave();

    public void CommitToSave()
    {
        if (_roundTotal <= 0)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.AddMagiculas(_roundTotal);

        _roundTotal = 0;
        OnRoundTotalChanged?.Invoke(_roundTotal);
    }
}
