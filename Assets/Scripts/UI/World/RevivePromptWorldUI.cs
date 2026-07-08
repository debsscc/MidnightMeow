// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Obsoleto — progresso e textos migrados para DownedPlayerWorldUI (label no caído).
// ---------------------------------------------------------------- 

using System;
using UnityEngine;

[Obsolete("Use DownedPlayerWorldUI no jogador caído — label único com máquina de estados.")]
[RequireComponent(typeof(NetworkPlayerRevive), typeof(NetworkPlayerHealth))]
public class RevivePromptWorldUI : MonoBehaviour
{
    private void Awake() => enabled = false;
}
