#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configura cenas Fase-* com stack multiplayer, selamento, carruagem e wave settings.
/// </summary>
public static class PhaseSceneSetupEditor
{
    private const string ManagersPrefabPath = "Assets/Prefabs/Multiplayer/MultiplayerManagers.prefab";
    private const string GameManagerPrefabPath = "Assets/Prefabs/Multiplayer/MultiplayerGameManager.prefab";
    private const string SpawnManagerPrefabPath = "Assets/Prefabs/Multiplayer/PlayerSpawnManager.prefab";
    private const string CameraRigPrefabPath = "Assets/Prefabs/Multiplayer/MultiplayerCameraRig.prefab";
    private const string HoleSpritePath = "Assets/Art/Sprites/Enviroment/Level2/Decoração/buraco03spawner.png";
    private const string CarriageConfigPath = "Assets/Data/Gameplay/CarriageConfig.asset";
    private const string SealConfigPath = "Assets/Resources/RatHoleSealConfig.asset";

    [MenuItem("MidnightMeow/Phases/Setup Active Phase Scene")]
    public static void SetupActivePhaseScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.StartsWith("Fase-"))
        {
            EditorUtility.DisplayDialog("Phases", "Abra uma cena Fase-* antes de executar.", "OK");
            return;
        }

        SetupScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[PhaseSetup] Concluído em '{scene.name}'.");
    }

    [MenuItem("MidnightMeow/Phases/Setup All Phase Scenes")]
    public static void SetupAllPhaseScenes()
    {
        string[] scenes =
        {
            "Assets/Scenes/Fases/Fase-1.unity",
            "Assets/Scenes/Fases/Fase-2.unity",
            "Assets/Scenes/Fases/Fase-3.unity"
        };

        string original = SceneManager.GetActiveScene().path;
        foreach (string path in scenes)
        {
            if (!System.IO.File.Exists(path))
                continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SetupScene(scene);
            EditorSceneManager.SaveOpenScenes();
        }

        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    [MenuItem("MidnightMeow/Phases/Register Network Prefabs (Boss + Carriage)")]
    public static void RegisterNetworkPrefabs()
    {
        RegisterPrefabInDefaultList("Assets/Prefabs/Enemies/Rato_Boss.prefab");
        RegisterPrefabInDefaultList("Assets/Prefabs/Gameplay/Carriage.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("[PhaseSetup] Prefabs de rede atualizados.");
    }

    [MenuItem("MidnightMeow/Phases/Add Enemy Health Bars To Prefabs")]
    public static void AddEnemyHealthBarsToEnemyPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });
        int updated = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            if (root.GetComponent<HealthComponent>() != null && root.GetComponent<EnemyHealthBarDisplay>() == null)
            {
                root.AddComponent<EnemyHealthBarDisplay>();
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                updated++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PhaseSetup] EnemyHealthBarDisplay adicionado em {updated} prefab(s) de inimigo.");
    }

    private static void SetupScene(Scene scene)
    {
        Undo.SetCurrentGroupName($"Phase Setup {scene.name}");
        int undo = Undo.GetCurrentGroup();

        PhaseWaveSettingsCatalog catalog = AssetDatabase.LoadAssetAtPath<PhaseWaveSettingsCatalog>(
            "Assets/Resources/PhaseWaveSettingsCatalog.asset");

        PhaseWaveSettingsCatalog.PhaseEntry entry = null;
        if (catalog != null)
            catalog.TryGetEntry(scene.name, out entry);

        EnsureMultiplayerStack();
        Transform enemySpawnsRoot = EnsureEnemySpawnRoot();
        NetworkWaveManager waveManager = EnsureGameLoop(entry, enemySpawnsRoot);
        EnsurePlayerSpawns();
        EnsurePlayerSpawnCharacterPrefabs();
        RemoveScenePlacedPlayerCharacters();
        EnsureCameraBoundsVolume();

        if (entry != null && entry.enableRatHoleSealing)
            InstallRatHoleSpawnPoints(waveManager);

        if (entry != null && entry.enableCarriage)
            InstallCarriage();

        if (scene.name == "Fase-3")
            EnsureBossPrefabMarker();

        WireNightManager(entry);
        WireBootstrapper(waveManager);

        Undo.CollapseUndoOperations(undo);
    }

    private static void EnsureMultiplayerStack()
    {
        if (Object.FindFirstObjectByType<MultiplayerGameManager>(FindObjectsInactive.Include) == null)
            InstantiatePrefabAtRoot(GameManagerPrefabPath, "MultiplayerGameManager");

        if (Object.FindFirstObjectByType<MultiplayerBootstrapper>(FindObjectsInactive.Include) == null)
            InstantiatePrefabAtRoot(ManagersPrefabPath, "MultiplayerManagers");

        if (Object.FindFirstObjectByType<PlayerSpawnManager>(FindObjectsInactive.Include) == null)
            InstantiatePrefabAtRoot(SpawnManagerPrefabPath, "PlayerSpawnManager");

        if (Object.FindFirstObjectByType<MultiplayerCameraController>(FindObjectsInactive.Include) == null)
            InstantiatePrefabAtRoot(CameraRigPrefabPath, "MultiplayerCameraRig");
    }

    private static GameObject InstantiatePrefabAtRoot(string path, string label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[PhaseSetup] Prefab ausente: {path}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, $"Create {label}");
        instance.name = label;
        return instance;
    }

    private static Transform EnsureEnemySpawnRoot()
    {
        GameObject root = GameObject.Find("---- Spawn Points Inimigos ----");
        if (root == null)
            root = GameObject.Find("---- Spawn Points Inimigos ---- (1)");

        if (root != null)
            return root.transform;

        root = new GameObject("---- Spawn Points Inimigos ----");
        Undo.RegisterCreatedObjectUndo(root, "Create Enemy Spawn Root");
        return root.transform;
    }

    private static NetworkWaveManager EnsureGameLoop(
        PhaseWaveSettingsCatalog.PhaseEntry entry,
        Transform enemySpawnsRoot)
    {
        GameObject systems = GameObject.Find("---- Sistemas ----");
        Transform parent = systems != null ? systems.transform : null;

        GameObject loop = GameObject.Find("_GameLoop");
        if (loop == null)
        {
            loop = new GameObject("_GameLoop");
            Undo.RegisterCreatedObjectUndo(loop, "Create _GameLoop");
            if (parent != null)
                loop.transform.SetParent(parent, false);
        }

        if (loop.GetComponent<NetworkObject>() == null)
            Undo.AddComponent<NetworkObject>(loop);

        NetworkWaveManager waveManager = loop.GetComponent<NetworkWaveManager>();
        if (waveManager == null)
            waveManager = Undo.AddComponent<NetworkWaveManager>(loop);

        List<Transform> spawnPoints = CollectEnemySpawnPoints(enemySpawnsRoot, loop.transform);
        waveManager.ConfigureSpawnPoints(spawnPoints.ToArray());

        if (entry?.waveSettings != null)
            waveManager.ConfigureWaveSettings(entry.waveSettings);

        NetworkRatHoleSealManager sealManager = loop.GetComponent<NetworkRatHoleSealManager>();
        if (sealManager == null)
            sealManager = Undo.AddComponent<NetworkRatHoleSealManager>(loop);

        RatHoleSealConfig sealConfig = AssetDatabase.LoadAssetAtPath<RatHoleSealConfig>(SealConfigPath);
        if (sealConfig != null)
            sealManager.ConfigureSealConfig(sealConfig);

        RatHoleSealZoneVisual.EnsureAttached(sealManager);

        return waveManager;
    }

    private static List<Transform> CollectEnemySpawnPoints(Transform enemySpawnsRoot, Transform loopTransform)
    {
        var points = new List<Transform>();

        WaveGenerator legacy = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        if (legacy != null)
        {
            foreach (Transform child in legacy.transform)
            {
                if (!IsPlayerSpawnPoint(child))
                    points.Add(child);
            }
        }

        if (points.Count == 0 && enemySpawnsRoot != null)
        {
            for (int i = 0; i < enemySpawnsRoot.childCount; i++)
                points.Add(enemySpawnsRoot.GetChild(i));
        }

        if (points.Count == 0)
        {
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                CollectByName(go.transform, "SP1", points);

            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                CollectByName(go.transform, "SpawnPoint", points);
        }

        if (points.Count == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject sp = new GameObject($"SP_Auto_{i + 1}");
                Undo.RegisterCreatedObjectUndo(sp, "Create Spawn");
                sp.transform.SetParent(enemySpawnsRoot != null ? enemySpawnsRoot : loopTransform, false);
                sp.transform.position = new Vector3(-20f + i * 12f, 0f, 0f);
                points.Add(sp.transform);
            }
        }

        return points;
    }

    private static void CollectByName(Transform root, string namePart, List<Transform> output)
    {
        if (root.name.Contains(namePart) && !output.Contains(root) && !IsPlayerSpawnPoint(root))
            output.Add(root);

        for (int i = 0; i < root.childCount; i++)
            CollectByName(root.GetChild(i), namePart, output);
    }

    private static void EnsurePlayerSpawns()
    {
        PlayerSpawnManager spawnManager = Object.FindFirstObjectByType<PlayerSpawnManager>(FindObjectsInactive.Include);
        if (spawnManager == null)
            return;

        SerializedObject so = new SerializedObject(spawnManager);
        SerializedProperty spawnPoints = so.FindProperty("spawnPoints");
        if (spawnPoints != null && spawnPoints.arraySize >= 2)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            return;
        }

        GameObject root = GameObject.Find("---- Spawn Points Jogadores ----");
        if (root == null)
        {
            root = new GameObject("---- Spawn Points Jogadores ----");
            Undo.RegisterCreatedObjectUndo(root, "Create Player Spawn Root");
        }

        Transform sp1 = root.transform.Find("SP1");
        Transform sp2 = root.transform.Find("SP2");
        if (sp1 == null)
        {
            GameObject a = new GameObject("SP1");
            Undo.RegisterCreatedObjectUndo(a, "SP1");
            a.transform.SetParent(root.transform, false);
            a.transform.position = new Vector3(-36f, 4.5f, 0f);
            sp1 = a.transform;
        }

        if (sp2 == null)
        {
            GameObject b = new GameObject("SP2");
            Undo.RegisterCreatedObjectUndo(b, "SP2");
            b.transform.SetParent(root.transform, false);
            b.transform.position = new Vector3(-39f, 2.5f, 0f);
            sp2 = b.transform;
        }

        if (spawnPoints != null)
        {
            spawnPoints.arraySize = 2;
            spawnPoints.GetArrayElementAtIndex(0).objectReferenceValue = sp1;
            spawnPoints.GetArrayElementAtIndex(1).objectReferenceValue = sp2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void EnsurePlayerSpawnCharacterPrefabs()
    {
        PlayerSpawnManager spawnManager = Object.FindFirstObjectByType<PlayerSpawnManager>(FindObjectsInactive.Include);
        if (spawnManager == null)
            return;

        GameObject nixie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Nixie.prefab");
        GameObject cora = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Cora.prefab");
        if (nixie == null || cora == null)
            return;

        SerializedObject so = new SerializedObject(spawnManager);
        SerializedProperty entries = so.FindProperty("characterPrefabs");
        if (entries == null)
            return;

        entries.arraySize = 2;
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("characterType").enumValueIndex = (int)LobbyCharacterType.CharacterA;
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = nixie;
        entries.GetArrayElementAtIndex(1).FindPropertyRelative("characterType").enumValueIndex = (int)LobbyCharacterType.CharacterB;
        entries.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = cora;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureCameraBoundsVolume()
    {
        CameraBoundsVolume bounds = Object.FindFirstObjectByType<CameraBoundsVolume>(FindObjectsInactive.Include);
        if (bounds == null)
            return;

        PolygonCollider2D poly = bounds.GetComponent<PolygonCollider2D>();
        if (poly != null)
            poly.enabled = true;
    }

    private static void InstallRatHoleSpawnPoints(NetworkWaveManager waveManager)
    {
        if (waveManager == null)
            return;

        Sprite holeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HoleSpritePath);
        SerializedObject so = new SerializedObject(waveManager);
        SerializedProperty spawnPoints = so.FindProperty("spawnPoints");
        if (spawnPoints == null)
            return;

        for (int i = 0; i < spawnPoints.arraySize; i++)
        {
            Transform sp = spawnPoints.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (sp == null)
                continue;

            if (IsPlayerSpawnPoint(sp))
                continue;

            CircleCollider2D trigger = sp.GetComponent<CircleCollider2D>();
            if (trigger == null)
                trigger = Undo.AddComponent<CircleCollider2D>(sp.gameObject);
            if (trigger != null)
            {
                trigger.isTrigger = true;
                trigger.radius = 2.4f;
            }

            RatHoleSpawnPoint hole = sp.GetComponent<RatHoleSpawnPoint>();
            if (hole == null)
                hole = Undo.AddComponent<RatHoleSpawnPoint>(sp.gameObject);

            SerializedObject holeSo = new SerializedObject(hole);
            holeSo.FindProperty("holeId").intValue = i + 1;
            if (holeSprite != null)
                holeSo.FindProperty("holeSprite").objectReferenceValue = GetOrCreateHoleSpriteRenderer(sp, holeSprite);
            holeSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static bool IsPlayerSpawnPoint(Transform sp)
    {
        Transform current = sp;
        while (current != null)
        {
            if (current.name.Contains("Spawn Points Jogadores", System.StringComparison.Ordinal))
                return true;

            current = current.parent;
        }

        return false;
    }

    private static SpriteRenderer GetOrCreateHoleSpriteRenderer(Transform sp, Sprite sprite)
    {
        Transform visual = sp.Find("HoleVisual");
        if (visual == null)
        {
            GameObject go = new GameObject("HoleVisual");
            Undo.RegisterCreatedObjectUndo(go, "Hole Visual");
            go.transform.SetParent(sp, false);
            visual = go.transform;
        }

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<SpriteRenderer>(visual.gameObject);

        renderer.sprite = sprite;
        renderer.sortingOrder = 1;
        return renderer;
    }

    private static void InstallCarriage()
    {
        CarriageController[] sceneCarriages = Object.FindObjectsByType<CarriageController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < sceneCarriages.Length; i++)
        {
            if (sceneCarriages[i] != null)
                Undo.DestroyObjectImmediate(sceneCarriages[i].gameObject);
        }

        if (!TryGetMapBounds(out Bounds bounds))
        {
            Debug.LogWarning("[PhaseSetup] CameraBoundsVolume ausente — CarriagePath não configurado.");
            return;
        }

        CarriageConfig config = AssetDatabase.LoadAssetAtPath<CarriageConfig>(CarriageConfigPath);
        float pathY = config != null && config.useCustomPathY ? config.pathY : bounds.center.y;
        Vector3 left = new Vector3(config != null ? config.pathStartX : bounds.min.x, pathY, 0f);
        Vector3 right = new Vector3(config != null ? config.pathEndX : bounds.max.x, pathY, 0f);

        CarriagePath path = Object.FindFirstObjectByType<CarriagePath>(FindObjectsInactive.Include);
        GameObject pathRoot;
        if (path != null)
        {
            pathRoot = path.gameObject;
        }
        else
        {
            pathRoot = new GameObject("CarriagePath");
            Undo.RegisterCreatedObjectUndo(pathRoot, "Carriage Path");
            path = pathRoot.AddComponent<CarriagePath>();
        }

        Transform wpStart = pathRoot.transform.Find("Waypoint_Start");
        if (wpStart == null)
        {
            GameObject startGo = new GameObject("Waypoint_Start");
            startGo.transform.SetParent(pathRoot.transform, false);
            wpStart = startGo.transform;
        }

        Transform wpEnd = pathRoot.transform.Find("Waypoint_End");
        if (wpEnd == null)
        {
            GameObject endGo = new GameObject("Waypoint_End");
            endGo.transform.SetParent(pathRoot.transform, false);
            wpEnd = endGo.transform;
        }

        wpStart.position = left;
        wpEnd.position = right;
        path.ConfigureWaypoints(new[] { wpStart, wpEnd });

        Debug.Log(
            "[PhaseSetup] CarriagePath configurado na cena. A carruagem NÃO é colocada aqui — " +
            "spawn exclusivo em runtime pelo servidor (CarriageSpawner).");
    }

    private static GameObject CreateCarriageInScene()
    {
        GameObject go = new GameObject("Carriage");
        Undo.RegisterCreatedObjectUndo(go, "Carriage");
        go.tag = "Structure";
        go.layer = LayerMask.NameToLayer("Structure");

        Undo.AddComponent<NetworkObject>(go);
        Undo.AddComponent<NetworkTransform>(go);
        Undo.AddComponent<CarriageController>(go);
        Undo.AddComponent<NetworkCarriageHealth>(go);
        Undo.AddComponent<CarriageDamageFilter>(go);
        Undo.AddComponent<NetworkCarriageRepairManager>(go);
        Undo.AddComponent<CarriageRepairWorldUI>(go);
        HealthComponent health = Undo.AddComponent<HealthComponent>(go);
        BoxCollider2D box = Undo.AddComponent<BoxCollider2D>(go);
        box.size = new Vector2(2.4f, 1.6f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.75f, 0.55f, 0.25f, 1f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(2.4f, 1.6f);

        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("_maxHealth").floatValue = 120f;
        healthSo.FindProperty("_allowDestroyOnDeath").boolValue = false;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        CarriageConfig config = AssetDatabase.LoadAssetAtPath<CarriageConfig>(CarriageConfigPath);
        CarriageController carriage = go.GetComponent<CarriageController>();
        SerializedObject carriageSo = new SerializedObject(carriage);
        if (config != null)
            carriageSo.FindProperty("config").objectReferenceValue = config;
        carriageSo.ApplyModifiedPropertiesWithoutUndo();

        string prefabDir = "Assets/Prefabs/Gameplay";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Gameplay");

        PrefabUtility.SaveAsPrefabAsset(go, $"{prefabDir}/Carriage.prefab");
        return go;
    }

    private static void EnsureBossPrefabMarker()
    {
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Rato_Boss.prefab");
        if (bossPrefab == null)
            return;

        if (bossPrefab.GetComponent<BossEnemyMarker>() == null)
        {
            BossEnemyMarker marker = bossPrefab.AddComponent<BossEnemyMarker>();
            EditorUtility.SetDirty(bossPrefab);
            PrefabUtility.SavePrefabAsset(bossPrefab);
        }
    }

    private static void WireNightManager(PhaseWaveSettingsCatalog.PhaseEntry entry)
    {
        if (entry?.waveSettings == null)
            return;

        NightManager night = Object.FindFirstObjectByType<NightManager>(FindObjectsInactive.Include);
        if (night == null)
            return;

        SerializedObject so = new SerializedObject(night);
        so.FindProperty("nightConfiguration").objectReferenceValue = entry.waveSettings;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveScenePlacedPlayerCharacters()
    {
        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int removed = 0;

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth player = players[i];
            if (player == null)
                continue;

            Undo.DestroyObjectImmediate(player.gameObject);
            removed++;
        }

        if (removed > 0)
            Debug.Log($"[PhaseSetup] Removidos {removed} personagem(ns) colocado(s) na cena (use spawn do PlayerSpawnManager).");
    }

    private static void WireBootstrapper(NetworkWaveManager waveManager)
    {
        MultiplayerBootstrapper bootstrapper = Object.FindFirstObjectByType<MultiplayerBootstrapper>(FindObjectsInactive.Include);
        MultiplayerGameManager gameManager = Object.FindFirstObjectByType<MultiplayerGameManager>(FindObjectsInactive.Include);
        if (bootstrapper == null)
            return;

        SerializedObject so = new SerializedObject(bootstrapper);
        so.FindProperty("skipGameplayChecksOutsideGameplayScene").boolValue = true;
        if (gameManager != null)
            so.FindProperty("gameManager").objectReferenceValue = gameManager;
        if (waveManager != null)
            so.FindProperty("waveManager").objectReferenceValue = waveManager;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool TryGetMapBounds(out Bounds bounds)
    {
        bounds = default;
        CameraBoundsVolume volume = Object.FindFirstObjectByType<CameraBoundsVolume>(FindObjectsInactive.Include);
        if (volume == null || volume.BoundsCollider == null)
            return false;

        bounds = volume.BoundsCollider.bounds;
        return bounds.size.sqrMagnitude > 0.01f;
    }

    private static void RegisterPrefabInDefaultList(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return;

        NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
        if (list == null)
            return;

        foreach (NetworkPrefab entry in list.PrefabList)
        {
            if (entry.Prefab == prefab)
                return;
        }

        list.Add(new NetworkPrefab { Prefab = prefab });
        EditorUtility.SetDirty(list);
    }
}
#endif
