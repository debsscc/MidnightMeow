using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Mapeia cenas Fase-* para spawn, mecânicas e condição de vitória.
/// </summary>
[CreateAssetMenu(fileName = "PhaseWaveSettingsCatalog", menuName = "MidnightMeow/Gameplay/Phase Wave Settings Catalog")]
public class PhaseWaveSettingsCatalog : ScriptableObject
{
    public enum PhaseWinCondition
    {
        SealAllHoles,
        CarriageReachEnd,
        KillBoss
    }

    [System.Serializable]
    public class PhaseEntry
    {
        public string sceneName = "Fase-1";
        public WaveSettings waveSettings;
        public bool enableRatHoleSealing;
        public bool enableCarriage;

        [Header("Spawn")]
        [Tooltip("Perfil padrão aplicado a buracos sem SO próprio.")]
        public RatHoleSpawnProfile defaultHoleSpawnProfile;
        [Tooltip("Quando desligado, ondas não são usadas.")]
        public bool useWaveSpawning;
        [Tooltip("Spawn contínuo por buracos não selados.")]
        public bool useHoleSpawning = true;
        public float holeSpawnInterval = 4f;

        [Tooltip("Limite global de ratos vivos na fase. Spawn por buraco aborta se o contador atingir este valor.")]
        [Min(1)]
        [FormerlySerializedAs("maxEnemiesAlive")]
        public int maxRatsAlive = 35;

        public float firstSpawnDelay = 3f;

        [Header("Vitória")]
        public PhaseWinCondition winCondition = PhaseWinCondition.SealAllHoles;
    }

    public PhaseEntry[] phases;

    private static PhaseWaveSettingsCatalog _cached;

    public static PhaseWaveSettingsCatalog LoadCached()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<PhaseWaveSettingsCatalog>("PhaseWaveSettingsCatalog");
        return _cached;
    }

    public bool TryGetEntry(string sceneName, out PhaseEntry entry)
    {
        entry = null;
        if (phases == null || string.IsNullOrEmpty(sceneName))
            return false;

        for (int i = 0; i < phases.Length; i++)
        {
            if (phases[i] != null && phases[i].sceneName == sceneName)
            {
                entry = phases[i];
                return true;
            }
        }

        return false;
    }
}
