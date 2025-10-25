// Assets/Scripts/FX/FXBootstrap.cs
using UnityEngine;

public class FXBootstrap : MonoBehaviour
{
    public GameObject debrisPrefab;
    void Awake() { FXSpawner.DebrisPrefab = debrisPrefab; }
}
