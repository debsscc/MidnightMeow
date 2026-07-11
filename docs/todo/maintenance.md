# Tarefas de manutenção pendentes

_Última revisão: 2026-07-10_

---

## [TASK CONCLUÍDA] Remoção de Gizmos de Depuração (Dash)

- **O que foi feito:** `AbilityDebugVisualHost.showInPlayMode` default `false`; gizmos restritos a `#if UNITY_EDITOR`. `PlayerGameplayModuleInstaller.installAbilityDebugVisual` default `false` (só instala em Editor ou `Debug.isDebugBuild` se habilitado). `MeleeAttackDebugVisual` alinhado (sem overlay em Play por padrão). Scripts: `AbilityDebugVisualHost.cs`, `MeleeAttackDebugVisual.cs`, `PlayerGameplayModuleInstaller.cs`.

- **Como testar:** Build Release → Fase-1 com Nix → Dash não exibe retângulo ciano. Editor com `installAbilityDebugVisual` ligado no prefab + `showInPlayMode` no host → retângulo opcional.

- **Resultado Esperado:** Sem retângulo de debug do Dash em builds de produção.

---

## [TASK CONCLUÍDA] Shaders de Ataque/Habilidades do Nixie (build)

- **O que foi feito:** `AbilityZoneFill` e `MeleeHitWave` em Always Included Shaders. Materiais `Resources/AbilityZoneFillMaterial.mat` e `MeleeHitWaveMaterial.mat`. `CombatVisualMaterials` centraliza criação de instâncias. `MeleeAttackVisual`, `AbilityDebugVisualHost` e `MeleeAttackDebugVisual` usam Resources em vez de `Shader.Find` puro. Scripts + `ProjectSettings/GraphicsSettings.asset`.

- **Como testar:** Build player → Nix ataque melee mostra onda laranja (não retângulo branco). Habilidades Nix (push/charge) com zona colorida se debug host ativo.

- **Resultado Esperado:** Shaders custom presentes na build.

---

## [TASK CONCLUÍDA] Sliders de Áudio Inoperantes na Build

- **O que foi feito (2026-07-10):** Mixer único `MidnightMeowAudioMixer` em `Assets/Resources/MidnightMeowAudioMixer.mixer` (GUID `0369658093ea21d40a1c846ecafdd511` — mesmas refs de prefabs/cenas). Removidas as cópias duplicadas `Assets/Audio/NewAudioMixer` e `Assets/Resources/NewAudioMixer`. `GameAudioSettings` carrega só `Resources.Load("MidnightMeowAudioMixer")`.

- **Como testar:** Build → Menu2 → Opções → mover sliders Master/Music/SFX → volume audível muda; reabrir menu mantém valores.

- **Resultado Esperado:** Um único mixer no projeto; slider SFX afeta interação, heartbeat, inimigos e boss.

---

## [TASK CONCLUÍDA] Shader da Tela Inicial muito escuro na Build

- **O que foi feito:** `vertexColorAlwaysGammaSpace = true` nos canvases de Menu2 via `MainMenuController.ApplyMenuCanvasGammaSpace()`; cena `Menu2.unity` atualizada (`m_VertexColorAlwaysGammaSpace: 1` nos dois Canvas).

- **Como testar:** Build standalone vs Editor → Menu2 com luminosidade similar (fundo e botões legíveis, não ~2× mais escuro).

- **Resultado Esperado:** UI do menu em color space Linear com vertex colors em gamma space.

---

## Verificação manual

- [x] Dash sem debug em Release (código — validar build)
- [x] Melee Nix com shader na build (código — validar build)
- [x] Sliders áudio Menu2 build (código — validar build)
- [x] Menu2 não escuro na build (código — validar build)
