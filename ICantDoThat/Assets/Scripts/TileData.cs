using UnityEngine;

public class TileData : MonoBehaviour
{
    public string tileName;
    public bool isWalkable = true;

    private void OnValidate()
    {
        // Auto-grab name from GameObject in both Editor and Play mode
        if (string.IsNullOrEmpty(tileName))
            tileName = gameObject.name;
    }
}
