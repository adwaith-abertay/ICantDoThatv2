using UnityEngine;

public class TileData : MonoBehaviour
{
    public string tileName;
    public bool isWalkable = true;
    public bool isVent = false;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(tileName))
            tileName = gameObject.name;
    }
}
