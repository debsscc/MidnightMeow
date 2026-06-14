# Perfis de personagem e animação (data-driven)

Última revisão: 2026-06-14

## Objetivo

Centralizar balanceamento e animações de jogadores/inimigos em ScriptableObjects com **interface homogênea**, evitando campos espalhados em prefabs e componentes.

## CharacterGameplayProfile

**Caminho:** `Assets/Data/Characters/{Personagem}GameplayProfile.asset`  
**Código:** `CharacterGameplayProfile.cs`  
**Aplicação:** componente `CharacterProfileApplier` no prefab (ordem `-90`).

### Seções do Inspector (prioridade)

| Seção | Conteúdo |
|-------|----------|
| Identidade | Nome de exibição |
| Movimento e Vitalidade | `PlayerCoreStats` — vida, movimento, dash, adrenalina, munição |
| Ataque Principal | Modo (`Ranged` / `Melee`) + `CoraRangedCombatStats` ou `NixieMeleeCombatStats` |
| Habilidades | `CharacterAbilitySet` |
| Animações | `CharacterAnimationProfile` |
| Configurações Avançadas | Layers de inimigo, máscara do dash, failsafe |

### Personagens atuais

| Personagem | Profile | Ataque |
|------------|---------|--------|
| Cora | `CoraGameplayProfile.asset` | Ranged (`CoraRangedCombatStats.asset` — inclui **attackRange**) |
| Nixie | `NixieGameplayProfile.asset` | Melee (`NixieMeleeCombatStats.asset`) |

Ambos compartilham `PlayerCoreStats.asset` para dash, movimento e adrenalina.

## CharacterAnimationProfile + AnimatorProfileBinder

**Caminho:** `Assets/Data/Characters/{Personagem}AnimationProfile.asset`  
**Código:** `CharacterAnimationProfile.cs`, `AnimatorProfileBinder.cs`  
**Aplicação:** `AnimatorProfileBinder` no mesmo GameObject do `Animator` (ordem `-100`).

### Como trocar animações

1. Duplique ou edite o `CharacterAnimationProfile` do personagem.
2. Defina `baseController` (ex.: `AC_Player.controller` ou `AC_Enemy.controller`).
3. Preencha `clipOverrides` mapeando **nome do clip original** no controller → `AnimationClip` desejado.
4. Adicione `AnimatorProfileBinder` no prefab do inimigo (mesmo GO do `Animator`).
5. Em runtime, `BuildRuntimeController()` gera um `AnimatorOverrideController`.
6. `PlayerAnimationHandler` / `EnemyAnimationHandler` leem hashes e tempos do binder.

Template inimigo: `Assets/Data/Characters/EnemyDefaultAnimationProfile.asset` (controller `AC_Enemy`).

Triggers e floats (`MoveSpeed`, `OnShoot`, `OnAbility1`, etc.) podem ser renomeados no SO se o Animator usar nomes diferentes.

## Checklist ao criar personagem novo

- [ ] `CharacterGameplayProfile` em `Assets/Data/Characters/`
- [ ] `CharacterAnimationProfile` com controller base + overrides
- [ ] `CharacterProfileApplier` + `AnimatorProfileBinder` no prefab
- [ ] Atualizar `docs/editor/prefabs/{Personagem}.md`

## Relacionado

- [01-data-driven.md](01-data-driven.md)
- [character-abilities-implementation.md](../combat/character-abilities-implementation.md)
