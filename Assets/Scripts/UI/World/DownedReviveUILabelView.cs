using TMPro;
using UnityEngine;

/// <summary>
/// Referência ao rótulo TMP do prefab world-space de reviver (sem montagem em runtime).
/// </summary>
[DisallowMultipleComponent]
public class DownedReviveUILabelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    public TextMeshProUGUI Label
    {
        get
        {
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);
            return label;
        }
    }
}
