using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Corrige Light2D da Fase-3 que estavam sob Canvas Overlay (letterbox/UI).
// Em resoluções ≠ 1920×1080 o CanvasScaler deslocava/escalava as luzes → cena escura.
// ---------------------------------------------------------------- */

/// <summary>
/// Desacopla <see cref="Light2D"/> de Canvas Overlay/Scaler e encaixa Sprite Lights
/// no frustum ortográfico da câmera de gameplay. Idempotente.
/// </summary>
public static class PhaseLightingHierarchyFix
{
    public const string LightsRootName = "Lights";
    public const string LegacyLightingCanvasHint = "Texture_Light";

    /// <summary>Chamar ao entrar em cena de gameplay (ex.: Fase-3).</summary>
    public static void EnsureForActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        // Bug reportado só na Fase-3, mas o padrão (Light2D sob Canvas) é sempre inválido.
        if (!GameplaySceneBootstrap.IsGameplayScene(scene.name))
            return;

        RepairCanvasBoundLights(scene);
    }

    private static void RepairCanvasBoundLights(Scene scene)
    {
        Light2D[] lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform lightsRoot = null;
        int relocated = 0;

        for (int i = 0; i < lights.Length; i++)
        {
            Light2D light = lights[i];
            if (light == null)
                continue;

            if (light.gameObject.scene != scene)
                continue;

            Canvas canvas = light.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // Já em world-space: ainda corrige scale.z=0 e fitter de sprite.
                SanitizeTransform(light.transform);
                EnsureSpriteFitter(light);
                continue;
            }

            // Global/Sprite sob Canvas Overlay = anti-padrão URP 2D (Fase-3 letterbox).
            if (lightsRoot == null)
                lightsRoot = EnsureLightsRoot(scene);

            RelocateToWorld(light, lightsRoot);
            SanitizeTransform(light.transform);
            EnsureSpriteFitter(light);
            relocated++;
        }

        if (relocated > 0)
            Debug.Log($"[PhaseLightingHierarchyFix] {relocated} Light2D movido(s) de Canvas → '{LightsRootName}' em '{scene.name}'.");
    }

    private static Transform EnsureLightsRoot(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform existing = roots[i].transform.Find(LightsRootName);
            if (existing != null)
                return existing;

            if (roots[i].name == LightsRootName)
                return roots[i].transform;

            // Fase-1 usa "Lights"; Fase-3 tem "Enviroment" — preferir irmão world-space.
            if (roots[i].name == "Enviroment" || roots[i].name == "Environment")
            {
                Transform nested = roots[i].transform.Find(LightsRootName);
                if (nested != null)
                    return nested;

                var nestedGo = new GameObject(LightsRootName);
                nestedGo.transform.SetParent(roots[i].transform, false);
                nestedGo.transform.localPosition = Vector3.zero;
                nestedGo.transform.localRotation = Quaternion.identity;
                nestedGo.transform.localScale = Vector3.one;
                return nestedGo.transform;
            }
        }

        var go = new GameObject(LightsRootName);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = Vector3.zero;
        return go.transform;
    }

    private static void RelocateToWorld(Light2D light, Transform lightsRoot)
    {
        Transform t = light.transform;

        // Preserva pose world no instante do detach (antes do CanvasScaler “mentir” de novo).
        Vector3 worldPos = t.position;
        Quaternion worldRot = t.rotation;

        t.SetParent(lightsRoot, true);
        t.position = worldPos;
        t.rotation = worldRot;

        // Fase-3: objeto nomeado "Global Light 2D" está serializado como Point (tipo 3),
        // não Global (tipo 4). Point com raio grande depende da posição — sob CanvasScaler
        // em Free Aspect ele sai da área jogável e a fase fica preta.
        bool ambientLike = light.lightType == Light2D.LightType.Global
                           || light.gameObject.name.IndexOf("Global Light", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (ambientLike)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
    }

    private static void SanitizeTransform(Transform t)
    {
        Vector3 scale = t.localScale;
        // Texture_Light na Fase-3 estava com scale.z = 0 (matriz degenerada / luz quebrada).
        if (Mathf.Abs(scale.z) < 0.0001f)
            scale.z = 1f;
        if (Mathf.Abs(scale.x) < 0.0001f)
            scale.x = 1f;
        if (Mathf.Abs(scale.y) < 0.0001f)
            scale.y = 1f;
        t.localScale = scale;
    }

    private static void EnsureSpriteFitter(Light2D light)
    {
        if (light.lightType != Light2D.LightType.Sprite)
            return;

        if (light.GetComponent<OrthographicSpriteLightFitter>() != null)
            return;

        OrthographicSpriteLightFitter fitter = light.gameObject.AddComponent<OrthographicSpriteLightFitter>();
        fitter.ConfigureFromLight(light);
    }
}
