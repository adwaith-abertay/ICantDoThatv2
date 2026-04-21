using System.Collections.Generic;
using UnityEngine;

public enum CollectibleType { None, Axe, FireExtinguisher }

public class TileData : MonoBehaviour
{
    public string tileName;
    public bool isWalkable = true;
    public bool isVent = false;
    public bool isAirlock = false;
    public bool hasCollectible = false;
    public CollectibleType collectibleType = CollectibleType.None;

    // Blocked by locked doors — managed by DoorManager, no need to touch in Inspector
    [HideInInspector]
    public HashSet<string> blockedNeighbours = new HashSet<string>();

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(tileName))
            tileName = gameObject.name;
    }
}
