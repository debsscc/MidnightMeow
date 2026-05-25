using UnityEngine;

/// <summary>
/// Catálogo central de rotas de cena. Um asset por projeto ou por modo de jogo.
/// </summary>
[CreateAssetMenu(fileName = "SceneFlowCatalog", menuName = "MidnightMeow/Screen Flow/Scene Flow Catalog")]
public class SceneFlowCatalog : ScriptableObject
{
    public SceneFlowRouteDefinition[] routes = System.Array.Empty<SceneFlowRouteDefinition>();

    public bool TryGetRoute(string routeId, out SceneFlowRouteDefinition route)
    {
        route = null;
        if (string.IsNullOrEmpty(routeId) || routes == null)
            return false;

        for (int i = 0; i < routes.Length; i++)
        {
            if (routes[i] != null && routes[i].routeId == routeId)
            {
                route = routes[i];
                return true;
            }
        }

        return false;
    }
}
