// Assets/Scripts/Camera/CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // assign your Runner here

    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 3.0f, -6.0f); // y up, z back
    public float smoothTime = 0.12f;                       // follow damping

    [Header("Look")]
    public float lookAheadZ = 6f;                          // look a bit in front of runner
    public float lookSlerp = 0.15f;                        // rotation damping

    Vector3 _vel; // internal smooth velocity

    void LateUpdate()
    {
        if (!target) return;

        // Smooth position follow
        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);

        // Smooth look-at: a point a bit above and ahead of the runner
        Vector3 lookPoint = target.position + new Vector3(0f, 1.2f, lookAheadZ);
        Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lookSlerp);
    }
}
