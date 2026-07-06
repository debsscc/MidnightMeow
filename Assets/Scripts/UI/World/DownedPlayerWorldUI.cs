using UnityEngine;

/// <summary>
/// Instancia e posiciona o prefab de prompt "Aperte E para reviver" acima do jogador caído.
/// Estilo visual definido no prefab (mesmo padrão do selamento de buracos).
/// </summary>
[RequireComponent(typeof(NetworkPlayerHealth))]
public class DownedPlayerWorldUI : MonoBehaviour
{
    [SerializeField] private GameObject reviveUIPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.25f, 0f);

    private NetworkPlayerHealth _health;
    private GameObject _promptInstance;
    private Transform _promptTransform;

    private void Awake()
    {
        _health = GetComponent<NetworkPlayerHealth>();

        if (reviveUIPrefab == null && _health != null && _health.DownedConfig != null)
            reviveUIPrefab = _health.DownedConfig.revivePromptPrefab;

        InstantiatePromptIfNeeded();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_promptInstance != null)
            Destroy(_promptInstance);
    }

    private void InstantiatePromptIfNeeded()
    {
        if (_promptInstance != null || reviveUIPrefab == null)
            return;

        _promptInstance = Instantiate(reviveUIPrefab, transform);
        _promptInstance.name = reviveUIPrefab.name;
        _promptTransform = _promptInstance.transform;
        _promptTransform.localPosition = Vector3.zero;
        _promptTransform.localRotation = Quaternion.identity;
        _promptInstance.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_health == null || !_health.IsSpawned)
        {
            SetVisible(false);
            return;
        }

        if (_promptInstance == null)
        {
            InstantiatePromptIfNeeded();
            if (_promptInstance == null)
                return;
        }

        bool hasActiveSession = NetworkDownedReviveManager.Instance != null
                                && NetworkDownedReviveManager.Instance.HasActiveSession(_health.OwnerClientId);

        bool show = _health.CanBeRevived && !hasActiveSession;
        SetVisible(show);
        if (!show)
            return;

        _promptTransform.position = transform.position + offset;
    }

    private void SetVisible(bool visible)
    {
        if (_promptInstance != null)
            _promptInstance.SetActive(visible);
    }
}
