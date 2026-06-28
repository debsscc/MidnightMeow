using UnityEngine;

/// <summary>
/// Resolve prefabs de Nixie/Cora quando o <see cref="PlayerSpawnManager"/> não tem mapeamento no Inspector.
/// </summary>
public static class CharacterPrefabLibrary
{
    private const string NixiePath = "Assets/Prefabs/Characters/Nixie.prefab";
    private const string CoraPath = "Assets/Prefabs/Characters/Cora.prefab";

    private static GameObject _nixie;
    private static GameObject _cora;

    public static GameObject GetNixiePrefab()
    {
        if (_nixie == null)
            _nixie = LoadPrefab(NixiePath, "Nixie");
        return _nixie;
    }

    public static GameObject GetCoraPrefab()
    {
        if (_cora == null)
            _cora = LoadPrefab(CoraPath, "Cora");
        return _cora;
    }

    public static GameObject GetPrefab(LobbyCharacterType type)
    {
        return type switch
        {
            LobbyCharacterType.CharacterB => GetCoraPrefab(),
            LobbyCharacterType.CharacterA => GetNixiePrefab(),
            _ => null
        };
    }

    private static GameObject LoadPrefab(string assetPath, string resourceName)
    {
#if UNITY_EDITOR
        GameObject editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (editorPrefab != null)
            return editorPrefab;
#endif

        return Resources.Load<GameObject>($"Characters/{resourceName}");
    }
}
