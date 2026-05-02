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

    [Header("Robot Blocked Spawn Tiles")]
    public List<string> robotBlockedTiles = new List<string> { "L18", "A30" };

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

        // If shuffled[4] is blocked for robot, swap it with the first
        // non-blocked tile that isn't already used by crew (indices 0-3)
        if (robotBlockedTiles.Contains(shuffled[4]))
        {
            for (int i = 0; i < 4; i++)
            {
                if (!robotBlockedTiles.Contains(shuffled[i]))
                {
                    // Swap robot's tile with this crew tile
                    string temp = shuffled[4];
                    shuffled[4] = shuffled[i];
                    shuffled[i] = temp;
                    break;
                }
            }
        }

        AssignSpawn(captain,    shuffled[0]);
        AssignSpawn(scientist,  shuffled[1]);
        AssignSpawn(engineer,   shuffled[2]);
        AssignSpawn(soldier,    shuffled[3]);
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