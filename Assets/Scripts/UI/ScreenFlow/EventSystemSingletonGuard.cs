using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Destrói EventSystems duplicados no Awake (antes do OnEnable que gera o aviso do UGUI).
/// </summary>
[DefaultExecutionOrder(-20000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EventSystem))]
public class EventSystemSingletonGuard : MonoBehaviour
{
    private void Awake()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems.Length <= 1)
            return;

        EventSystem keeper = systems[0];
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && systems[i].gameObject.scene.name == "DontDestroyOnLoad")
            {
                keeper = systems[i];
                break;
            }
        }

        if (GetComponent<EventSystem>() != keeper)
            Destroy(gameObject);
    }
}
