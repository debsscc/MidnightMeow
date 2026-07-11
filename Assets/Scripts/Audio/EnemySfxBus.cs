// ----------------------------------------------------------------
// FEITO POR: Debs Carvalho
// DATA: 09/07/2026
// DESCRIÇÃO: Bus global de SFX de inimigos com limite de burst (estilo horde / Vampire Survivors).
// ----------------------------------------------------------------

using UnityEngine;

public enum EnemySfxKind
{
    Attack,
    Damage,
    Death
}

public static class EnemySfxBus
{
    private const int PoolSize = 8;

    private static GameObject _root;
    private static AudioSource[] _pool;
    private static int _poolIndex;

    private static BurstWindow _attackWindow = new BurstWindow(0.18f, 5);
    private static BurstWindow _damageWindow = new BurstWindow(0.14f, 6);
    private static BurstWindow _deathWindow = new BurstWindow(0.2f, 4);

    private static EnemyCommonSfxConfig _config;

    public static EnemyCommonSfxConfig Config
    {
        get
        {
            if (_config == null)
                _config = Resources.Load<EnemyCommonSfxConfig>("EnemyCommonSfxConfig");
            return _config;
        }
    }

    public static void Play(EnemySfxKind kind, Vector3 worldPosition, AudioClip clipOverride = null, float volumeMultiplier = 1f)
    {
        EnemyCommonSfxConfig config = Config;
        AudioClip clip = clipOverride ?? ResolveClip(kind, config);
        if (clip == null)
            return;

        BurstWindow window = GetWindow(kind);
        if (!window.TryConsume())
            return;

        float baseVolume = ResolveBaseVolume(kind, config) * volumeMultiplier;
        float stackAttenuation = window.GetStackAttenuation();
        float pitch = config != null ? config.SamplePitch() : Random.Range(0.92f, 1.08f);

        AudioSource source = GetPooledSource();
        source.transform.position = worldPosition;
        source.pitch = pitch;
        source.spatialBlend = kind == EnemySfxKind.Attack ? 0.45f : 0.25f;
        source.minDistance = 2.5f;
        source.maxDistance = 22f;
        if (!GameAudioSettings.BindSfxOutput(source))
            Debug.LogWarning($"[EnemySfxBus] Falha ao rotear {kind} para o grupo SFX do AudioMixer.");
        source.PlayOneShot(clip, baseVolume * stackAttenuation);
    }

    public static void PlayAttack(Vector3 worldPosition, AudioClip clipOverride = null) =>
        Play(EnemySfxKind.Attack, worldPosition, clipOverride);

    public static void PlayDamage(Vector3 worldPosition, AudioClip clipOverride = null) =>
        Play(EnemySfxKind.Damage, worldPosition, clipOverride);

    public static void PlayDeath(Vector3 worldPosition, AudioClip clipOverride = null) =>
        Play(EnemySfxKind.Death, worldPosition, clipOverride);

    private static AudioClip ResolveClip(EnemySfxKind kind, EnemyCommonSfxConfig config)
    {
        if (config == null)
            return null;

        return kind switch
        {
            EnemySfxKind.Attack => config.PickAttackClip(),
            EnemySfxKind.Damage => config.damageClip,
            EnemySfxKind.Death => config.deathClip,
            _ => null
        };
    }

    private static float ResolveBaseVolume(EnemySfxKind kind, EnemyCommonSfxConfig config)
    {
        if (config == null)
            return 0.7f;

        return kind switch
        {
            EnemySfxKind.Attack => config.attackVolume,
            EnemySfxKind.Damage => config.damageVolume,
            EnemySfxKind.Death => config.deathVolume,
            _ => 0.7f
        };
    }

    private static BurstWindow GetWindow(EnemySfxKind kind)
    {
        return kind switch
        {
            EnemySfxKind.Attack => _attackWindow,
            EnemySfxKind.Damage => _damageWindow,
            _ => _deathWindow
        };
    }

    private static AudioSource GetPooledSource()
    {
        EnsurePool();
        AudioSource source = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % PoolSize;
        return source;
    }

    private static void EnsurePool()
    {
        if (_pool != null)
            return;

        _root = new GameObject(nameof(EnemySfxBus));
        Object.DontDestroyOnLoad(_root);
        _pool = new AudioSource[PoolSize];

        for (int i = 0; i < PoolSize; i++)
        {
            GameObject child = new GameObject($"Source_{i}");
            child.transform.SetParent(_root.transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            _pool[i] = source;
        }

        GameAudioSettings.EnsureExists();
    }

    // Janela de burst para limitar o número de SFX de inimigos que podem tocar ao mesmo tempo.
    private sealed class BurstWindow
    {
        private readonly float _windowSeconds;
        private readonly int _maxBurst;
        private float _windowStart;
        private int _count;

        public BurstWindow(float windowSeconds, int maxBurst)
        {
            _windowSeconds = windowSeconds;
            _maxBurst = Mathf.Max(1, maxBurst);
        }
    // Tenta consumir um slot na janela, retorna false se a janela está cheia.
        public bool TryConsume()
        {
            float now = Time.unscaledTime;
            if (now - _windowStart > _windowSeconds)
            {
                _windowStart = now;
                _count = 0;
            }

            if (_count >= _maxBurst)
                return false;

            _count++;
            return true;
        }

        // Atribui atenuação baseada no número de SFX já tocados nesta janela.
        public float GetStackAttenuation()
        {
            if (_count <= 2)
                return 1f;

            int overflow = _count - 2;
            return 1f / (1f + overflow * 0.22f);
        }
    }
}
