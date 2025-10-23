// Assets/Scripts/Game/NearMissZone.cs
using UnityEngine;

public class NearMissZone : MonoBehaviour
{
    bool _triggered;

    void OnTriggerEnter(Collider other)
    {
        if (!GameManager.I.Alive) return;
        if (other.GetComponent<RunnerController>())
            _triggered = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (_triggered && other.GetComponent<RunnerController>())
        {
            if (GameManager.I.Alive) // you lived = near miss
            {
                var shaker = Camera.main ? Camera.main.GetComponent<CameraShaker>() : null;
                shaker?.Shake(0.25f, 0.05f);
                // Optional: small reward or stat
                // GameManager.I.AddCoin(0);
            }
            _triggered = false;
        }
    }
}