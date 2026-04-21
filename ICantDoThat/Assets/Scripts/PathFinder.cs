using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance;

    [Header("Pathfinding Test")]
    public string startTile;
    public string targetTile;
    public bool runTest;

    [Header("Highlight Settings")]
    public Material highlightMaterial;

    private Dictionary<TileData, Material> originalMaterials = new Dictionary<TileData, Material>();

    private void Awake() => Instance = this;

    private void Update()
    {
        if (runTest)
        {
            runTest = false;
            TestPath();
        }
    }

    private void TestPath()
    {
        List<TileData> path = FindPath(startTile, targetTile);
        if (path == null) return;

        Debug.Log($"Path found! {path.Count} tiles:");
        foreach (TileData tile in path)
            Debug.Log(tile.tileName);

        HighlightPath(path);
    }

    public void HighlightPath(List<TileData> path)
    {
        ClearHighlights();
        foreach (TileData tile in path)
        {
            MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                originalMaterials[tile] = renderer.material;
                renderer.material = highlightMaterial;
            }
        }
    }

    public void ClearHighlights()
    {
        foreach (var kvp in originalMaterials)
        {
            MeshRenderer renderer = kvp.Key.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = kvp.Value;
        }
        originalMaterials.Clear();
    }

    public List<TileData> FindPath(string startTile, string targetTile, HashSet<string> blockedTiles = null, bool canUseVents = false)
    {
        TileData start = GridManager.Instance.GetTile(startTile);
        TileData target = GridManager.Instance.GetTile(targetTile);

        if (start == null || target == null)
        {
            Debug.LogWarning("Start or target tile not found.");
            return null;
        }

        Queue<TileData> queue = new Queue<TileData>();
        Dictionary<TileData, TileData> cameFrom = new Dictionary<TileData, TileData>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            TileData current = queue.Dequeue();

            if (current == target)
                return BuildPath(cameFrom, start, target);

                foreach (TileData neighbour in GridManager.Instance.GetNeighbours(current.tileName))
                {
                    if (blockedTiles != null && blockedTiles.Contains(neighbour.tileName)) continue;
                    if (neighbour.isVent && !canUseVents) continue;

                    // Skip if this neighbour is blocked by a locked door
                    if (current.blockedNeighbours.Contains(neighbour.tileName)) continue;

                    if (!cameFrom.ContainsKey(neighbour))
                    {
                        queue.Enqueue(neighbour);
                        cameFrom[neighbour] = current;
                    }
                }
        }

        Debug.LogWarning($"No path found from {startTile} to {targetTile}");
        return null;
    }

    private List<TileData> BuildPath(Dictionary<TileData, TileData> cameFrom, TileData start, TileData target)
    {
        List<TileData> path = new List<TileData>();
        TileData current = target;

        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }
}
