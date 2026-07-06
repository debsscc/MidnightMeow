using UnityEngine;

/// <summary>
/// Recebe Animation Events de clips de gameplay usados só como preview na UI (sem lógica de combate).
/// </summary>
[DisallowMultipleComponent]
public class UiAnimationEventStub : MonoBehaviour
{
    public void PerformFire() { }
}
