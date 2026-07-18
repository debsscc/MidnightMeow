// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: Barra de progresso e texto de selamento em world-space sobre cada buraco. Traduzido
// ---------------------------------------------------------------- 

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
[RequireComponent(typeof(RatHoleSpawnPoint))]
public class RatHoleSealStatusUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.85f, 0f);
    [SerializeField] private SpriteRenderer holeSprite;

    private static readonly List<Vector2> ZoneBuffer = new List<Vector2>(2);

    private RatHoleSpawnPoint _hole;
    private Canvas _canvas;
    private Image _fill;
    private GameObject _barRoot;
    private TextMeshProUGUI _label;

    private void Awake()
    {
        _hole = GetComponent<RatHoleSpawnPoint>();
        ResolveHoleSprite();
        BuildUI();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_canvas != null)
            Destroy(_canvas.gameObject);
    }

    private void LateUpdate()
    {
        if (_hole == null)
            return;

        NetworkRatHoleSealManager manager = NetworkRatHoleSealManager.Instance;
        if (manager == null || !manager.IsSpawned)
        {
            if (_hole.IsSealed)
                ShowSealed();
            else
            {
                SetHoleSpriteVisible(true);
                SetVisible(false);
            }
            return;
        }

        if (!manager.TryGetSession(_hole.HoleId, out RatHoleSealSession session))
        {
            if (_hole.IsSealed)
                ShowSealed();
            else
            {
                SetHoleSpriteVisible(true);
                SetVisible(false);
            }
            return;
        }

        if (session.IsSealed || _hole.IsSealed)
        {
            ShowSealed();
            return;
        }

        if (!session.IsActive)
        {
            SetHoleSpriteVisible(true);
            SetVisible(false);
            return;
        }

        SetHoleSpriteVisible(true);
        SetVisible(true);
        SetBarVisible(true);
        _canvas.transform.SetPositionAndRotation(ResolveActiveLabelPosition(session, manager), Quaternion.identity);
        if (_fill != null)
            _fill.fillAmount = session.Progress;
        if (_label != null)
        {
            int pct = Mathf.RoundToInt(session.Progress * 100f);
            _label.text = UiLocalization.FormatSealProgress(pct);
        }
    }

    private Vector3 ResolveActiveLabelPosition(in RatHoleSealSession session, NetworkRatHoleSealManager manager)
    {
        Vector2 anchor = _hole.AnchorPosition;
        CooperativeZoneLabelPlacementUtility.CollectSealZones(session, ZoneBuffer);

        float visualRadius = 1.1f;
        RatHoleSealConfig config = manager != null ? manager.Config : null;
        if (config != null)
            visualRadius = config.GetZoneVisualDiameter() * 0.5f;

        return CooperativeZoneLabelPlacementUtility.ResolvePosition(
            ZoneBuffer,
            visualRadius,
            anchor,
            offset,
            entityAnchorForSideChoice: anchor);
    }

    private void ShowSealed()
    {
        SetHoleSpriteVisible(false);
        SetVisible(true);
        SetBarVisible(false);
        _canvas.transform.SetPositionAndRotation((Vector3)_hole.AnchorPosition + offset, Quaternion.identity);
        if (_label != null)
            _label.text = UiLocalization.GetSealComplete();
    }

    private void ResolveHoleSprite()
    {
        if (holeSprite != null)
            return;

        holeSprite = GetComponent<SpriteRenderer>();
        if (holeSprite == null)
            holeSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void SetHoleSpriteVisible(bool visible)
    {
        if (holeSprite != null)
            holeSprite.enabled = visible;
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void SetBarVisible(bool visible)
    {
        if (_barRoot != null)
            _barRoot.SetActive(visible);
    }

    private void BuildUI()
    {
        // Mesmo canvas/tamanho/sorting do prompt — escala world fixa (sem herdar scale do buraco).
        _canvas = GameplayUiFonts.CreateWorldInteractionCanvas("RatHoleSealStatus", out RectTransform rootRect);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rootRect, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.35f);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        GameplayUiFonts.ApplyWorldInteraction(_label);

        _barRoot = new GameObject("ProgressBar");
        _barRoot.transform.SetParent(rootRect, false);
        var barRect = _barRoot.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.12f, 0.05f);
        barRect.anchorMax = new Vector2(0.88f, 0.32f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(_barRoot.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        Stretch(bgRect);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        Stretch(fillRect, 1.5f);
        _fill = fillGo.AddComponent<Image>();
        _fill.color = new Color(0.25f, 0.75f, 0.45f, 0.95f);
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
