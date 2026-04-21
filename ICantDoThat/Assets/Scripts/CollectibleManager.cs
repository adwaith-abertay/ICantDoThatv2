using System.Collections.Generic;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;

    // Tracks which crew member holds which collectible
    private Dictionary<string, CollectibleType> crewCollectibles = new Dictionary<string, CollectibleType>();

    private void Awake() => Instance = this;

    public void TryPickup(string crewTag, string tileName)
    {
        TileData tile = GridManager.Instance.GetTile(tileName);
        if (tile == null || !tile.hasCollectible || tile.collectibleType == CollectibleType.None) return;
        if (crewCollectibles.ContainsKey(crewTag)) return; // Already holding one

        crewCollectibles[crewTag] = tile.collectibleType;
        tile.hasCollectible = false;
        tile.collectibleType = CollectibleType.None;
        Debug.Log($"{crewTag} picked up {crewCollectibles[crewTag]}!");
    }

    public CollectibleType GetCollectible(string crewTag)
    {
        return crewCollectibles.TryGetValue(crewTag, out CollectibleType type) ? type : CollectibleType.None;
    }

    public bool HasCollectible(string crewTag, CollectibleType type)
    {
        return crewCollectibles.TryGetValue(crewTag, out CollectibleType held) && held == type;
    }

    // Returns tag of first crew member holding a specific collectible
    public string GetCrewWithCollectible(CollectibleType type)
    {
        foreach (var kvp in crewCollectibles)
            if (kvp.Value == type) return kvp.Key;
        return null;
    }
}
