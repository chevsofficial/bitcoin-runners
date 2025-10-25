// Assets/Scripts/FX/FX_PowerupVisuals.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PowerupSystem))]
public class FX_PowerupVisuals : MonoBehaviour
{
    [Header("Scene refs")]
    public Camera cam;                       // assign Main Camera
    public Volume postVolume;                // a Global Volume in Run scene

    [Header("Magnet")]
    public Transform magnetRing;             // child torus/quad
    public float ringPulseSpeed = 3f;
    public float ringPulseAmp = 0.12f;

    [Header("Shield")]
    public Renderer shieldRim;               // emissive rim mesh or runner mat instance
    public float shieldFlickerSpeed = 12f;
    public Color shieldBase = new Color(0.3f,0.7f,1f);
    public float shieldIntensity = 1.8f;

    [Header("Dash")]
    public ParticleSystem dashSpeedLines;    // screen-space particles on camera

    PowerupSystem _pu;
    Vignette _vig;
    ColorAdjustments _color;
    ChromaticAberration _ca;

    void Awake()
    {
        _pu = GetComponent<PowerupSystem>();
    }

    void Start()
    {
        if (_pu != null)
        {
            _pu.OnPowerupStart += OnStartPU;
            _pu.OnPowerupEnd += OnEndPU;
        }

        if (postVolume && postVolume.profile)
        {
            postVolume.profile.TryGet(out _vig);
            postVolume.profile.TryGet(out _color);
            postVolume.profile.TryGet(out _ca);
        }

        SetMagnet(false);
        SetShield(false);
        SetDash(false);
    }

    void OnDestroy()
    {
        if (_pu != null)
        {
            _pu.OnPowerupStart -= OnStartPU;
            _pu.OnPowerupEnd -= OnEndPU;
        }
    }

    void Update()
    {
        // Magnet ring gentle pulse
        if (magnetRing && _pu.Active == PowerType.Magnet)
        {
            float s = 1f + Mathf.Sin(Time.time * ringPulseSpeed) * ringPulseAmp;
            magnetRing.localScale = new Vector3(s, s, s);
        }

        // Shield rim emissive flicker
        if (shieldRim && _pu.Active == PowerType.Shield)
        {
            float k = 0.5f + 0.5f * Mathf.PerlinNoise(Time.time * shieldFlickerSpeed, 0f);
            var propBlock = new MaterialPropertyBlock();
            shieldRim.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", shieldBase * (k * shieldIntensity));
            shieldRim.SetPropertyBlock(propBlock);
        }
    }

    void OnStartPU(PowerType t)
    {
        if (t == PowerType.Magnet) SetMagnet(true);
        else if (t == PowerType.Shield) SetShield(true);
        else if (t == PowerType.Dash) SetDash(true);
    }

    void OnEndPU(PowerType t)
    {
        if (t == PowerType.Magnet) SetMagnet(false);
        else if (t == PowerType.Shield) SetShield(false);
        else if (t == PowerType.Dash) SetDash(false);
    }

    void SetMagnet(bool on)
    {
        if (magnetRing) magnetRing.gameObject.SetActive(on);
    }

    void SetShield(bool on)
    {
        if (shieldRim)
        {
            // show/hide a rim mesh (or use runner mat override)
            shieldRim.gameObject.SetActive(on);
        }
    }

    void SetDash(bool on)
    {
        if (dashSpeedLines)
        {
            var em = dashSpeedLines.emission;
            if (on)
            {
                dashSpeedLines.Play(true);
                em.rateOverTime = 60f;
            }
            else
            {
                em.rateOverTime = 0f;
                dashSpeedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Post-processing punch during dash
        if (_color) _color.saturation.value = on ? 10f : 0f;          // mild boost
        if (_ca) _ca.intensity.value = on ? 0.12f : 0.0f;             // small CA
    }
}
