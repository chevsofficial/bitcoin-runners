#if UNITY_IOS
using System.Collections;
using UnityEngine;
using UnityEngine.iOS;

public class ATTRequester : MonoBehaviour
{
    IEnumerator Start()
    {
        // Wait a frame so the UI can settle
        yield return null;

        // Shows Apple ATT prompt (once per install)
        ATTrackingManager.RequestTrackingAuthorization(status =>
        {
            Debug.Log("[ATT] Authorization status: " + status);
        });
    }
}
#endif
