using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorData
{
    public string tileA;
    public string tileB;
    public GameObject doorSprite;
}

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance;

    [Header("Settings")]
    public int doorCloseCost = 2;

    [Header("Doors")]
    public List<DoorData> doors = new List<DoorData>();

    private HashSet<string> lockedDoors = new HashSet<string>();
    private bool doorModeActive = false;

    private void Awake() => Instance = this;

    private void Start()
    {
        foreach (DoorData door in doors)
        {
            if (door.doorSprite != null)
            {
                door.doorSprite.SetActive(false);
                SetDoorColor(door, Color.green);
            }
        }
    }

    public void EnterDoorMode()
    {
        if (PlayerActionManager.Instance.GetCurrentEnergy() < doorCloseCost)
        {
            Debug.Log("Not enough energy to lock doors.");
            return;
        }

        doorModeActive = true;

        foreach (DoorData door in doors)
        {
            if (door.doorSprite != null)
            {
                door.doorSprite.SetActive(true);
                SetDoorColor(door, lockedDoors.Contains(door.doorSprite.name) ? Color.red : Color.green);
            }
        }

        Debug.Log("Door mode active — click a door to lock/unlock it.");
    }

    public void OnDoorClicked(string doorName)
    {
        if (!doorModeActive) return;

        DoorData door = GetDoor(doorName);
        if (door == null) return;

        if (lockedDoors.Contains(doorName))
        {
            lockedDoors.Remove(doorName);
            SetDoorColor(door, Color.green);
            RestorePassage(door);
            Debug.Log($"{doorName} unlocked.");
        }
        else
        {
            if (PlayerActionManager.Instance.GetCurrentEnergy() < doorCloseCost)
            {
                Debug.Log("Not enough energy to lock this door.");
                return;
            }

            PlayerActionManager.Instance.SpendEnergy(doorCloseCost);
            lockedDoors.Add(doorName);
            SetDoorColor(door, Color.red);
            BlockPassage(door);
            Debug.Log($"{doorName} locked — passage between {door.tileA} and {door.tileB} blocked.");
        }

        PlayerActionUI.Instance.RefreshButtons();
    }

    public void OnTurnEnd()
    {
        doorModeActive = false;

        foreach (DoorData door in doors)
        {
            if (door.doorSprite == null) continue;

            if (lockedDoors.Contains(door.doorSprite.name))
            {
                door.doorSprite.SetActive(true);
                SetDoorColor(door, Color.red);
            }
            else
            {
                door.doorSprite.SetActive(false);
            }
        }
    }

    private void BlockPassage(DoorData door)
    {
        TileData tileA = GridManager.Instance.GetTile(door.tileA);
        TileData tileB = GridManager.Instance.GetTile(door.tileB);

        if (tileA != null) tileA.blockedNeighbours.Add(door.tileB);
        if (tileB != null) tileB.blockedNeighbours.Add(door.tileA);
    }

    private void RestorePassage(DoorData door)
    {
        TileData tileA = GridManager.Instance.GetTile(door.tileA);
        TileData tileB = GridManager.Instance.GetTile(door.tileB);

        if (tileA != null) tileA.blockedNeighbours.Remove(door.tileB);
        if (tileB != null) tileB.blockedNeighbours.Remove(door.tileA);
    }

    public void UnlockAllDoors()
    {
        List<string> toUnlock = new List<string>(lockedDoors);
        foreach (string doorName in toUnlock)
        {
            DoorData door = GetDoor(doorName);
            if (door != null)
            {
                RestorePassage(door);
                door.doorSprite.SetActive(false);
            }
        }
        lockedDoors.Clear();
        doorModeActive = false;
        Debug.Log("All doors unlocked for next turn.");
    }

    private void SetDoorColor(DoorData door, Color color)
    {
        SpriteRenderer sr = door.doorSprite.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    private DoorData GetDoor(string doorName)
    {
        return doors.Find(d => d.doorSprite != null && d.doorSprite.name == doorName);
    }

    public bool IsDoorLocked(string doorName) => lockedDoors.Contains(doorName);
    public bool IsDoorModeActive() => doorModeActive;
}