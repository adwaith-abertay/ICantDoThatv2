using System.Collections.Generic;
using UnityEngine;

public class FireSpread : MonoBehaviour
{
    public static FireSpread Instance;

    [Header("Fire Settings")]
    public string startingFireTile;

    [Header("Spawn Depth")]
    public float effectSpawnZ = -13.5f;

    public GameObject firePrefab;
    public GameObject foamPrefab;

    private HashSet<string> burningTiles = new HashSet<string>();
    private HashSet<string> foamedTiles = new HashSet<string>();
    private Dictionary<string, GameObject> spawnedFires = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> spawnedFoam = new Dictionary<string, GameObject>();
    private bool fireSystemBroken = false;

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
        if (fireSystemBroken)
        {
            Debug.Log("Fire system broken — no spreading this turn.");
            return;
        }

        HashSet<string> toIgnite = new HashSet<string>();

        foreach (string burning in burningTiles)
        {
            List<TileData> neighbours = GridManager.Instance.GetAllNeighbours(burning);
            foreach (TileData neighbour in neighbours)
            {
                if (foamedTiles.Contains(neighbour.tileName)) continue;
                if (burningTiles.Contains(neighbour.tileName)) continue;
                if (!neighbour.isWalkable) continue;
                toIgnite.Add(neighbour.tileName);
            }
        }

        foreach (string tile in toIgnite)
            IgniteTile(tile);
    }

    public void IgniteTile(string tileName)
    {
        if (burningTiles.Contains(tileName)) return;
        if (foamedTiles.Contains(tileName)) return;

        GameObject tileObj = GameObject.Find(tileName);
        if (tileObj == null)
        {
            Debug.LogError($"IgniteTile: Could not find tile '{tileName}'");
            return;
        }

        if (firePrefab != null)
        {
            Vector3 spawnPos = GetSpawnPosition(tileObj);
            GameObject fire = Instantiate(firePrefab, spawnPos, Quaternion.identity);
            spawnedFires[tileName] = fire;
            Debug.Log($"Fire spawned at {spawnPos}");
        }
        else
        {
            Debug.LogError("firePrefab is NULL — drag it into the Inspector!");
        }

        burningTiles.Add(tileName);
        Debug.Log($"Fire spread to: {tileName}");
    }

    public void IgniteTileByName(string tileName) => IgniteTile(tileName);

    public void ExtinguishTile(string tileName)
    {
        if (!burningTiles.Contains(tileName)) return;

        // Remove fire visual
        if (spawnedFires.ContainsKey(tileName))
        {
            Destroy(spawnedFires[tileName]);
            spawnedFires.Remove(tileName);
        }

        burningTiles.Remove(tileName);

        // Mark as foamed — permanently fireproof
        foamedTiles.Add(tileName);

        // Spawn foam visual
        if (foamPrefab != null)
        {
            GameObject tileObj = GameObject.Find(tileName);
            if (tileObj != null)
            {
                Vector3 spawnPos = GetSpawnPosition(tileObj);
                GameObject foam = Instantiate(foamPrefab, spawnPos, Quaternion.identity);
                spawnedFoam[tileName] = foam;
            }
        }

        Debug.Log($"Fire extinguished at {tileName} — tile is now foamed and fireproof!");

        // If start tile is extinguished, fire system breaks
        if (tileName == startingFireTile)
        {
            fireSystemBroken = true;
            Debug.Log("Start fire tile extinguished — fire system broken! No more spreading.");
        }
    }

    public void ExtinguishAllFire()
    {
        List<string> tiles = new List<string>(burningTiles);
        foreach (string tile in tiles)
            ExtinguishTile(tile);
    }

    // Extinguisher holders and Robot are immune to fire damage
    public bool ShouldTakeFirDamage(GameObject character)
    {
        if (character == null) return false;

        // Robot is always immune
        if (character.GetComponent<Robot>() != null) return false;

        // Extinguisher holder is immune
        CharacterMovement cm = character.GetComponent<CharacterMovement>();
        if (cm != null && CollectibleManager.Instance.HasCollectible(
            character.tag, CollectibleType.FireExtinguisher)) return false;

        return true;
    }

    // Uses tile's actual transform Z so particle systems render at the correct depth
    private Vector3 GetSpawnPosition(GameObject tileObj)
    {
        Renderer r = tileObj.GetComponent<Renderer>();
        Vector3 center = r != null ? r.bounds.center : tileObj.transform.position;
        return new Vector3(center.x, center.y-1.2f, -13.5f);
    }

    public bool IsFireActive() => burningTiles.Count > 0;
    public bool IsTileOnFire(string tileName) => burningTiles.Contains(tileName);
    public bool IsTileFoamed(string tileName) => foamedTiles.Contains(tileName);
    public bool IsFireSystemBroken() => fireSystemBroken;
    public HashSet<string> GetBurningTiles() => new HashSet<string>(burningTiles);
}