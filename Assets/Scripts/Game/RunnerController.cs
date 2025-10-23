// Assets/Scripts/Game/RunnerController.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HitRouter))]
// [RequireComponent(typeof(PowerupSystem))] // Uncomment if PowerupSystem must exist
public class RunnerController : MonoBehaviour
{
    public GameConstants cfg;

    CharacterController _cc;
    HitRouter _hit;

    // Lane logic
    int lane = 1; // lanes: 0 = left, 1 = mid, 2 = right
    float laneX => (lane - 1) * cfg.laneWidth;
    float currentX;
    float laneSwitchT;

    // Motion
    Vector3 vel;
    float jumpT;

    // Slide
    float slideT;
    bool sliding;
    float _origHeight;
    Vector3 _origCenter;

    public bool IsJumping => jumpT > 0f;
    public bool IsSliding => sliding;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _hit = GetComponent<HitRouter>();
        if (_hit == null) _hit = gameObject.AddComponent<HitRouter>(); // safety

        // Set controller size from config
        _cc.height = cfg.colliderSize.y;
        _cc.radius = cfg.colliderSize.x * 0.5f;

        // Remember original collider settings for slide restore
        _origHeight = _cc.height;
        _origCenter = _cc.center;
    }

    void Update()
    {
        if (!GameManager.I.Alive) return;

        // --- Input: lane / jump / slide ---
        if (InputManager.I.Left && lane > 0)
        {
            lane--;
            laneSwitchT = 0f;
            GameEvents.LaneSwap(-1);
        }
        if (InputManager.I.Right && lane < 2)
        {
            lane++;
            laneSwitchT = 0f;
            GameEvents.LaneSwap(+1);
        }
        if (InputManager.I.Up && jumpT <= 0f && !sliding)
        {
            jumpT = cfg.jumpAirTime;
            GameEvents.Jump();
        }

        if (InputManager.I.Down && slideT <= 0f && !sliding && jumpT <= 0f)
        {
            slideT = cfg.slideTime;
            sliding = true;
            _cc.height = 1.0f; // crouch height
            _cc.center = new Vector3(_cc.center.x, _cc.height * 0.5f, _cc.center.z); // keep feet grounded
            GameEvents.Slide();
        }

        // --- Lane tween (ease-out cubic) ---
        laneSwitchT += Time.deltaTime;
        float t = Mathf.Clamp01(laneSwitchT / cfg.laneSwitchTime);
        currentX = Mathf.Lerp(currentX, laneX, 1f - Mathf.Pow(1f - t, 3f));

        // --- Forward speed ---
        vel.z = GameManager.I.Speed;

        // --- Jump arc & gravity ---
        if (jumpT > 0f)
        {
            float p = 1f - (jumpT / cfg.jumpAirTime);                  // 0..1 over air time
            float y = Mathf.Sin(Mathf.PI * p) * 1.6f;                  // ~1.6m peak
            float yVel = (y - transform.position.y) / Mathf.Max(Time.deltaTime, 0.0001f);
            vel.y = yVel;
            jumpT -= Time.deltaTime;
            if (jumpT <= 0f) vel.y = -1f; // hand off to gravity on landing phase
        }
        else
        {
            vel.y += Physics.gravity.y * Time.deltaTime * 1.5f;       // snappier gravity
            if (_cc.isGrounded && vel.y < 0f) vel.y = -2f;             // stick to ground
        }

        // --- Slide timer & restore collider ---
        if (sliding)
        {
            slideT -= Time.deltaTime;
            if (slideT <= 0f)
            {
                sliding = false;
                _cc.height = _origHeight;
                _cc.center = _origCenter;
            }
        }

        // --- Move character ---
        Vector3 target = new Vector3(currentX, transform.position.y, transform.position.z);
        float dx = target.x - transform.position.x;
        _cc.Move(new Vector3(dx, vel.y * Time.deltaTime, vel.z * Time.deltaTime));
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!GameManager.I.Alive) return;

        if (hit.collider.CompareTag("ObstacleBarrier"))
        {
            _hit.TryKill();
        }
        else if (hit.collider.CompareTag("ObstacleLowBar"))
        {
            if (!IsSliding) _hit.TryKill();
        }
        // Note: gaps typically use triggers; see GapTrigger.cs for that path.
    }
}
