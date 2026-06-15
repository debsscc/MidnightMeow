using Unity.Netcode;
using UnityEngine;

/// <summary>
/// UI world-space de downed/revive. Desativada no playtest (sem texto/barras na morte).
/// Reative via <see cref="PlayerGameplayModuleInstaller.installDownedUI"/> quando o reviver voltar.
/// </summary>
[RequireComponent(typeof(NetworkPlayerHealth))]
public class DownedPlayerWorldUI : MonoBehaviour
{
    private void Awake()
    {
        enabled = false;
    }
}
