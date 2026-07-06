using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillBarEntry : MonoBehaviour
{
    [SerializeField] private AbilitySlot abilitySlot;
    [SerializeField] private string localizationKey;
    [SerializeField] private string animatorTrigger;
    [SerializeField] private GameObject state1;
    [SerializeField] private GameObject state2;
    [SerializeField] private GameObject state3;
    [SerializeField] private GameObject state4;
    [SerializeField] private float selectedPulseScale = 1.06f;
    [SerializeField] private float selectedPulseSpeed = 4f;

    private Button _button;
    private bool _selected;
    private int _upgradeVisualState = 1;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _pulseRoutine;

    public AbilitySlot AbilitySlot => abilitySlot;
    public string LocalizationKey => localizationKey;
    public string AnimatorTrigger => animatorTrigger;
    public bool IsSelected => _selected;

    public event Action<SkillBarEntry> Clicked;

    public void Configure(AbilitySlot slot, string descriptionKey, string trigger)
    {
        abilitySlot = slot;
        localizationKey = descriptionKey;
        animatorTrigger = trigger;
        ResolveStateObjects();
        EnsureButton();
    }

    private void Awake()
    {
        _baseScale = transform.localScale;
        ResolveStateObjects();
        EnsureButton();
    }

    private void OnDisable()
    {
        StopPulse();
        transform.localScale = _baseScale;
    }

    private void ResolveStateObjects()
    {
        if (state1 == null) state1 = transform.Find("State1")?.gameObject;
        if (state2 == null) state2 = transform.Find("State2")?.gameObject;
        if (state3 == null) state3 = transform.Find("State3")?.gameObject;
        if (state4 == null) state4 = transform.Find("State4")?.gameObject;
    }

    private void EnsureButton()
    {
        _button = GetComponent<Button>();
        if (_button == null)
            _button = gameObject.AddComponent<Button>();

        Image raycastImage = GetComponent<Image>();
        if (raycastImage == null && state1 != null)
            raycastImage = state1.GetComponent<Image>();

        if (raycastImage != null)
            _button.targetGraphic = raycastImage;

        _button.onClick.RemoveListener(HandleClick);
        _button.onClick.AddListener(HandleClick);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplySelectionVisual();
    }

    public void ApplyUpgradeVisual(int tier, bool canAffordUpgrade)
    {
        _upgradeVisualState = ResolveVisualState(tier, canAffordUpgrade);
        SetActiveState(_upgradeVisualState);
        ApplySelectionVisual();
    }

    private void ApplySelectionVisual()
    {
        if (_selected)
            StartPulse();
        else
            StopPulse();
    }

    private void StartPulse()
    {
        if (_pulseRoutine != null)
            return;

        if (isActiveAndEnabled)
            _pulseRoutine = StartCoroutine(PulseSelection());
        else
            transform.localScale = _baseScale * selectedPulseScale;
    }

    private void StopPulse()
    {
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }

        transform.localScale = _baseScale;
    }

    private IEnumerator PulseSelection()
    {
        while (_selected)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * selectedPulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1f, selectedPulseScale, wave);
            transform.localScale = _baseScale * scale;
            yield return null;
        }

        transform.localScale = _baseScale;
        _pulseRoutine = null;
    }

    private static int ResolveVisualState(int tier, bool canAffordUpgrade)
    {
        if (tier >= 3)
            return 4;

        if (canAffordUpgrade)
            return 2;

        if (tier > 0)
            return 3;

        return 1;
    }

    private void SetActiveState(int visualState)
    {
        if (state1 != null) state1.SetActive(visualState == 1);
        if (state2 != null) state2.SetActive(visualState == 2);
        if (state3 != null) state3.SetActive(visualState == 3);
        if (state4 != null) state4.SetActive(visualState == 4);
    }

    private void HandleClick()
    {
        Clicked?.Invoke(this);
    }
}
