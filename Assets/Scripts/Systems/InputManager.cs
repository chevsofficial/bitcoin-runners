// Assets/Scripts/Systems/InputManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using ET = UnityEngine.InputSystem.EnhancedTouch;  // alias to avoid Touch ambiguity

public class InputManager : SingletonServiceBehaviour<InputManager>
{
    public static InputManager I => ServiceLocator.TryGet(out InputManager service) ? service : null;

    [Header("Swipe config")]
    public float minSwipeInches = 0.25f;
    public float horizMaxAngle = 35f;
    public float vertMaxAngle = 35f;

    float _dpi = 160f;
    Vector2 _start;
    bool _tracking;

    public bool Left { get; private set; }
    public bool Right { get; private set; }
    public bool Up { get; private set; }
    public bool Down { get; private set; }

    public override void Initialize()
    {
        _dpi = Mathf.Max(1f, Screen.dpi);
        ET.EnhancedTouchSupport.Enable();
    }

    public override void Shutdown()
    {
        ET.EnhancedTouchSupport.Disable();
        Left = Right = Up = Down = false;
        _tracking = false;
    }

    void Update()
    {
        // clear latches
        Left = Right = Up = Down = false;

        // Desktop/test (New Input)
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Left = true;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Right = true;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Up = true;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Down = true;
        }

        // Touch swipes (EnhancedTouch)
        var touches = ET.Touch.activeTouches;  // <- explicitly EnhancedTouch.Touch
        if (touches.Count == 0) { _tracking = false; return; }

        var t = touches[0];
        float minSwipePixels = minSwipeInches * _dpi;

        if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _tracking = true;
            _start = t.screenPosition;
        }
        else if (_tracking && (t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                               t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                               t.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
        {
            Vector2 delta = t.screenPosition - _start;
            if (delta.magnitude >= minSwipePixels)
            {
                float angleR = Vector2.Angle(delta, Vector2.right);
                float angleU = Vector2.Angle(delta, Vector2.up);

                if (angleR <= horizMaxAngle) Right = true;
                else if (angleR >= 180f - horizMaxAngle) Left = true;
                else if (angleU <= vertMaxAngle) Up = true;
                else if (angleU >= 180f - vertMaxAngle) Down = true;

                _tracking = false;
            }
            else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                _tracking = false;
            }
        }
    }
}
