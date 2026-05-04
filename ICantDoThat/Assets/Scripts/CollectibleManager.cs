using System.Collections.Generic;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;

    [Header("Captain Collectible Icons")]
    public GameObject captainAxeIcon;
    public GameObject captainExtinguisherIcon;

    [Header("Soldier Collectible Icons")]
    public GameObject soldierAxeIcon;
    public GameObject soldierExtinguisherIcon;

    [Header("Engineer Collectible Icons")]
    public GameObject engineerAxeIcon;
    public GameObject engineerExtinguisherIcon;

    [Header("Scientist Collectible Icons")]
    public GameObject scientistAxeIcon;
    public GameObject scientistExtinguisherIcon;

    private Dictionary<string, CollectibleType> crewCollectibles = new Dictionary<string, CollectibleType>();

    private void Awake()
    {
        Instance = this;

        // Ensure all icons start hidden
        foreach (GameObject icon in new GameObject[]
        {
            captainAxeIcon, captainExtinguisherIcon,
            soldierAxeIcon, soldierExtinguisherIcon,
            engineerAxeIcon, engineerExtinguisherIcon,
            scientistAxeIcon, scientistExtinguisherIcon
        })
        {
            if (icon != null) icon.SetActive(false);
        }
    }

    public void TryPickup(string crewTag, string tileName)
    {
        TileData tile = GridManager.Instance.GetTile(tileName);
        if (tile == null || !tile.hasCollectible || tile.collectibleType == CollectibleType.None) return;
        if (crewCollectibles.ContainsKey(crewTag)) return;

        CollectibleType pickedUp = tile.collectibleType; // ← save BEFORE clearing

        crewCollectibles[crewTag] = pickedUp;
        tile.hasCollectible = false;
        tile.collectibleType = CollectibleType.None;

        UpdateCollectibleIcon(crewTag, pickedUp); // ← pass saved type

        Debug.Log($"{crewTag} picked up {pickedUp}!");
    }

    private void UpdateCollectibleIcon(string crewTag, CollectibleType type)
    {
        GameObject axeIcon = null;
        GameObject extIcon = null;

        switch (crewTag)
        {
            case "Captain":
                axeIcon = captainAxeIcon;
                extIcon = captainExtinguisherIcon;
                break;
            case "Soldier":
                axeIcon = soldierAxeIcon;
                extIcon = soldierExtinguisherIcon;
                break;
            case "Engineer":
                axeIcon = engineerAxeIcon;
                extIcon = engineerExtinguisherIcon;
                break;
            case "Scientist":
                axeIcon = scientistAxeIcon;
                extIcon = scientistExtinguisherIcon;
                break;
        }

        if (axeIcon != null) axeIcon.SetActive(type == CollectibleType.Axe);
        if (extIcon != null) extIcon.SetActive(type == CollectibleType.FireExtinguisher);

        // Fire the correct event based on what was actually picked up
        if (type == CollectibleType.Axe)
            UIEventsListener.OnAxePickedUp?.Invoke(crewTag);
        else if (type == CollectibleType.FireExtinguisher)
            UIEventsListener.OnExtinguisherPickedUp?.Invoke(crewTag);
    }

    public CollectibleType GetCollectible(string crewTag)
    {
        return crewCollectibles.TryGetValue(crewTag, out CollectibleType type) ? type : CollectibleType.None;
    }

    public bool HasCollectible(string crewTag, CollectibleType type)
    {
        return crewCollectibles.TryGetValue(crewTag, out CollectibleType held) && held == type;
    }

    public string GetCrewWithCollectible(CollectibleType type)
    {
        foreach (var kvp in crewCollectibles)
            if (kvp.Value == type) return kvp.Key;
        return null;
    }
}