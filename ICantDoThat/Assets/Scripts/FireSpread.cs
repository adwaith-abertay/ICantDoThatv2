using System.Collections.Generic;
using UnityEngine;

public class FireSpread : MonoBehaviour
{
    [Header("Fire Settings")]
    public string startTile;
    public Material fireMaterial;

    private HashSet<string> burningTiles = new HashSet<string>();
    private Queue<string> spreadQueue = new Queue<string>();
    private bool firstSpread = true;

    private void Start()
    {
        // Ignite the start tile immediately
        IgniteTile(startTile);

        // Queue its 4 neighbours for the first F press (+ shape)
        List<TileData> neighbours = GridManager.Instance.GetNeighbours(startTile);
        foreach (TileData n in neighbours)
            spreadQueue.Enqueue(n.tileName);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            SpreadFire();
    }

    private void SpreadFire()
    {
        if (firstSpread)
        {
            // Burn ALL 4 neighbours at once (+ shape)
            List<string> firstBatch = new List<string>(spreadQueue);
            spreadQueue.Clear();

            foreach (string tileName in firstBatch)
            {
                if (!burningTiles.Contains(tileName))
                {
                    IgniteTile(tileName);

                    List<TileData> neighbours = GridManager.Instance.GetNeighbours(tileName);
                    foreach (TileData n in neighbours)
                    {
                        if (!burningTiles.Contains(n.tileName) && !new HashSet<string>(spreadQueue).Contains(n.tileName))
                            spreadQueue.Enqueue(n.tileName);
                    }
                }
            }

            firstSpread = false;
            return;
        }

        // After first spread — randomly pick 4 tiles each F press
        int spread = 0;

        while (spread < 4 && spreadQueue.Count > 0)
        {
            List<string> queueList = new List<string>(spreadQueue);
            spreadQueue.Clear();

            int randomIndex = Random.Range(0, queueList.Count);
            string chosen = queueList[randomIndex];
            queueList.RemoveAt(randomIndex);

            foreach (string t in queueList)
                spreadQueue.Enqueue(t);

            if (!burningTiles.Contains(chosen))
            {
                IgniteTile(chosen);

                List<TileData> neighbours = GridManager.Instance.GetNeighbours(chosen);
                foreach (TileData n in neighbours)
                {
                    if (!burningTiles.Contains(n.tileName) && !new HashSet<string>(spreadQueue).Contains(n.tileName))
                        spreadQueue.Enqueue(n.tileName);
                }

                spread++;
            }
        }
    }

    private void IgniteTile(string tileName)
    {
        TileData tile = GridManager.Instance.GetTile(tileName);
        if (tile == null) return;

        MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.material = fireMaterial;

        burningTiles.Add(tileName);
        Debug.Log($"Fire spread to: {tileName}");
    }
}
