# Concluídas — Gameplay

## Dash sem colisão/dano com inimigos

**Implementação (2026-06-19):**

- Colisão: `dashPassThroughLayers` nos perfis (`CharacterProfileApplier` → `PlayerDash.ApplyPassThroughLayers`).
- Dano: `HealthComponent` ignora dano se `PlayerDash.IsDashing` ou `NetworkPlayerAbilityRelay.NetworkIsDashing`; `NetworkPlayerHealth.ServerApplyExternalDamage` também bloqueia; invulnerabilidade extra via `SetInvulnerableFor` no início do dash.
