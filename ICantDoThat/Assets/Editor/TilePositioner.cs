using UnityEngine;
using UnityEditor;

public class TilePositioner : EditorWindow
{
    private float originX = -6.1435f;  // X of bottom-left tile (u1)
    private float originY = -4.2897f;  // Y of bottom-left tile (u1)
    private float tileSize = 0.5f;     // adjust this until tiles align
    private float lockedZ = 279.704f;  // your map's Z

    // u=0, t=1, s=2 ... a=20
    private const string ROW_LETTERS = "utsrqponmlkjihgfedcba";

    [MenuItem("Tools/Tile Positioner")]
    public static void ShowWindow()
    {
        GetWindow<TilePositioner>("Tile Positioner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tile Auto-Positioner", EditorStyles.boldLabel);

        originX = EditorGUILayout.FloatField("Origin X (u1)", originX);
        originY = EditorGUILayout.FloatField("Origin Y (u1)", originY);
        tileSize = EditorGUILayout.FloatField("Tile Size", tileSize);
        lockedZ = EditorGUILayout.FloatField("Locked Z", lockedZ);

        GUILayout.Space(10);

        if (GUILayout.Button("SNAP ALL TILES"))
            SnapAllTiles();
    }

private void SnapAllTiles()
{
    TileData[] allTiles = Resources.FindObjectsOfTypeAll<TileData>();
    int snapped = 0;

    foreach (TileData tile in allTiles)
    {
        // Skip prefab assets, only process scene objects
        if (tile.gameObject.scene.name == null) continue;

        string name = tile.tileName.ToLower().Trim();
        if (name.Length < 2) continue;

        char rowChar = name[0];
        int rowIndex = ROW_LETTERS.IndexOf(rowChar);
        if (!int.TryParse(name.Substring(1), out int col)) continue;
        if (rowIndex < 0 || col < 1) continue;

        float x = originX + (col - 1) * tileSize;
        float y = originY + rowIndex * tileSize;

        Undo.RecordObject(tile.transform, "Snap Tile");
        tile.transform.position = new Vector3(x, y, lockedZ);
        snapped++;
    }

    Debug.Log($"Snapped {snapped} tiles.");
}

}
