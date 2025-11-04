// Assets/Scripts/Systems/InputManager.cs
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using ET = UnityEngine.InputSystem.EnhancedTouch;  // alias to avoid Touch ambiguity
#endif

public class InputManager : SingletonServiceBehaviour<InputManager>
{
    public static InputManager I => ServiceLocator.TryGet(out InputManager service) ? service : null;

    [Header("Swipe config")]
    public float minSwipeInches = 0.25f;
    public float horizMaxAngle = 35f;
    public float vertMaxAngle = 35f;

    public enum InputCommand
    {
        Left,
        Right,
        Up,
        Down
    }

    struct SwipeTracker
    {
        public Vector2 start;
        public bool tracking;
    }

    enum SwipePhase
    {
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled
    }

    float _dpi = 160f;
    const int MaxBufferedCommands = 8;
    readonly Queue<InputCommand> _commandQueue = new Queue<InputCommand>();
    readonly Dictionary<int, SwipeTracker> _activeSwipes = new Dictionary<int, SwipeTracker>();
#if ENABLE_INPUT_SYSTEM
    bool _enhancedTouchEnabled;
#endif

    public bool Left { get; private set; }
    public bool Right { get; private set; }
    public bool Up { get; private set; }
    public bool Down { get; private set; }

    public override void Initialize()
    {
        _dpi = Mathf.Max(1f, Screen.dpi);
#if ENABLE_INPUT_SYSTEM
        _enhancedTouchEnabled = TryEnableEnhancedTouch();
#endif
    }

    public override void Shutdown()
    {
#if ENABLE_INPUT_SYSTEM
        if (_enhancedTouchEnabled)
        {
            ET.EnhancedTouchSupport.Disable();
            _enhancedTouchEnabled = false;
        }
#endif
        _commandQueue.Clear();
        _activeSwipes.Clear();
        Left = Right = Up = Down = false;
    }

    void Update()
    {
        // clear latches
        Left = Right = Up = Down = false;

#if ENABLE_INPUT_SYSTEM
        // Desktop/test (New Input)
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) RegisterCommand(InputCommand.Left);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) RegisterCommand(InputCommand.Right);
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) RegisterCommand(InputCommand.Up);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) RegisterCommand(InputCommand.Down);
        }

#else
        // Desktop/test (Legacy Input)
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) RegisterCommand(InputCommand.Left);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) RegisterCommand(InputCommand.Right);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) RegisterCommand(InputCommand.Up);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) RegisterCommand(InputCommand.Down);
#endif

        // Touch swipes (EnhancedTouch)
#if ENABLE_INPUT_SYSTEM
        if (_enhancedTouchEnabled)
        {
            var touches = ET.Touch.activeTouches;  // <- explicitly EnhancedTouch.Touch
            if (touches.Count == 0)
            {
                _activeSwipes.Clear();
                return;
            }

            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                HandleTouch(t.touchId, t.screenPosition, ConvertPhase(t.phase));
            }
            return;
        }
#endif

        if (Input.touchCount == 0)
        {
            _activeSwipes.Clear();
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch legacyTouch = Input.GetTouch(i);
            HandleTouch(legacyTouch.fingerId, legacyTouch.position, ConvertPhase(legacyTouch.phase));
        }
    }

    public bool TryDequeue(out InputCommand command)
    {
        if (_commandQueue.Count > 0)
        {
            command = _commandQueue.Dequeue();
            return true;
        }

        command = default;
        return false;
    }

    public void ClearBufferedCommands()
    {
        _commandQueue.Clear();
    }

#if ENABLE_INPUT_SYSTEM
    bool TryEnableEnhancedTouch()
    {
        try
        {
            ET.EnhancedTouchSupport.Enable();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InputManager] Enhanced Touch support unavailable: {ex.Message}");
            return false;
        }
    }
#endif

    void RegisterCommand(InputCommand command)
    {
        switch (command)
        {
            case InputCommand.Left:
                Left = true;
                break;
            case InputCommand.Right:
                Right = true;
                break;
            case InputCommand.Up:
                Up = true;
                break;
            case InputCommand.Down:
                Down = true;
                break;
        }

        if (_commandQueue.Count >= MaxBufferedCommands)
        {
            _commandQueue.Dequeue();
        }
        _commandQueue.Enqueue(command);
    }

    void HandleTouch(int fingerId, Vector2 position, SwipePhase phase)
    {
        if (phase == SwipePhase.Began)
        {
            _activeSwipes[fingerId] = new SwipeTracker
            {
                start = position,
                tracking = true
            };
            return;
        }

        if (!_activeSwipes.TryGetValue(fingerId, out SwipeTracker tracker))
        {
            return;
        }

        if (!tracker.tracking)
        {
            if (phase == SwipePhase.Ended || phase == SwipePhase.Canceled)
            {
                _activeSwipes.Remove(fingerId);
            }
            return;
        }

        if (phase == SwipePhase.Moved || phase == SwipePhase.Stationary || phase == SwipePhase.Ended || phase == SwipePhase.Canceled)
        {
            float minSwipePixels = minSwipeInches * _dpi;
            Vector2 delta = position - tracker.start;

            if (delta.magnitude >= minSwipePixels)
            {
                if (TryResolveDirection(delta, out InputCommand command))
                {
                    RegisterCommand(command);
                }

                tracker.tracking = false;
                _activeSwipes[fingerId] = tracker;
            }

            if (phase == SwipePhase.Ended || phase == SwipePhase.Canceled)
            {
                _activeSwipes.Remove(fingerId);
            }
        }
    }

    bool TryResolveDirection(Vector2 delta, out InputCommand command)
    {
        command = default;
        float angleR = Vector2.Angle(delta, Vector2.right);
        float angleU = Vector2.Angle(delta, Vector2.up);

        if (angleR <= horizMaxAngle)
        {
            command = InputCommand.Right;
            return true;
        }

        if (angleR >= 180f - horizMaxAngle)
        {
            command = InputCommand.Left;
            return true;
        }

        if (angleU <= vertMaxAngle)
        {
            command = InputCommand.Up;
            return true;
        }

        if (angleU >= 180f - vertMaxAngle)
        {
            command = InputCommand.Down;
            return true;
        }

        return false;
    }

    SwipePhase ConvertPhase(UnityEngine.TouchPhase phase)
    {
        switch (phase)
        {
            case UnityEngine.TouchPhase.Began:
                return SwipePhase.Began;
            case UnityEngine.TouchPhase.Moved:
                return SwipePhase.Moved;
            case UnityEngine.TouchPhase.Stationary:
                return SwipePhase.Stationary;
            case UnityEngine.TouchPhase.Ended:
                return SwipePhase.Ended;
            case UnityEngine.TouchPhase.Canceled:
                return SwipePhase.Canceled;
            default:
                return SwipePhase.Stationary;
        }
    }

#if ENABLE_INPUT_SYSTEM
    SwipePhase ConvertPhase(UnityEngine.InputSystem.TouchPhase phase)
    {
        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                return SwipePhase.Began;
            case UnityEngine.InputSystem.TouchPhase.Moved:
                return SwipePhase.Moved;
            case UnityEngine.InputSystem.TouchPhase.Stationary:
                return SwipePhase.Stationary;
            case UnityEngine.InputSystem.TouchPhase.Ended:
                return SwipePhase.Ended;
            case UnityEngine.InputSystem.TouchPhase.Canceled:
                return SwipePhase.Canceled;
            default:
                return SwipePhase.Stationary;
        }
    }
#endif
}
