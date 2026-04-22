using System.Collections.Generic;
using UnityEngine;

public class SpawnRandomizer : MonoBehaviour
{
    [Header("Spawn Tiles (add 5 tile names)")]
    public List<string> spawnTiles = new List<string>();

    [Header("Characters (drag GameObjects here)")]
    public GameObject captain;
    public GameObject scientist;
    public GameObject engineer;
    public GameObject soldier;
    public GameObject robot;

    private void Awake()
    {
        if (spawnTiles.Count < 5)
        {
            Debug.LogWarning("SpawnRandomizer: Need at least 5 spawn tiles!");
            return;
        }

        // Shuffle a copy of the tile list
        List<string> shuffled = new List<string>(spawnTiles);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        // Assign one unique tile to each character
        AssignSpawn(captain,   shuffled[0]);
        AssignSpawn(scientist, shuffled[1]);
        AssignSpawn(engineer,  shuffled[2]);
        AssignSpawn(soldier,   shuffled[3]);
        AssignRobotSpawn(robot, shuffled[4]);

        Debug.Log($"Spawns assigned — Captain:{shuffled[0]} Scientist:{shuffled[1]} Engineer:{shuffled[2]} Soldier:{shuffled[3]} Robot:{shuffled[4]}");
    }

    private void AssignSpawn(GameObject character, string tile)
    {
        if (character == null) return;
        CharacterMovement cm = character.GetComponent<CharacterMovement>();
        if (cm != null)
            cm.spawnTile = tile;
    }

    private void AssignRobotSpawn(GameObject character, string tile)
    {
        if (character == null) return;
        Robot r = character.GetComponent<Robot>();
        if (r != null)
            r.spawnTile = tile;
    }
}