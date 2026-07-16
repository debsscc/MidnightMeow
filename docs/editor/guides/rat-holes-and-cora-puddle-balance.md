# Guia Editor — Limite de Rat Holes + Poça da Cora

Última revisão: 2026-07-16

Guia manual pós-refatoração de código. Não altera assets automaticamente além dos valores já migrados; use este checklist no Unity Editor.

---

## 1. Limite máximo de ratos (`maxRatsAlive`)

### Onde configurar

| Asset | Caminho |
|-------|---------|
| Catálogo de fases | `Assets/Resources/PhaseWaveSettingsCatalog.asset` |

1. Selecione o asset no Project.
2. Em cada entrada de `Phases` (Fase-1, Fase-2, Fase-3), ajuste **Max Rats Alive**.
3. Valores atuais migrados do antigo `maxEnemiesAlive`:

| Fase | `maxRatsAlive` (padrão atual) |
|------|-------------------------------|
| Fase-1 | 35 |
| Fase-2 | 40 |
| Fase-3 | 1 (boss; hole spawn desligado) |

### O que o código faz

- `NetworkWaveManager` / `LocalRatHoleSpawnService` contam ratos vivos.
- Se `vivos >= maxRatsAlive`, o spawn por buraco **não gera** novo rato (timer do buraco fica em espera).
- Perfis por buraco (`RatHoleSpawnProfile` em `Assets/Data/Gameplay/`) controlam **intervalo e tipos**, não o teto global.

### Teste rápido

1. Abra `Fase-1`, Play Mode.
2. Baixe temporariamente `maxRatsAlive` para `3`.
3. Confirme que, após 3 ratos vivos, nenhum buraco spawna até algum morrer.

---

## 2. Pausa de spawn durante selamento

Não há campo novo no Inspector para isso. Comportamento automático:

1. Jogador pressiona **E** → sessão de selamento ativa → `RatHoleSpawnPoint.IsBeingSealed = true`.
2. O `RatHoleSpawnController` daquele buraco **pausa o timer** (não avança delay, não spawna).
3. Cancelamento por abandono (`abandonTimeout` em `RatHoleSealConfig`) ou conclusão do selamento → `IsBeingSealed = false` (ou buraco selado para de spawnar via `CanSpawn`).

Config de selamento: `Assets/Resources/RatHoleSealConfig.asset`.

---

## 3. Poça da Cora — `castRange` vs `puddleRadius`

### Onde configurar

| Asset | Caminho |
|-------|---------|
| Definição da Poça | `Assets/Data/Abilities/Definitions/CoraPoolAbility.asset` |
| Set da Cora (referência) | `Assets/Data/Abilities/CoraAbilitySet.asset` → `ability2` |

Em cada tier (`Tier 1` / `Tier 2` / `Tier 3`):

| Campo | Uso |
|-------|-----|
| **Cast Range** | Distância máxima do mouse/mira a partir da Cora |
| **Puddle Radius** | Raio matemático de dano + telegraph visual da poça |
| **Range** (legado) | Fallback se `castRange` ou `puddleRadius` forem `0` |

Valores iniciais (paridade com o antigo `range` único):

| Tier | Cast Range | Puddle Radius |
|------|------------|---------------|
| 1 | 4 | 4 |
| 2 | 5.6 | 5.6 |
| 3 | 7.2 | 7.2 |

Agora você pode, por exemplo, manter cast longo (`castRange: 8`) e poça pequena (`puddleRadius: 2.5`) sem um afetar o outro.

### Teste rápido

1. Em Tier 1, defina `castRange = 8` e `puddleRadius = 2`.
2. Play Mode com Cora: a poça só pode ser lançada longe, mas o círculo de dano/telegraph fica pequeno.

---

## 4. Visual da Poça (Prefab) alinhado ao `puddleRadius`

### Prefab

`Assets/Prefabs/Combat/CoraDamagePool.prefab`

### Como o código escala o visual

Em `CoraDamagePool.Initialize`:

1. Lê `puddleRadius` (via `ResolvePuddleRadius()`).
2. Calcula diâmetro alvo = `puddleRadius × 2 × visualScaleMultiplier`.
3. Ajusta `transform.localScale` do sprite para esse diâmetro.
4. Ajusta o `CircleCollider2D.radius` em espaço local para bater com o raio mundial.

Campo no componente: **Visual Scale Multiplier** (padrão `0.8`).

### Ajuste recomendado no Editor

1. Abra o prefab `CoraDamagePool`.
2. Deixe **Scale do Transform em (1,1,1)** — o runtime sobrescreve o scale.
3. Se o sprite “parecer” maior/menor que o gizmo de dano:
   - Ajuste só **Visual Scale Multiplier** (ex.: `1.0` = sprite cobre o raio exato; `< 1` = sprite menor que o AoE).
4. Se houver **Particle System** filho:
   - Preferir Shape Radius / Start Size em unidades de mundo compatíveis com `puddleRadius`.
   - Ou marcar **Scaling Mode = Hierarchy** e deixar o scale do pai (controlado pelo código) redimensionar as partículas.
5. Confirme no Scene View (Gizmos do prefab / Play Mode) que o círculo magenta de debug cobre a mesma área que o VFX.

### Checklist visual

- [ ] Telegraph (`AbilityDebugVisualHost`) ≈ tamanho do sprite/partículas
- [ ] Inimigos só tomam dano dentro do círculo visual esperado
- [ ] Tiers 1–3 mudam o tamanho visual quando `puddleRadius` muda (sem editar scale manual do prefab)

---

## Referências de código

| Sistema | Scripts |
|---------|---------|
| Teto de ratos | `PhaseWaveSettingsCatalog`, `NetworkWaveManager`, `LocalRatHoleSpawnService`, `RatHoleSpawnController` |
| Pausa no selamento | `RatHoleSpawnPoint.IsBeingSealed`, `NetworkRatHoleSealManager`, `RatHoleSpawnController` |
| Poça | `AbilityTierData`, `PlayerAbilityHandler`, `CoraDamagePool`, `AbilityDebugVisualHost` |
