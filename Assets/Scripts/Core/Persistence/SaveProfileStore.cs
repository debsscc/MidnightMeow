using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistência de saves em JSON no disco. Suporta múltiplos slots e perfil ativo em memória.
/// </summary>
[DisallowMultipleComponent]
public class SaveProfileStore : MonoBehaviour
{
    public static SaveProfileStore Instance { get; private set; }

    public event Action OnProfileChanged;

    [SerializeField] private int defaultSlot = 0;

    private GameSaveData _active;

    public GameSaveData Active => _active;

    private static string SaveDirectory =>
        Path.Combine(Application.persistentDataPath, "MidnightMeow", "saves");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadOrCreate(defaultSlot);

        try
        {
            ServiceLocator.RegisterService(this);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveProfileStore: ServiceLocator: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasSave(int slot = 0)
    {
        return File.Exists(GetPath(slot));
    }

    /// <summary>
    /// Continuar só é permitido se existir save e o jogador era host da sessão anterior.
    /// </summary>
    public bool CanContinue(int slot = 0)
    {
        if (!HasSave(slot))
            return false;

        GameSaveData data = LoadFromDisk(slot);
        return data != null && data.wasHost;
    }

    public void LoadOrCreate(int slot = 0)
    {
        _active = LoadFromDisk(slot) ?? CreateFresh(slot);
        _active.slotIndex = slot;
        OnProfileChanged?.Invoke();
    }

    public void SaveActive()
    {
        if (_active == null)
            return;

        Directory.CreateDirectory(SaveDirectory);
        _active.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
        string json = JsonUtility.ToJson(_active, prettyPrint: true);
        File.WriteAllText(GetPath(_active.slotIndex), json);
        OnProfileChanged?.Invoke();
    }

    public void ResetActive()
    {
        _active = CreateFresh(_active?.slotIndex ?? defaultSlot);
        SaveActive();
    }

    public bool TrySpendMagiculas(int cost)
    {
        if (_active == null || cost <= 0)
            return cost <= 0;

        if (_active.magiculas < cost)
            return false;

        _active.magiculas -= cost;
        SaveActive();
        return true;
    }

    public void AddMagiculas(int amount)
    {
        if (_active == null || amount <= 0)
            return;

        _active.magiculas += amount;
        SaveActive();
    }

    public void SetSelectedCharacter(LobbyCharacterType type)
    {
        if (_active == null)
            return;

        if (type == LobbyCharacterType.CharacterB)
            _active.cora.characterType = type;
        else if (type == LobbyCharacterType.CharacterA)
            _active.nix.characterType = type;

        SaveActive();
    }

    public LobbyCharacterType GetSelectedCharacter()
    {
        if (_active == null)
            return LobbyCharacterType.CharacterA;

        return _active.SelectedCharacter;
    }

    public AbilityProgressionState BuildProgressionState(LobbyCharacterType type)
    {
        CharacterSaveData saved = _active?.GetCharacterData(type) ?? new CharacterSaveData();
        return new AbilityProgressionState
        {
            phaseIndex = 3,
            ability1Unlocked = true,
            ability2Unlocked = true,
            primaryTier = saved.primaryTier,
            ability1Tier = saved.ability1Tier,
            ability2Tier = saved.ability2Tier
        };
    }

    public void ApplyProgressionState(LobbyCharacterType type, AbilityProgressionState state)
    {
        if (_active == null || state == null)
            return;

        CharacterSaveData saved = _active.GetCharacterData(type);
        saved.primaryTier = state.primaryTier;
        saved.ability1Tier = state.ability1Tier;
        saved.ability2Tier = state.ability2Tier;
        SaveActive();
    }

    private static GameSaveData CreateFresh(int slot)
    {
        return new GameSaveData
        {
            slotIndex = slot,
            magiculas = 2,
            nix = new CharacterSaveData { characterType = LobbyCharacterType.CharacterA },
            cora = new CharacterSaveData { characterType = LobbyCharacterType.CharacterB }
        };
    }

    private static GameSaveData LoadFromDisk(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data != null)
                data.slotIndex = slot;
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveProfileStore: falha ao ler slot {slot}: {ex.Message}");
            return null;
        }
    }

    private static string GetPath(int slot) =>
        Path.Combine(SaveDirectory, $"save_slot_{slot}.json");
}
