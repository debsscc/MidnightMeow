# Guia Editor — Passiva Stun (Nixie) + Splash (Cora)

Última revisão: 2026-07-13

Após puxar os scripts desta refatoração, configure os assets/prefabs no Unity. **Não** há criação procedural de prefabs no código: as referências precisam ser ligadas no Inspector.

---

## 1. ScriptableObjects (valores de balanceamento)

### 1.1 Nixie — Stun

1. Abra `Assets/Data/Abilities/NixPassiveConfig.asset`.
2. Confirme / ajuste:
   - `Kills Required` = `5`
   - `Passive Duration` = `5`
   - **`Stun Duration`** = tempo do stun **após** o knockback (sugestão inicial: `1.25`)
   - Splash: deixe `Splash Count = 0` (Nix não usa splash)

O raio/shader do ataque melee **não** muda mais com a passiva. Os valores de alcance ficam só em `Assets/Data/Stats/Player/NixieMeleeCombatStats.asset`.

Cores do visual com passiva ativa continuam em `Assets/Data/Combat/NixieMeleeHitVisual.asset` (`passiveFillColor`, etc.).

### 1.2 Cora — Respingo / Splash

1. Abra `Assets/Data/Abilities/CoraPassiveConfig.asset`.
2. Confirme / ajuste:
   - `Kills Required` = `5`
   - `Passive Duration` = `5`
   - **`Splash Count`** — qtd. de sub-projéteis (ex.: `3`)
   - **`Splash Range`** — raio de busca em unidades Unity (ex.: `4`)
   - **`Splash Damage Percentage`** — fração do dano original (ex.: `0.5` = 50%)
   - **`Prioritize Different Enemies`** — `true` distribui alvos; se faltar, reutiliza
   - `Stun Duration` = `0` (Cora não aplica stun pela passiva)

O ricochete da passiva **foi removido**. Bounce de adrenaline/frenzy (se existir) permanece independente.

---

## 2. Prefab do sub-projétil (Splash)

### 2.1 Duplicar o projétil de rede

1. Em `Assets/Prefabs/Combat/`, localize o prefab de rede usado pela Cora (mesmo referenciado em `NetworkProjectileSpawner.networkProjectilePrefab` — tipicamente o Network Projectile / variante com `NetworkObject`).
2. Duplique (Ctrl+D) e renomeie para algo como `SplashProjectile` / `NetworkSplashProjectile`.
3. No prefab duplicado, garanta:
   - `NetworkObject`
   - `NetworkTransform` (ou o mesmo setup do projétil pai)
   - `NetworkProjectileController`
   - `Projectile` + `Rigidbody2D` + `Collider2D` + Animator/VFX
4. **Não** precisa de script C# novo: o `Projectile.ConfigureAsSplashSeeker` já trata o teleguiado.
5. Opcional: scale menor / sprite distinto para ler como “respingo”.

### 2.2 Registrar no NetworkManager (obrigatório em NGO)

1. Abra a cena/bootstrap com `NetworkManager`.
2. Em **Network Prefabs List**, adicione o novo prefab de splash.
3. Sem isso, `NetworkObject.Spawn` do respingo falha em runtime.

### 2.3 Referenciar no lançador (Cora)

No prefab `Assets/Prefabs/Characters/Cora.prefab`:

| Componente | Campo | Valor |
|------------|--------|--------|
| `NetworkProjectileSpawner` | `Network Projectile Prefab` | projétil principal (inalterado) |
| `NetworkProjectileSpawner` | **`Network Splash Projectile Prefab`** | o prefab duplicado da §2.1 |
| `NetworkProjectileSpawner` | `Splash Enemy Layers` | layer **Enemy** |
| `PlayerShooting` | `Splash Projectile Prefab` | mesma duplicata (offline / fallback) |
| `PlayerShooting` | `Splash Enemy Layers` | layer **Enemy** |

Ataque normal sem passiva segue destruindo no inimigo/parede (`maxBounces` do `ProjectileStats`).

---

## 3. Inimigos — estado de Stun

### 3.1 Já coberto em código (sem setup mínimo)

- `EnemyHitStun` — timer local
- `EnemyMovement` — para NavMesh enquanto `IsStunned`
- `EnemyTelegraphedAttacker` — não inicia ataque e **cancela** o padrão em andamento
- `NetworkEnemyController`:
  - `ApplyKnockbackThenStunRpc` (fluxo Nix: dano → knockback → stun)
  - `NetworkVariable<bool>` `_networkIsCombatStunned` replicada

Não é obrigatório criar um estado novo de Animator para a mecânica funcionar.

### 3.2 Visual opcional no Animator

Se quiser pose/idle de stun:

1. Abra o Animator Controller do rato (ex.: o usado em `Rato_*.prefab`).
2. Crie parâmetro **Bool** exatamente: `IsStunned`.
3. Transição Idle/Run → Stun quando `IsStunned == true`; saída quando `false`.
4. `NetworkEnemyController` seta esse bool nos clientes via `NetworkVariable`.

Se o parâmetro **não** existir, o código ignora (sem erro).

### 3.3 Checklist por variante de inimigo

Em cada `Rato_*.prefab` / `Enemy.prefab`:

- [ ] `EnemyHitStun` presente
- [ ] `NetworkEnemyController` presente (MP)
- [ ] (Opcional) parâmetro `IsStunned` no Animator

---

## 4. Smoke test rápido

1. **Nixie sem passiva:** um golpe no trapézio acerta **todos** os inimigos na forma (não só 1).
2. **Nixie com passiva (5 kills):** knockback → stun (~`Stun Duration`); inimigo para de andar/atacar.
3. **Cora sem passiva:** projétil some em inimigo/parede; sem ricochete de passiva.
4. **Cora com passiva:** no impacto, somem o projétil pai e nascem N respingos teleguiados no `Splash Range`.
5. **MP:** splash só spawna no servidor; clientes veem via NetworkTransform.
6. **Splash sem alvo / miss:** respingo segue a direção do impacto; some em parede ou ao atingir `ProjectileStats.maxDistance` (igual ao projétil principal).

---

## Referências de código

| Área | Scripts |
|------|---------|
| SO passiva | `PassiveAbilityConfig`, assets `NixPassiveConfig` / `CoraPassiveConfig` |
| Nix melee | `PlayerMeleeCombat`, `MeleeAttackVisual` |
| Stun inimigo | `EnemyHitStun`, `NetworkEnemyController`, `EnemyTelegraphedAttacker` |
| Splash | `Projectile`, `ProjectileSplashUtility`, `NetworkProjectileSpawner`, `NetworkProjectileController` |
