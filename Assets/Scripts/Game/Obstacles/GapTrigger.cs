// Assets/Scripts/Game/Obstacles/GapTrigger.cs
using UnityEngine;

public class GapTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!GameManager.I.Alive) return;

        var runner = other.GetComponent<RunnerController>();
        if (runner && !runner.IsJumping)
        {
            runner.GetComponent<HitRouter>()?.TryKill();
        }
    }
}
