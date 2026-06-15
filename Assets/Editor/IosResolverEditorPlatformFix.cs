#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// O Google.IOSResolver referencia UnityEditor.iOS.Extensions.Xcode, ausente em editores
/// Windows/Linux sem iOS Build Support. Desativa o plugin nessas plataformas.
/// </summary>
[InitializeOnLoad]
static class IosResolverEditorPlatformFix
{
    const string IosResolverDll = "Google.IOSResolver.dll";

    static IosResolverEditorPlatformFix()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor
            && Application.platform != RuntimePlatform.LinuxEditor)
        {
            return;
        }

        EditorApplication.delayCall += DisableIosResolverOnNonMacEditor;
    }

    static void DisableIosResolverOnNonMacEditor()
    {
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.EndsWith(IosResolverDll, System.StringComparison.OrdinalIgnoreCase))
                continue;

            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null || !importer.GetCompatibleWithEditor())
                continue;

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(false);
            importer.SaveAndReimport();
        }
    }
}
#endif
