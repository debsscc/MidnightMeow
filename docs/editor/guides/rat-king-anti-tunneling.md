# Guia Editor — Rei Rato anti-tunneling (Fuga + Investida)

Última revisão: 2026-07-17

Após o código que usa `Physics2D.CircleCast` + `Rigidbody2D.MovePosition` no `RatKingController`, calibre o prefab e as paredes na cena. **Não** há script que atribua layers em runtime — tudo abaixo é manual no Inspector.

**Prefab:** `Assets/Prefabs/Enemies/Rato_Boss.prefab`  
**Script:** `RatKingController` (Fuga e Investida vivem neste componente; não há `RatKingFleeState` / `RatKingChargeState` separados).

---

## 1. LayerMask de obstáculos (`obstacleLayer`)

### 1.1 No prefab do Rei Rato

1. Abra `Rato_Boss.prefab`.
2. Selecione o **root** (onde está `RatKingController`).
3. No Inspector, header **Obstáculos (anti-tunneling)**:
   - **Obstacle Layer** → marque **Wall** (layer 6).
   - Se o boss **não** deve atravessar paredes de dash do player, marque também **DashableWall** (12).
4. Deixe **Player** / **Enemy** **desmarcados** — o Cast não deve tratar o jogador como parede.

### 1.2 Nas paredes da cena

1. Selecione cada parede / tilemap de colisão sólida.
2. Layer do GameObject = **Wall** (mesma layer da máscara).
3. Collider 2D (**Box** / **Composite** / etc.): **Is Trigger = desligado**.
4. Confirme em **Edit → Project Settings → Physics 2D → Layer Collision Matrix**:
   - **Enemy × Wall** = colidem (marcado).

Referência de walls: [Environment.md](../prefabs/Environment.md) (`Wall.prefab` já usa layer Wall).

### 1.3 Se a máscara ficar vazia

Com `Obstacle Layer` = Nothing, o código **não** clampa o dash (só loga warning se `debugLogs` estiver ligado) e o boss volta a poder mirar através de paredes. Preencha a máscara antes de testar.

---

## 2. Collision Detection (anti-tunneling em alta velocidade)

O dash usa `chargeDashSpeed` alto (~18). Discrete deixa o corpo “pular” para dentro da malha.

1. No root do `Rato_Boss`, abra **Rigidbody 2D**.
2. **Collision Detection**:
   - De `Discrete` → **`Continuous`**.
3. Em Physics 2D **não** existe `Continuous Dynamic` (isso é 3D). Use **Continuous**.
4. **Gravity Scale** = `0`, **Freeze Rotation Z** ligado.
5. **Body Type**: o runtime (`EnemyPhysicsBody`) usa Kinematic na locomoção e Dynamic durante fuga/dash físicos. No prefab, Continuous no RB já serializado é o que importa.

Salve o prefab (Apply / Save).

---

## 3. Sincronia Collider ↔ `obstacleCheckRadius`

O Cast usa um círculo cujo raio deve cobrir o corpo do boss. Se o raio for menor que o collider, o centro para “cedo” no Cast mas a cápsula ainda clipa na parede.

### 3.1 Medir o collider

1. No `Rato_Boss`, localize o **Capsule Collider 2D** (root ou child de hitbox).
2. Anote **Size** (X, Y) e **Offset**.
3. Raio teórico (world) ≈ `min(Size.x, Size.y) × 0.5 × escala do transform desse collider`.

Exemplo: Size `(1.1, 1.4)`, scale `1` → raio ≈ **0.55**.

### 3.2 Campos no `RatKingController`

| Campo | Sugestão | Função |
|-------|----------|--------|
| **Obstacle Check Radius** | ≈ metade do menor eixo do capsule | Raio do `CircleCast` |
| **Obstacle Skin** | `0.08` | Folga extra ao recuar o destino |
| **Min Flee Clearance** | `0.4` | Abaixo disso a fuga aborta e dispara o ataque a distância |

O código ainda faz `max(Inspector, metade do CapsuleCollider2D)`. Se o Capsule estiver **muito grande** (ex. Size ~12), o Cast fica conservador (dash curto / fuga ataca cedo). Nesse caso:

1. Reduza o Capsule para o tamanho real dos “pés”/corpo do boss, **ou**
2. Aceite o clamp curto até o collider ser corrigido.

### 3.3 Checklist visual

- Scene View: Capsule envolve o sprite sem sobras enormes.
- `Obstacle Check Radius` no Inspector ≈ metade da menor dimensão do Capsule.
- Telegraph da investida encurta quando há parede no caminho (comprimento = distância clampada).

---

## 4. Validação em Play Mode (Fase-3)

1. Posicione o boss perto de uma **Wall** sólida.
2. **Investida**: telegraph retangular deve **parar antes da parede**; o dash não atravessa.
3. **Fuga**: ao encostar de costas na parede, boss **não** empurra infinitamente — desliza tangencialmente ou inicia as 5 faixas cedo.
4. Com `Debug Logs` ligado no `RatKingController`, procure:
   - `Flee blocked by obstacle`
   - `Flee clearance exhausted`
5. Cliente remoto: telegraph encurtado deve bater com o dash real (mesmo comprimento no servidor).

---

## 5. Relacionados

- Setup geral do boss: [rat-king-boss-setup.md](rat-king-boss-setup.md)
- Knockback de ratos comuns: [enemy-knockback-anti-tunneling.md](enemy-knockback-anti-tunneling.md)
- Layers: [project-context.md](../project-context.md)
