using UnityEngine;

public class GroundRepeater : MonoBehaviour
{
    [Header("References")]
    public Transform runner;           // drag your Runner root here
    [Header("Tile Setup")]
    public float tileLength = 50f;     // the Z length of ONE tile
    public int tileCount = 3;          // number of child tiles

    Transform[] tiles;

    void Awake()
    {
        int n = transform.childCount;
        tiles = new Transform[n];
        for (int i = 0; i < n; i++) tiles[i] = transform.GetChild(i);
        System.Array.Sort(tiles, (a, b) => a.position.z.CompareTo(b.position.z));
    }

    void Update()
    {
        if (!runner) return;

        // If a tile’s far edge is fully behind the runner, push it forward by total track length.
        float backEdgeZ = runner.position.z - tileLength;
        for (int i = 0; i < tiles.Length; i++)
        {
            float tileFrontZ = tiles[i].position.z + tileLength * 0.5f;
            if (tileFrontZ < backEdgeZ)
            {
                float jump = tileLength * tileCount;
                tiles[i].position += new Vector3(0f, 0f, jump);
            }
        }
    }
}
