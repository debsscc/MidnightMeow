using UnityEngine;

[CreateAssetMenu(fileName = "HubSessionPrefabCatalog", menuName = "Scriptable Objects/Multiplayer/Hub Session Prefab Catalog")]
public class HubSessionPrefabCatalog : ScriptableObject
{
    public GameObject preparationSessionManagerPrefab;
    public GameObject charactersSessionManagerPrefab;

    private static HubSessionPrefabCatalog _cached;

    public static HubSessionPrefabCatalog LoadCached()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<HubSessionPrefabCatalog>("HubSessionPrefabCatalog");
        return _cached;
    }
}
