# Guia Editor — Conserto da Carruagem (Input E + zonas)

Última revisão: 2026-07-15

Corrige a falha silenciosa do **E** e alinha o conserto ao fluxo de selamento/reviver.

## Diagnóstico (o que estava quebrado)

1. **`NetworkCarriageRepairManager` vivia no mesmo `.cs` que `NetworkCarriageHealth`**  
   Dois `NetworkBehaviour` no mesmo arquivo compartilham o mesmo GUID de script. O source-gen de RPCs do NGO frequentemente deixa o `RequestStartRepairRpc` sem binding efetivo → o cliente “chama” o RPC e o servidor **não executa** (falha silenciosa).  
   **Correção:** manager em arquivo próprio + GUID novo no prefab.

2. **UI vs interação desalinhadas**  
   O label podia mostrar “Aperte E” com base em `CarriageState.Broken` enquanto a interação exigia `IsBroken`. Agora ambos usam **`NetworkCarriageHealth.IsBroken`**.

3. **Fallback de tecla E**  
   Como no histórico do selamento, há fallback `Keyboard.eKey` além da action Interact.

A proximidade **não** usa Trigger collider (igual selamento/reviver): usa distância ao root da carruagem (`repairPromptRadius`).

## 1. Colliders da Carruagem

1. Prefab `Assets/Prefabs/Gameplay/Carriage.prefab`, root **Carriage**:
   - Tag = `Structure`
   - Layer = `Structure`
2. `BoxCollider2D` no **root** (não só no VisualRoot):
   - `Is Trigger` = **desligado** (colisão física / hitbox de dano)
   - Interação de conserto **não depende** de trigger; não é necessário ligar Is Trigger só para o E.
3. Confirme que o collider cobre o corpo visual (`CarriageConfig.colliderSize` / `colliderOffset`).

## 2. NetworkCarriageRepairManager no Prefab

1. No root da Carriage deve existir o componente **`NetworkCarriageRepairManager`** (script em `Assets/Scripts/Multiplayer/Carriage/NetworkCarriageRepairManager.cs`).
2. Campo **Config** → `Assets/Data/Gameplay/CarriageConfig.asset`.
3. O mesmo GameObject precisa de `NetworkObject` (já presente). Após reimport, se o component aparecer como Missing Script, remova e **Add Component → Network Carriage Repair Manager** de novo.

As zonas de conserto **não** são prefabs NetworkObject separados: o host cria círculos locais com `SealZoneRingVisual` (mesmo sistema do selamento). Não é obrigatório criar um prefab de círculo com `NetworkObject`.

## 3. UI / TextMeshPro

1. Em `CarriageConfig`:
   - `Repair Prompt Prefab` → prefab world-space TMP (ex.: `DownedRevivePromptUI`)
   - `Press E Text` → “Aperte E para consertar”
   - `Stay In Area Text` → “Fique na área para consertar”
   - `Repair Progress Text Format` → `{0}%`
2. `CarriageRepairWorldUI` no prefab da Carriage:
   - `Repair UI Prefab` pode ficar vazio (puxa do config)
   - O TMP é resolvido em runtime via `DownedReviveUILabelView` / `TextMeshProUGUI` no prefab instanciado — **não** precisa arrastar um Label na carruagem manualmente.
3. Progresso: clientes ouvem `NetworkVariable` `_repairProgress` / `_repairActive` (não editar no Inspector).

## 4. Player — interação

Nos prefabs `Cora` / `Nixie`, `PlayerGameplayModuleInstaller` com `Install Carriage Repair Interaction` = true adiciona `PlayerCarriageRepairInteraction` em runtime. Não é necessário colocar o component à mão.

## 5. Checklist de teste (Play Mode Host)

1. Quebre a carruagem (ratos / HP = 0) → label “Consertem…” / “Aperte E…”.
2. Aproxime e pressione **E** → SFX de Interact + círculos aparecem + texto “Fique na área…”.
3. Fique em pelo menos um círculo → % sobe.
4. 100% → zonas somem, SFX `Reviver.wav`, HP ~50% (`repairRestoreHealthFraction`), estado Idle/Moving.

Se o E ainda falhar: no Console do **servidor** procure logs `[NetworkCarriageRepairManager] RequestStartRepairRpc rejeitado:…`.

## Ver também

- [gameplay/carriage.md](../../gameplay/carriage.md)
- [guides/carriage-phase2-aggro-setup.md](carriage-phase2-aggro-setup.md)
- [multiplayer/revive-zone.md](../../multiplayer/revive-zone.md)
