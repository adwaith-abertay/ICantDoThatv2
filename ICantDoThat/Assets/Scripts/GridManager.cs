using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    private Dictionary<string, TileData> tiles = new Dictionary<string, TileData>();

    // Rows: a-v (0-21), Columns: 1-34
    private const string ROW_LETTERS = "ABCDEFGHIJKLMNOPQRSTUV";

    private void Awake()
    {
        Instance = this;
        RegisterTiles();
    }

    private void RegisterTiles()
    {
        TileData[] allTiles = FindObjectsOfType<TileData>();
        foreach (TileData tile in allTiles)
        {
            string key = tile.tileName.ToLower().Trim();
            if (!tiles.ContainsKey(key))
                tiles.Add(key, tile);
            else
                Debug.LogWarning($"Duplicate tile: {key}");
        }
        Debug.Log($"GridManager: {tiles.Count} tiles registered.");
    }

    public TileData GetTile(string tileName)
    {
        string key = tileName.ToLower().Trim();
        if (tiles.TryGetValue(key, out TileData tile))
            return tile;

        Debug.LogWarning($"Tile not found: {tileName}");
        return null;
    }

    public List<TileData> GetNeighbours(string tileName)
    {
        List<TileData> neighbours = new List<TileData>();

        char row = tileName[0];
        int col = int.Parse(tileName.Substring(1));
        int rowIndex = ROW_LETTERS.IndexOf(row);

        // Up, Down, Left, Right
        int[] rowOffsets = { 1, -1, 0, 0 };
        int[] colOffsets = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int newRowIndex = rowIndex + rowOffsets[i];
            int newCol = col + colOffsets[i];

            if (newRowIndex < 0 || newRowIndex >= ROW_LETTERS.Length) continue;
            if (newCol < 1 || newCol > 34) continue;

            string neighbourName = $"{ROW_LETTERS[newRowIndex]}{newCol}";
            TileData neighbour = GetTile(neighbourName);

            if (neighbour != null && neighbour.isWalkable)
                neighbours.Add(neighbour);
        }

        return neighbours;
    }
    public List<TileData> GetAllNeighbours(string tileName)
    {
        return GetNeighbours(tileName);
    }


    public bool TileExists(string tileName)
    {
        return tiles.ContainsKey(tileName.ToLower().Trim());
    }
}
