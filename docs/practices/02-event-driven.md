# Event-driven architecture

## Objetivo

Manter **baixo acoplamento**: sistemas não devem conhecer implementações concretas uns dos outros; comunicam-se por eventos.

## Camadas de eventos no projeto

### 1. `GameEvents` (estático, C#)

Arquivo: `Assets/Scripts/Core/GameEvents.cs`

Eventos globais de UI e fluxo, por exemplo:

- `OnPlayerHealthChanged`, `OnPlayerAdrenalineChanged`
- `OnWaveStatusChanged`, `OnNightEnded`, `OnPlayerDefeated`
- `OnPauseChanged`, `OnCienciaCollected`
- Tutorial HUD: `OnTutorialMoveExecuted`, `OnTutorialShootExecuted`, `OnTutorialSealHoleExecuted`, `OnTutorialTipChanged`, `OnTutorialCompleted`

**Uso:** UI e managers assinam; componentes de gameplay invocam via `GameEvents.Invoke*`.

### 2. UnityEvents em componentes

Ex.: `HealthComponent.OnHealthChanged`, `OnDied` — acoplamento local ao prefab, ideal para VFX/áudio no mesmo objeto.

### 3. ScriptableObject events (quando aplicável)

Para desacoplamento entre assemblies ou configuração no editor; preferir quando o listener variar por cena/modo.

## Regras

1. **Não** fazer `FindObjectOfType<GameManager>()` em todo `Update` para notificar mudanças.
2. **Assinar em `OnEnable`**, **desassinar em `OnDisable`** (evita vazamento e callbacks em objetos destruídos).
3. **Um evento = um significado** (ex.: não reutilizar `OnNightEnded` para “wave acabou” e “noite acabou” sem documentar).
4. Payloads tipados (`Action<float,float>`) em vez de `object`.

## Anti-padrões

- Cadeias de `GetComponent` atravessando 4 níveis da hierarquia só para avisar a UI.
- Singletons que expõem dezenas de métodos “NotifyX” — extrair para eventos ou interfaces estreitas.

## Para agentes de IA

Antes de adicionar referência direta entre `PlayerShooting` e `MultiplayerHUD`, verifique se já existe evento em `GameEvents` ou se um UnityEvent no `HealthComponent` resolve.
