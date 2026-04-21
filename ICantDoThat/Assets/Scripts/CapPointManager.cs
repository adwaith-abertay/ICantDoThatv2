using System.Collections.Generic;
using UnityEngine;

public class CapPointManager : MonoBehaviour
{
    public static CapPointManager Instance;

    [Header("Cap Point Tiles")]
    public List<string> capPointTiles = new List<string>();

    [Header("Materials")]
    public Material activeMaterial;
    public Material destroyedMaterial;

    private HashSet<string> destroyedCapPoints = new HashSet<string>();

    private void Awake() => Instance = this;

    private void Start()
    {
        foreach (string tileName in capPointTiles)
        {
            TileData tile = GridManager.Instance.GetTile(tileName);
            if (tile != null && activeMaterial != null)
            {
                MeshRenderer r = tile.GetComponent<MeshRenderer>();
                if (r != null) r.material = activeMaterial;
            }
        }
    }

    // Called by CharacterMovement every time a crew member steps on a tile
    // Scientist instant-destroys; others use the 4-step system in CharacterMovement
    public void CheckCapPoint(string tileName)
    {
        if (capPointTiles.Contains(tileName) && !destroyedCapPoints.Contains(tileName))
        {
            // Only the Scientist triggers instant destroy via here
            // Non-scientists call DestroyCapPoint directly after their 4-step countdown
        }
    }

    // Now public so CharacterMovement and AIBrain can call it directly
    public void DestroyCapPoint(string tileName)
    {
        if (destroyedCapPoints.Contains(tileName)) return;

        destroyedCapPoints.Add(tileName);
        Debug.Log($"Cap point {tileName} destroyed! {GetActiveCount()} remaining.");

        TileData tile = GridManager.Instance.GetTile(tileName);
        if (tile != null && destroyedMaterial != null)
        {
            MeshRenderer r = tile.GetComponent<MeshRenderer>();
            if (r != null) r.material = destroyedMaterial;
        }

        if (GetActiveCount() == 0)
        {
            Debug.Log("All cap points destroyed!");
            GameManager.Instance.AIWins("All cap points destroyed!");
        }
    }

    public int GenerateEnergy()
    {
        int energy = GetActiveCount();
        Debug.Log($"Cap points generated {energy} energy.");
        return energy;
    }

    public int GetActiveCount() => capPointTiles.Count - destroyedCapPoints.Count;

    public bool IsCapPoint(string tileName) =>
        capPointTiles.Contains(tileName) && !destroyedCapPoints.Contains(tileName);
}
