# Guia Editor — Knockback sem Tunneling (inimigos)

Última revisão: 2026-07-17

Corrige inimigos atravessando paredes no knockback da Nixie (e Q / utilitários). O código já aplica força via `Rigidbody2D` (sem `transform.position`). Este guia calibra os prefabs no Inspector.

**Prefabs alvo:** todos em `Assets/Prefabs/Enemies/` (`Rato_Padrao_Base`, `Veloz`, `Resistente`, `Eletrico`, `Acido`, `Rato_Boss`, `Enemy.prefab`).

---

## Contexto rápido (por que não deixar Dynamic o tempo todo)

| Modo | Quando | Motivo |
|------|--------|--------|
| **Kinematic** | Locomoção / perseguição | `NavMeshAgent` move o transform; `EnemyPhysicsBody` espelha com `MovePosition` |
| **Dynamic** | Só durante knockback | Código chama `EnemyPhysicsBody.BeginExternalPhysics()` — paredes bloqueiam o impulso |

Não force **Dynamic permanente** no prefab se o inimigo usa NavMesh: Agent e Rigidbody Dynamic brigam pela posição. O runtime já troca para Dynamic no knockback e volta para Kinematic.

---

## 1. Rigidbody2D — checklist por prefab

Abra cada `Rato_*.prefab` → componente **Rigidbody 2D** no root:

### 1.1 Body Type

| Campo | Valor recomendado no prefab | Notas |
|-------|------------------------------|--------|
| **Body Type** | `Kinematic` | Padrão de locomoção. Em Play Mode, durante knockback, o código muda para `Dynamic` sozinho. |
| **Simulated** | ✔ ligado | |
| **Gravity Scale** | `0` | Top-down 2D |
| **Constraints** | Freeze Rotation **Z** | Evita tombamento no impacto |

> **Exceção visual no Inspector em Play:** se você inspecionar um rato *no meio* do knockback, verá `Dynamic` — isso é esperado.

### 1.2 Collision Detection (crítico contra tunneling)

1. No **Rigidbody 2D**, localize **Collision Detection**.
2. Se estiver em `Discrete`, mude para **`Continuous`**.
3. Salve o prefab (Ctrl+S / Apply se for variante).

| Modo | Uso |
|------|-----|
| `Discrete` | **Evitar** — em alta velocidade o corpo “pula” para dentro da parede e só depois a física ejeta |
| `Continuous` | **Usar** — varre o trajeto entre frames contra colisores estáticos (paredes) |

> Em **Physics 2D** não existe `Continuous Dynamic` (isso é 3D). Em 2D a opção correta é **`Continuous`**.

O script `EnemyPhysicsBody` também força `Continuous` em runtime; ainda assim configure o prefab para o valor serializado já nascer certo.

### 1.3 Linear Damping (fricção / “freio”)

| Campo | Valor sugerido no prefab | Efeito |
|-------|--------------------------|--------|
| **Linear Damping** | `0` … `1` | Em Kinematic quase não importa; é o valor “de descanso” |
| **Knockback Linear Damping** (em `EnemyPhysicsBody`) | `3` (default) | Drag **só** enquanto Dynamic no knockback |

Ajuste fino do “deslize” do arremesso:

1. Selecione o root do inimigo.
2. Componente **Enemy Physics Body** → **Knockback Linear Damping**.
3. Sugestões:
   - `2` — knockback mais “longo”, ainda freia após bater na parede
   - `3` — equilíbrio (default no código)
   - `5`–`8` — para rápido após o impacto (pode encurtar a distância efetiva)

Opcional — **Physics Material 2D** no Collider do inimigo ou da parede:

1. `Create → 2D → Physics Material 2D` (ex.: `EnemyKnockback.physicsMaterial2D`).
2. **Friction** ≈ `0.4`–`0.6`, **Bounciness** ≈ `0`.
3. Atribua em **Capsule Collider 2D → Material** (ou no collider da parede).

---

## 2. Colliders e layers (paredes precisam existir para a física)

Sem colisão Enemy ↔ parede, Dynamic não resolve tunneling.

1. Confirme layer do inimigo: **Enemy**.
2. Paredes / obstáculos: layer **Wall** (ou Default com collider sólido).
3. Collider da parede: **não** trigger (`Is Trigger` desligado).
4. Em **Edit → Project Settings → Physics 2D → Layer Collision Matrix**:
   - **Enemy** × **Wall** = **marcado** (colidem).
5. Player × Enemy continua ignorado via `CombatLayerCollision` / `excludeLayers` — isso é intencional (não empurram um ao outro); **não** desligue Enemy × Wall.

---

## 3. NavMeshAgent (não desligar no prefab)

Deixe o **NavMesh Agent** como está. No knockback o código:

1. Pausa `EnemyMovement`
2. `isStopped = true`, `updatePosition = false`
3. Após o impulso: `Warp` na posição final e reativa o movimento

Não remova o Agent nem marque “Obstacle” só por causa do knockback.

---

## 4. Balanceamento do knockback (dados, não Inspector do rato)

Valores da Nixie melee: `Assets/Data/Stats/Player/NixieMeleeCombatStats.asset` (ou `MeleeCombatStats` referenciado pelo prefab).

| Campo | Papel |
|-------|--------|
| `knockbackDistance` | Usado no **servidor** (`ApplyKnockbackRpc`) → vira velocidade ≈ `distance / duration` |
| `knockbackDuration` | Janela com IA parada + impulso ativo |
| `knockbackForce` | Caminho **offline** (`KnockbackReceiver`) — velocidade/impulso direto |

Se o rato ainda “grudar” na parede sem atravessar, mas o combo parecer curto: aumente um pouco `knockbackDistance` ou diminua `Knockback Linear Damping` no `EnemyPhysicsBody`.

---

## 5. Validação em Play Mode

1. Entre numa fase com paredes sólidas (ex. Fase-1).
2. Com Nixie, empurre um rato **de frente contra a parede**.
3. Esperado:
   - O rato **não** some dentro da geometria
   - Não há ejeção brusca ao terminar o knockback
   - Durante o arremesso o rato não anda sozinho (IA pausada)
   - Combo melee continua acertando (inimigo permanece do lado de fora da parede)
4. Scene view: Rigidbody em knockback = **Dynamic** + **Continuous**; depois volta a **Kinematic**.

---

## 6. Prefabs — ordem sugerida de revisão

1. `Rato_Padrao_Base.prefab` (base da maioria)
2. Variantes que não herdam overrides de física: Veloz, Resistente, Eletrico, Acido
3. `Rato_Boss.prefab`
4. `Enemy.prefab` (template)

Em cada um: **Collision Detection = Continuous**, Gravity 0, Freeze Z, EnemyPhysicsBody presente.

---

## Relacionados

- Código: `EnemyPhysicsBody`, `NetworkEnemyController.ServerKnockbackRoutine`, `KnockbackReceiver`
- Prefab doc: [Enemy.md](../prefabs/Enemy.md), [Rato-variants.md](../prefabs/Rato-variants.md)
- Combat layers: `CombatLayerCollision.cs`
