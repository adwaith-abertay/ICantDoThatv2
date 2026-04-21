using System.Collections.Generic;
using UnityEngine;

public class FireSpread : MonoBehaviour
{
    public static FireSpread Instance;

    [Header("Fire Settings")]
    public string startingFireTile;
    public GameObject fireSpritePrefab; // Drag your fire 2D sprite prefab here

    private HashSet<string> burningTiles = new HashSet<string>();
    private Dictionary<string, GameObject> spawnedFires = new Dictionary<string, GameObject>();

    private void Awake() => Instance = this;

    private void Start() { }

    public void StartFire()
    {
        if (string.IsNullOrEmpty(startingFireTile))
        {
            Debug.LogWarning("No starting fire tile assigned!");
            return;
        }
        IgniteTile(startingFireTile);
    }

    public void SpreadFireTurn()
    {
        HashSet<string> toIgnite = new HashSet<string>();

        foreach (string burning in burningTiles)
        {
            List<TileData> neighbours = GridManager.Instance.GetAllNeighbours(burning);
            foreach (TileData neighbour in neighbours)
            {
                if (!burningTiles.Contains(neighbour.tileName) && neighbour.isWalkable)
                    toIgnite.Add(neighbour.tileName);
            }
        }

        foreach (string tile in toIgnite)
            IgniteTile(tile);
    }

    public void IgniteTile(string tileName)
    {
        if (burningTiles.Contains(tileName)) return;

        GameObject tileObj = GameObject.Find(tileName);
        if (tileObj == null) return;

        // Spawn fire sprite on top of the tile
        if (fireSpritePrefab != null)
        {
            Renderer r = tileObj.GetComponent<Renderer>();
            Vector3 spawnPos = r != null ? r.bounds.center : tileObj.transform.position;

            // Slightly in front so it renders on top
            spawnPos.z -= 0.1f;

            GameObject fire = Instantiate(fireSpritePrefab, spawnPos, Quaternion.identity);
            fire.transform.SetParent(tileObj.transform);
            spawnedFires[tileName] = fire;
        }

        burningTiles.Add(tileName);
        Debug.Log($"Fire spread to: {tileName}");
    }

    public void IgniteTileByName(string tileName) => IgniteTile(tileName);

    public void ExtinguishTile(string tileName)
    {
        if (!burningTiles.Contains(tileName)) return;

        // Destroy fire sprite
        if (spawnedFires.ContainsKey(tileName))
        {
            Destroy(spawnedFires[tileName]);
            spawnedFires.Remove(tileName);
        }

        burningTiles.Remove(tileName);
        Debug.Log($"Fire extinguished at: {tileName}");
    }

    public void ExtinguishAllFire()
    {
        List<string> tiles = new List<string>(burningTiles);
        foreach (string tile in tiles)
            ExtinguishTile(tile);
    }

    public bool IsFireActive() => burningTiles.Count > 0;
    public bool IsTileOnFire(string tileName) => burningTiles.Contains(tileName);
    public HashSet<string> GetBurningTiles() => new HashSet<string>(burningTiles);
}
