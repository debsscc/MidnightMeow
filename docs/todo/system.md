
# Dash dos Personagens
- ~~O nixie deve ter um cooldown de dash menor que o da cora mas percorrer uma distância menor.~~ **Feito:** `NixieCoreStats` (cooldown 0.65s, distância menor via dashDuration 0.14 × dashSpeed 20) vs `CoraCoreStats` (cooldown 1.1s, dash mais longo).
- ~~Nixie e Cora devem poder ter "Cargas" de dash~~ **Feito:** `PlayerStats.maxDashCharges` + sistema de cargas/recarga em `PlayerDash.cs` (Nixie: 2 cargas, Cora: 1).
- ~~A quantidade máxima de cargas deve ser configurável e deve ser possível melhorar com upgrades~~ **Feito:** configurável via SO; `PlayerDash.SetDashChargeBonus(int)` exposto para upgrades futuros.

# Defesa dos Inimigos
- ~~Os inimigos devem ter Ranged Defense~~ **Feito:** campo `rangedDefense` em `EnemyStats`; enum `DamageType` (Melee/Ranged/Generic); `DamageDefenseUtility` + propagação em projéteis, melee e habilidades.

# Escudo dos Inimigos - EM ANÁLISE
- Além da Vida, os inimigos tem que ter um "Escudo". Uma sobrevida que pode ser quebrada pela Investida do Nixie.

# Sistema de Selamento - EM ANÁLISE
- Permitir parar o spawn dos ratos

# Mecânica de Reviver - EM TESTE

# Mecânica de Carruagem - EM ANÁLISE
