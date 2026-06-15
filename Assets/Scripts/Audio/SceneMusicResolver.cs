using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Localiza a trilha configurada em uma cena (Soundtrack / Sound Track / Music).
/// </summary>
public static class SceneMusicResolver
{
    private static readonly string[] MusicObjectNames = { "Soundtrack", "Sound Track", "Music" };

    public static bool TryResolve(Scene scene, out AudioClip clip, out bool loop)
    {
        clip = null;
        loop = true;

        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (TryResolveInHierarchy(roots[i], out clip, out loop))
                return true;
        }

        return false;
    }

    public static void SuppressSceneMusicSources(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            SuppressInHierarchy(roots[i]);
    }

    private static bool TryResolveInHierarchy(GameObject root, out AudioClip clip, out bool loop)
    {
        clip = null;
        loop = true;

        if (root == null)
            return false;

        if (IsMusicObject(root.name) && root.TryGetComponent(out AudioSource rootSource))
        {
            if (TryReadClip(rootSource, out clip, out loop))
                return true;
        }

        AudioSource[] sources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || !IsMusicObject(source.gameObject.name))
                continue;

            if (TryReadClip(source, out clip, out loop))
                return true;
        }

        return false;
    }

    private static void SuppressInHierarchy(GameObject root)
    {
        if (root == null)
            return;

        if (IsMusicObject(root.name) && root.TryGetComponent(out AudioSource rootSource))
            SuppressSource(rootSource);

        AudioSource[] sources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source != null && IsMusicObject(source.gameObject.name))
                SuppressSource(source);
        }
    }

    private static bool IsMusicObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        for (int i = 0; i < MusicObjectNames.Length; i++)
        {
            if (string.Equals(objectName, MusicObjectNames[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryReadClip(AudioSource source, out AudioClip clip, out bool loop)
    {
        clip = source != null ? source.clip : null;
        loop = source == null || source.loop;

        if (clip == null)
            return false;

        return true;
    }

    private static void SuppressSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.enabled = false;
    }
}
