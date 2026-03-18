using System.Collections.Generic;
using UnityEngine;

public class FireSpread : MonoBehaviour
{
    public static FireSpread Instance;

    [Header("Fire Settings")]
    public string fireSeedTile;      // set in Inspector
    public Material fireMaterial;
    public Material defaultMaterial;

    private HashSet<string> burningTiles = new HashSet<string>();
    private Queue<string> spreadQueue = new Queue<string>();
    private bool firstSpread = true;
    private bool fireActive = false;

    private void Awake() => Instance = this;

    // Player presses "Start Fire"
    public void IgniteTileByName(string _ignored)
    {
        if (fireActive) return;

        IgniteTile(fireSeedTile);
        fireActive = true;
        firstSpread = true;

        foreach (TileData n in GridManager.Instance.GetNeighbours(fireSeedTile))
            spreadQueue.Enqueue(n.tileName);

        Debug.Log($"Fire ignited on {fireSeedTile}! Will spread next turn.");
    }

    // Called each turn by GameManager
    public void SpreadFireTurn()
    {
        if (!fireActive || burningTiles.Count == 0) return;
        SpreadFire();
    }

    private void SpreadFire()
    {
        if (firstSpread)
        {
            // Turn 2: + shape
            List<string> firstBatch = new List<string>(spreadQueue);
            spreadQueue.Clear();

            foreach (string tileName in firstBatch)
            {
                if (!burningTiles.Contains(tileName))
                {
                    IgniteTile(tileName);
                    foreach (TileData n in GridManager.Instance.GetNeighbours(tileName))
                    {
                        if (!burningTiles.Contains(n.tileName) &&
                            !new HashSet<string>(spreadQueue).Contains(n.tileName))
                            spreadQueue.Enqueue(n.tileName);
                    }
                }
            }

            firstSpread = false;
            return;
        }

        // Turn 3+ : random 4
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
                foreach (TileData n in GridManager.Instance.GetNeighbours(chosen))
                {
                    if (!burningTiles.Contains(n.tileName) &&
                        !new HashSet<string>(spreadQueue).Contains(n.tileName))
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
        if (renderer != null) renderer.material = fireMaterial;

        burningTiles.Add(tileName);
        Debug.Log($"Fire on: {tileName}");
    }

    public void ExtinguishAllFire()
    {
        foreach (string tileName in burningTiles)
        {
            TileData tile = GridManager.Instance.GetTile(tileName);
            if (tile != null)
            {
                MeshRenderer r = tile.GetComponent<MeshRenderer>();
                if (r != null) r.material = defaultMaterial;
            }
        }

        burningTiles.Clear();
        spreadQueue.Clear();
        fireActive = false;
        firstSpread = true;
        Debug.Log("All fire extinguished!");
    }

    public bool IsFireActive() => fireActive;
    public bool IsTileOnFire(string tileName) => burningTiles.Contains(tileName);
    public HashSet<string> GetBurningTiles() => new HashSet<string>(burningTiles);
}
