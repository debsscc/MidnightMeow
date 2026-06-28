using UnityEngine;

[CreateAssetMenu(fileName = "GameplayPrefabCatalog", menuName = "Scriptable Objects/Multiplayer/Gameplay Prefab Catalog")]
public class GameplayPrefabCatalog : ScriptableObject
{
    public GameObject multiplayerCameraRigPrefab;
    [Tooltip("Visual de voo dos ataques inimigos ProjectileToZone (clientes MP).")]
    public GameObject enemyTelegraphTravelPrefab;
    [Tooltip("Carruagem da Fase 2 (spawn dinâmico no servidor).")]
    public GameObject carriagePrefab;

    private static GameplayPrefabCatalog _cached;

    public static GameplayPrefabCatalog LoadCached()
    {
        if (_cached != null)
            return _cached;

        _cached = Resources.Load<GameplayPrefabCatalog>("GameplayPrefabCatalog");
        return _cached;
    }
}
