// ----------------------------------------------------------------
// CRIADO POR: Debs Carvalho
// DATA: 2026-06-28
// DESCRIÇÃO: Navegação de UI por gamepad, joystick e HID genérico (clones).
// ----------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GamepadUiAutoSelect : MonoBehaviour
{
    private const float NavThreshold = 0.45f;
    private const float NavRepeatDelay = 0.18f;

    private Vector2 _lastNavDirection;
    private float _nextNavTime;

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        HandleGenericSubmitCancel(es);

        if (!ShouldAutoSelect())
            return;

        TryNavigate(es);
    }

    private void TryNavigate(EventSystem es)
    {
        UiSelectionUtility.ClearIfInvalid();

        if (!TryReadNavigationVector(out Vector2 nav))
        {
            _lastNavDirection = Vector2.zero;
            return;
        }

        Vector2 direction = DominantDirection(nav);
        if (direction.sqrMagnitude < 0.01f)
            return;

        float now = Time.unscaledTime;
        if (direction == _lastNavDirection && now < _nextNavTime)
            return;

        _lastNavDirection = direction;
        _nextNavTime = now + NavRepeatDelay;

        GameObject currentGo = es.currentSelectedGameObject;
        if (currentGo == null)
        {
            GameObject first = FindFirstSelectable();
            if (first != null)
                es.SetSelectedGameObject(first);
            return;
        }

        MoveDirection moveDir = ToMoveDirection(direction);
        if (moveDir == MoveDirection.None)
            return;

        var moveData = new AxisEventData(es) { moveDir = moveDir };
        ExecuteEvents.Execute(currentGo, moveData, ExecuteEvents.moveHandler);
    }

    private static void HandleGenericSubmitCancel(EventSystem es)
    {
        if (GenericControllerInput.WasSubmitPressedThisFrame())
        {
            GameObject selected = es.currentSelectedGameObject;
            if (selected != null)
                ExecuteEvents.Execute(selected, new BaseEventData(es), ExecuteEvents.submitHandler);
        }

        if (GenericControllerInput.WasCancelPressedThisFrame())
        {
            GameObject selected = es.currentSelectedGameObject;
            if (selected != null)
                ExecuteEvents.Execute(selected, new BaseEventData(es), ExecuteEvents.cancelHandler);
        }
    }

    private static bool ShouldAutoSelect()
    {
        if (ScreenFlowStateMachine.CurrentPhase != ScreenFlowPhase.Gameplay)
            return true;

        return GameFlowOrchestrator.Instance != null && GameFlowOrchestrator.Instance.IsPauseActive;
    }

    private static bool TryReadNavigationVector(out Vector2 nav)
    {
        nav = Vector2.zero;

        Gamepad gp = GamepadInputUtility.Current;
        if (gp != null)
        {
            nav = gp.leftStick.ReadValue();
            if (nav.sqrMagnitude < NavThreshold * NavThreshold)
                nav = gp.dpad.ReadValue();
            if (nav.sqrMagnitude >= NavThreshold * NavThreshold)
                return true;
        }

        Joystick joystick = JoystickInputUtility.Current;
        if (joystick != null)
        {
            nav = joystick.stick.ReadValue();
            if (nav.sqrMagnitude >= NavThreshold * NavThreshold)
                return true;
        }

        if (GenericControllerInput.TryReadNavigateStick(out nav))
            return nav.sqrMagnitude >= NavThreshold * NavThreshold;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            float x = 0f;
            float y = 0f;
            if (kb.upArrowKey.isPressed) y += 1f;
            if (kb.downArrowKey.isPressed) y -= 1f;
            if (kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.rightArrowKey.isPressed) x += 1f;
            nav = new Vector2(x, y);
            return nav.sqrMagnitude > 0.01f;
        }

        return false;
    }

    private static Vector2 DominantDirection(Vector2 nav)
    {
        if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
            return new Vector2(Mathf.Sign(nav.x), 0f);

        if (Mathf.Abs(nav.y) > 0.01f)
            return new Vector2(0f, Mathf.Sign(nav.y));

        return Vector2.zero;
    }

    private static MoveDirection ToMoveDirection(Vector2 direction)
    {
        if (direction.y > 0.5f) return MoveDirection.Up;
        if (direction.y < -0.5f) return MoveDirection.Down;
        if (direction.x < -0.5f) return MoveDirection.Left;
        if (direction.x > 0.5f) return MoveDirection.Right;
        return MoveDirection.None;
    }

    private static GameObject FindFirstSelectable()
    {
        Selectable[] all = Selectable.allSelectablesArray;
        Selectable best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < all.Length; i++)
        {
            Selectable s = all[i];
            if (s == null || !s.isActiveAndEnabled || !s.IsInteractable())
                continue;

            if (s.navigation.mode == Navigation.Mode.None)
                continue;

            Canvas canvas = s.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled)
                continue;

            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            int order = root.sortingOrder;
            if (best == null || order > bestOrder)
            {
                bestOrder = order;
                best = s;
            }
        }

        return best != null ? best.gameObject : null;
    }
}
