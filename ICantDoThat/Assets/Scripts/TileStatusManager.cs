using System.Collections.Generic;
using UnityEngine;

public class TileStatusManager : MonoBehaviour
{
    public static TileStatusManager Instance;

    // Tracks which character is on which tile
    private Dictionary<string, GameObject> tileOccupants = new Dictionary<string, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateTile(string tileName, GameObject character)
    {
        // Remove character from their old tile
        List<string> keys = new List<string>(tileOccupants.Keys);
        foreach (string key in keys)
        {
            if (tileOccupants[key] == character)
            {
                tileOccupants.Remove(key);
                break;
            }
        }

        // Assign to new tile
        tileOccupants[tileName] = character;
        Debug.Log($"Tile {tileName} occupied by {character.tag}");
    }

    public GameObject GetOccupant(string tileName)
    {
        tileOccupants.TryGetValue(tileName, out GameObject occupant);
        return occupant;
    }

    public bool IsTileOccupied(string tileName)
    {
        return tileOccupants.ContainsKey(tileName) && tileOccupants[tileName] != null;
    }
}
