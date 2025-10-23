using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraJuiceController : MonoBehaviour
{
    [Header("Lane Swap Sway (roll)")]
    [SerializeField] float rollDegrees = 2.5f;         // ±2–3°
    [SerializeField] float rollDuration = 0.12f;       // 0.1–0.15s
    [SerializeField] AnimationCurve rollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("FOV Bumps")]
    [SerializeField] float fovJumpDelta = 2f;          // +2 on jump
    [SerializeField] float fovSlideDelta = -2f;        // −2 on slide
    [SerializeField] float fovDuration = 0.15f;        // quick ease
    [SerializeField] AnimationCurve fovCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shakes (uses existing CameraShaker)")]
    [SerializeField] float nearMissShakeAmp = 0.05f;   // very low amp
    [SerializeField] float nearMissShakeDur = 0.08f;
    [SerializeField] float hitShakeAmp = 0.35f;
    [SerializeField] float hitShakeDur = 0.25f;

    Camera _cam;
    float _baseFov;
    Coroutine _rollCo, _fovCo;
    CameraShaker _shaker;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _baseFov = _cam.fieldOfView;
        _shaker = GetComponent<CameraShaker>();
    }

    void OnEnable()
    {
        GameEvents.OnLaneSwap += HandleLaneSwap;
        GameEvents.OnJump += HandleJump;
        GameEvents.OnSlide += HandleSlide;
        GameEvents.OnNearMiss += HandleNearMissShake;
        GameEvents.OnHit += HandleHitShake;
    }

    void OnDisable()
    {
        GameEvents.OnLaneSwap -= HandleLaneSwap;
        GameEvents.OnJump -= HandleJump;
        GameEvents.OnSlide -= HandleSlide;
        GameEvents.OnNearMiss -= HandleNearMissShake;
        GameEvents.OnHit -= HandleHitShake;
    }

    void HandleLaneSwap(int dir)
    {
        if (_rollCo != null) StopCoroutine(_rollCo);
        _rollCo = StartCoroutine(RollSwayCo(Mathf.Sign(dir) * rollDegrees, rollDuration));
    }

    void HandleJump()
    {
        if (_fovCo != null) StopCoroutine(_fovCo);
        _fovCo = StartCoroutine(FovBumpCo(fovJumpDelta, fovDuration));
    }

    void HandleSlide()
    {
        if (_fovCo != null) StopCoroutine(_fovCo);
        _fovCo = StartCoroutine(FovBumpCo(fovSlideDelta, fovDuration));
    }

    void HandleNearMissShake()
    {
        if (_shaker == null)
            _shaker = GetComponent<CameraShaker>();

        if (_shaker != null)
            _shaker.Shake(nearMissShakeDur, nearMissShakeAmp);
    }

    void HandleHitShake()
    {
        if (_shaker == null)
            _shaker = GetComponent<CameraShaker>();

        if (_shaker != null)
            _shaker.Shake(hitShakeDur, hitShakeAmp);
    }

    IEnumerator RollSwayCo(float targetRoll, float dur)
    {
        // go to target
        float t = 0f;
        float start = _cam.transform.localEulerAngles.z;
        start = (start > 180f) ? start - 360f : start; // normalize to [-180,180]
        float end = targetRoll;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = rollCurve.Evaluate(Mathf.Clamp01(t));
            float z = Mathf.Lerp(start, end, k);
            var e = _cam.transform.localEulerAngles;
            _cam.transform.localEulerAngles = new Vector3(e.x, e.y, z);
            yield return null;
        }
        // return to 0
        t = 0f; start = end; end = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = rollCurve.Evaluate(Mathf.Clamp01(t));
            float z = Mathf.Lerp(start, end, k);
            var e = _cam.transform.localEulerAngles;
            _cam.transform.localEulerAngles = new Vector3(e.x, e.y, z);
            yield return null;
        }
    }

    IEnumerator FovBumpCo(float delta, float dur)
    {
        float start = _cam.fieldOfView;
        float end = Mathf.Clamp(_baseFov + delta, 40f, 100f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            _cam.fieldOfView = Mathf.Lerp(start, end, fovCurve.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
        // return
        t = 0f; start = _cam.fieldOfView; end = _baseFov;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            _cam.fieldOfView = Mathf.Lerp(start, end, fovCurve.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
    }
}
