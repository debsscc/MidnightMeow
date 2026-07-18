///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Anel pastel no chão indicando o raio de presença da escolta.
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Indicador world-space da área em que o jogador deve permanecer para a carruagem avançar.
/// Reutiliza <see cref="SealZoneRingVisual"/> (mesmo pipeline das zonas de conserto).
/// </summary>
[DisallowMultipleComponent]
public class CarriagePresenceZoneVisual : MonoBehaviour
{
    private CarriageController _carriage;
    private SealZoneRingVisual _ring;
    private Transform _ringRoot;

    /// <summary>Associa à carruagem e garante o anel runtime.</summary>
    public void Bind(CarriageController carriage)
    {
        _carriage = carriage;
        EnsureRing();
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (_carriage == null)
            _carriage = GetComponent<CarriageController>();

        CarriageConfig config = _carriage != null ? _carriage.Config : CarriageConfigUtility.Resolve();
        if (config == null || !config.showPlayerPresenceVisual)
        {
            SetVisible(false);
            return;
        }

        CarriageState state = _carriage != null ? _carriage.CurrentState : CarriageState.Idle;
        bool shouldShow = (state == CarriageState.Idle || state == CarriageState.Moving)
            && (_carriage == null || !_carriage.HasArrived);

        if (!shouldShow)
        {
            SetVisible(false);
            return;
        }

        EnsureRing();
        // Reaplica todo frame: permite ajustar raio/cores no CarriageConfig em Play Mode.
        ApplyAppearance(config, state);
        SetVisible(true);
    }

    private void ApplyAppearance(CarriageConfig config, CarriageState state)
    {
        if (_ring == null || config == null)
            return;

        bool idle = state == CarriageState.Idle;
        Color background = idle
            ? config.presenceZoneIdleBackgroundColor
            : config.presenceZoneMovingBackgroundColor;
        Color outline = idle
            ? config.presenceZoneIdleOutlineColor
            : config.presenceZoneMovingOutlineColor;

        _ring.Configure(
            background,
            config.presenceZoneFillColor,
            outline,
            config.presenceZoneSortingOrder,
            config.GetPlayerPresenceVisualDiameter(),
            config.presenceZoneOutlineThickness,
            config.presenceZoneShowInteriorFill);
        _ring.SetFill(0f);
    }

    private void EnsureRing()
    {
        if (_ring != null)
            return;

        Transform existing = transform.Find("PresenceZoneRing");
        GameObject ringGo;
        if (existing != null)
        {
            ringGo = existing.gameObject;
        }
        else
        {
            ringGo = new GameObject("PresenceZoneRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            ringGo.AddComponent<SpriteRenderer>();
            ringGo.AddComponent<SealZoneRingVisual>();
        }

        _ringRoot = ringGo.transform;
        _ring = ringGo.GetComponent<SealZoneRingVisual>();
        if (_ring == null)
            _ring = ringGo.AddComponent<SealZoneRingVisual>();
    }

    private void SetVisible(bool visible)
    {
        if (_ringRoot != null && _ringRoot.gameObject.activeSelf != visible)
            _ringRoot.gameObject.SetActive(visible);
    }
}
