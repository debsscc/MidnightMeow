///* ----------------------------------------------------------------
// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: Componente de UI que gerencia o cursor do jogador.
// ---------------------------------------------------------------- */

using System.ComponentModel;
using UnityEngine;
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    [Header("Cursor Sprites")]
    [SerializeField] private Texture2D defaultSprite;
    [SerializeField] private Texture2D onHoverSprite;
    [SerializeField] private Texture2D onClickSprite;

    private Vector2 hotspot = Vector2.zero; //HotSpot do cursor says where the click will be registered, usually the center or top-left of the sprite

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetDefaultCursor();
    }
    public void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultSprite, hotspot, CursorMode.Auto);
    }

    public void SetHoverCursor()
    {
        Cursor.SetCursor(onHoverSprite, hotspot, CursorMode.Auto);
    }
    public void SetClickCursor()
    {
        Cursor.SetCursor(onClickSprite, hotspot, CursorMode.Auto);
    }
}
