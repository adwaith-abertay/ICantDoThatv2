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
    public void CheckCapPoint(string tileName)
    {
        if (capPointTiles.Contains(tileName) && !destroyedCapPoints.Contains(tileName))
            DestroyCapPoint(tileName);
    }
    
    private void DestroyCapPoint(string tileName)
    {
        destroyedCapPoints.Add(tileName);
        Debug.Log($"Cap point {tileName} destroyed! {GetActiveCount()} remaining.");

        // Swap material to destroyed
        TileData tile = GridManager.Instance.GetTile(tileName);
        if (tile != null && destroyedMaterial != null)
        {
            MeshRenderer r = tile.GetComponent<MeshRenderer>();
            if (r != null) r.material = destroyedMaterial;
        }

        // If all cap points gone — AI wins
        if (GetActiveCount() == 0)
        {
            Debug.Log("All cap points destroyed!");
            GameManager.Instance.AIWins("All cap points destroyed!");
        }
    }

    // Called each turn end to give player energy
    public int GenerateEnergy()
    {
        int energy = GetActiveCount(); // 1 energy per active cap point
        Debug.Log($"Cap points generated {energy} energy.");
        return energy;
    }

    public int GetActiveCount() => capPointTiles.Count - destroyedCapPoints.Count;

    public bool IsCapPoint(string tileName) =>
        capPointTiles.Contains(tileName) && !destroyedCapPoints.Contains(tileName);
}
