# Data-driven design

## Objetivo

Permitir que **game designers** balancem o jogo sem abrir código. Valores de gameplay não devem ficar espalhados como números mágicos em `MonoBehaviour`.

## Regras

1. **Toda variável importante de balanceamento** vive em `ScriptableObject` (SO) ou em assets em `Assets/Data/`.
2. **MonoBehaviours** leem SOs via `[SerializeField]` ou injeção no `Awake`/`Start` (ex.: `PlayerInitializer`).
3. **Proibido** em produção: `if (health < 37.5f)` sem constante nomeada ou campo de SO.
4. **Prefabs** referenciam assets por GUID (arrastar no Inspector); agentes devem documentar essas referências em `docs/editor/prefabs/`.

## Onde colocar assets

| Tipo | Pasta sugerida |
|------|----------------|
| Stats de jogador/inimigo/projétil | `Assets/Data/Stats/` |
| Ondas, upgrades, config global | `Assets/Data/` (subpastas por domínio) |
| Config multiplayer | SO em `Scripts/.../ScriptableObjects/` + instância em `Data/` |

## Padrão de ScriptableObject

```csharp
[CreateAssetMenu(fileName = "NewXStats", menuName = "MidnightMeow/Stats/X Stats")]
public class XStats : ScriptableObject
{
    [Tooltip("Descrição clara para o designer.")]
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
}
```

## Exemplos no projeto

- `PlayerStats` → `Assets/Data/Stats/Player/DefaultPlayerStats.asset`
- `EnemyStats`, `ProjectileStats`, `UpgradeDefinition`, `WaveSettings`, `GameConfig`

## Para agentes de IA

Ao criar mecânica nova: defina o SO primeiro, crie o asset em `Data/`, documente o asset no markdown do prefab que o consome, e só então implemente o comportamento que **lê** esses valores.
