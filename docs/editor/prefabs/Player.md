# Prefab: Player

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Characters/Player.prefab`

## Resumo

Personagem jogável (Nyxie): movimento 2D, tiro, dash, adrenalina/frenzy, áudio e stack **Netcode** para multiplayer.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Nome | `Player` |
| Tag | `Player` |
| Layer | `Player` (3) |

## Hierarquia (filhos principais)

```
Player
├── Audios
│   ├── loop_audio
│   └── sfx_audio
├── (visual / firePoint / colliders — ver prefab no Unity)
└── Shadow (Layer: Shadow)
```

## Componentes Unity (raiz)

| Componente | Notas |
|------------|--------|
| `Transform` | |
| `Rigidbody2D` | Física 2D |
| `Collider2D` | Hitbox |
| `SpriteRenderer` / filhos | Arte do gato |
| `Animator` | Controller: ver GUID `a6e00dd278b03d443bc782d709d04f16` |
| `PlayerInput` | Input System, mapa `Gameplay` |
| `Canvas` + `CanvasScaler` + `GraphicRaycaster` | UI world-space ligada ao player |
| `NetworkObject` | Replicação NGO |
| `OwnerNetworkTransform` | Transform com autoridade do dono |

## Scripts de gameplay (Assembly-CSharp)

| Script | Responsabilidade | Referências notáveis |
|--------|------------------|----------------------|
| `PlayerInputHandler` | Ponte input → lógica | |
| `PlayerMovement` | Movimento | `stats` → DefaultPlayerStats |
| `PlayerAmmo` | Munição | `stats` |
| `PlayerShooting` | Disparo | `projectilePrefab` → Projectile/NetworkProjectile |
| `PlayerAim` | Mira | `firePoint`, `stats` |
| `PlayerAbilityHandler` | Habilidade ativa | `firePoint`, `currentAbility` |
| `PlayerAnimationHandler` | Animações | refs a shooting, health, movement |
| `HealthComponent` | Vida | `_maxHealth` inicial 100 |
| `PlayerAdrenaline` | Frenzy | `stats`, UnityEvents |
| `SpriteBlink` | Feedback dano | |
| `PlayerInitializer` | Aplica upgrades/progression | ver SOs abaixo |
| `PlayerAudioController` | SFX/loop | clips de movimento/tiro/dash |
| `PlayerDash` | Dash | `passThroughLayer` = DashableWall |
| `KnockbackReceiver` | Knockback | force 8, duration 0.15 |

## Scripts multiplayer

| Script | Notas |
|--------|--------|
| `NetworkPlayerController` | Orquestra input/move/shoot no cliente autoritativo |
| `NetworkPlayerHealth` | `config` SO |
| `NetworkPlayerAdrenaline` | Sincroniza adrenalina |
| `NetworkPlayerSpectator` | Modo espectador |
| `NetworkProjectileSpawner` | Spawn de projétil em rede |
| `MultiplayerCombatIntegrityLogger` | Logs de diagnóstico (`ativo: 1`) |

## ScriptableObjects

| Campo | Asset | Caminho |
|-------|-------|---------|
| `baseStats` / `stats` | DefaultPlayerStats | `Assets/Data/Stats/Player/DefaultPlayerStats.asset` |
| `progressionData` | *(instância no prefab)* | Ver GUID `b87f7c79296088641991071b4e517b5c` |
| `upgradeDefinitions[3]` | Upgrades Health/FireRate/Dash | `Assets/Data/` (upgrade assets) |
| `NetworkPlayerHealth.config` | Player health config MP | GUID `50a79734eaf520a409e26b037cab7b62` |

## Prefabs referenciados

| Uso | Prefab |
|-----|--------|
| Projétil | `NetworkProjectile` / `Projectile` (GUID `eadee2043abe1c540b4356dff9dbd9a7`) |

## Configuração esperada

- `PlayerInitializer.playerDash` deve referenciar `PlayerDash` (verificar se não está `None`).
- `NetworkPlayerController.playerCamera` atribuída em runtime pelo bootstrap.
- Cores de jogador (`playerColors`): 4 entradas para até 4 jogadores.

## Modo multiplayer

- [x] `NetworkObject`
- [x] `OwnerNetworkTransform`
- `NetworkPlayerHealth` — inconsciência + timer + bleed out
- `NetworkPlayerRevive` — segurar **Interact**; reviver pausa timer; progresso decai ao soltar
- `DownedPlayerWorldUI` / `RevivePromptWorldUI` — barras world-space (via `PlayerGameplayModuleInstaller`)
- `PlayerDamageImmunity` — i-frames e atravessar inimigos após dano
- Dono executa movimento/disparo; health/adrenalina com componentes de rede dedicados.

## Config (SO)

- `MultiplayerConfig.downedPlayerConfig` → `Assets/Data/Multiplayer/DownedPlayerConfig.asset`
- Campos: `unconsciousDuration`, `reviveHoldDuration`, `reviveProgressDecayPerSecond`, `reviveRange`, `reviveHealthFraction`

## Variante corpo a corpo

Ver [Player_Melee.md](Player_Melee.md).

## Histórico

| Data | Alteração |
|------|-----------|
| 2026-05-22 | Doc inicial na reorganização do projeto |
| 2026-05-22 | GUID do prefab restaurado (`b18ed4e45e4d20a4dbdac339b666e689`) após movimentação |

## Valores a confirmar no Editor

Preencha após revisar o Inspector (envie os valores para atualizar esta doc):

| Script | Campo | Valor atual |
|--------|-------|-------------|
| PlayerInitializer | playerDash | |
| NetworkPlayerController | playerCamera | |
| PlayerShooting | projectilePrefab | |
| NetworkProjectileSpawner | networkProjectilePrefab | |
