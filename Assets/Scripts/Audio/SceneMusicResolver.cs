using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Localiza a trilha configurada em uma cena (Soundtrack / Sound Track / Music).
/// </summary>
public static class SceneMusicResolver
{
    private static readonly string[] MusicObjectNames = { "Soundtrack", "Sound Track", "Music", "Sountrack" };

    public static bool TryResolve(Scene scene, out AudioClip clip, out bool loop)
    {
        clip = null;
        loop = true;

        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        if (IsSceneWithoutMusic(scene.name))
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
        clip = null;
        loop = true;

        if (source == null)
            return false;

        loop = source.loop;

        // Unity 6: o Inspector grava em m_Resource; .clip pode vir nulo.
        clip = source.clip;
        if (clip == null)
        {
            AudioResource resource = source.resource;
            clip = resource as AudioClip;
        }

        return clip != null;
    }

    private static void SuppressSource(AudioSource source)
    {
        if (source == null)
            return;

        // Nunca desativar as fontes do crossfade persistente — Play() falha em component disabled
        // e a trilha das fases/menu deixa de voltar depois dos créditos.
        if (IsCrossfadeControllerSource(source))
        {
            source.Stop();
            source.volume = 0f;
            return;
        }

        source.Stop();
        source.mute = true;
        source.volume = 0f;
        source.playOnAwake = false;
        source.enabled = false;
    }

    /// <summary>True se a fonte pertence ao <see cref="MusicCrossfadeController"/> (MusicA/MusicB).</summary>
    public static bool IsCrossfadeControllerSource(AudioSource source)
    {
        if (source == null)
            return false;

        Transform parent = source.transform.parent;
        if (parent != null && parent.name == nameof(MusicCrossfadeController))
            return true;

        return source.GetComponentInParent<MusicCrossfadeController>() != null;
    }

    /// <summary>Para e silencia todas as fontes de trilha em cenas carregadas (inclui inativas).</summary>
    public static void SuppressAllLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            SuppressSceneMusicSources(SceneManager.GetSceneAt(i));
    }

    /// <summary>
    /// Hubs do fluxo pré-gameplay: não têm trilha própria (evita clip legado em Sound Track)
    /// e mantêm a música que já está tocando (ex.: lobby → loading → preparação → personagens).
    /// </summary>
    public static bool CarriesMusicAcross(string sceneName) =>
        sceneName is "Loading1" or "Loading2" or "Preparation" or "Characters";

    private static bool IsSceneWithoutMusic(string sceneName) => CarriesMusicAcross(sceneName);

    /// <summary>
    /// Cenas que forçam fade para silêncio. Vazio no fluxo atual — a trilha do lobby
    /// permanece até uma cena com Soundtrack próprio (menu, fases, vitória/derrota).
    /// </summary>
    public static bool IsSilentHubScene(string sceneName) => false;
}
