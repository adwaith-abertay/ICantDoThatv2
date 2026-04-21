using System.Collections.Generic;
using UnityEngine;

public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance;

    [Header("O2 Settings")]
    public string o2Tile;

    [Header("Energy")]
    public int startingEnergy = 5;
    private int currentEnergy;

    private HashSet<string> fearedThisTurn = new HashSet<string>();

    private void Awake()
    {
        Instance = this;
        currentEnergy = startingEnergy;
        Debug.Log($"Player starts with {currentEnergy} energy.");
    }

    public void GenerateEnergy()
    {
        int gained = CapPointManager.Instance.GenerateEnergy();
        currentEnergy = Mathf.Min(currentEnergy + gained, 15);
        fearedThisTurn.Clear();
        Debug.Log($"Energy generated. Total: {currentEnergy}");
        PlayerActionUI.Instance.RefreshButtons();
        AirlockManager.Instance.RefreshButtons();
    }

    public void ActionFear(string tag)
    {
        if (currentEnergy < 1 || fearedThisTurn.Contains(tag)) return;

        CharacterMovement.ApplyFearToTag(tag);
        fearedThisTurn.Add(tag);
        currentEnergy -= 1;
        Debug.Log($"Fear applied to {tag}. Energy left: {currentEnergy}");

        PlayerActionUI.Instance.RefreshButtons();
        CheckEnergyDrained();
    }

    public void ActionGreaterFear(string tag)
    {
        if (currentEnergy < 3 || fearedThisTurn.Contains(tag)) return;

        // Reduce steps to 1 for this crew member
        GameObject obj = GameObject.FindWithTag(tag);
        if (obj != null)
        {
            CharacterMovement cm = obj.GetComponent<CharacterMovement>();
            if (cm != null) cm.ApplyGreaterFear();
        }

        fearedThisTurn.Add(tag);
        currentEnergy -= 3;
        Debug.Log($"Greater Fear applied to {tag}. Energy left: {currentEnergy}");

        PlayerActionUI.Instance.RefreshButtons();
        CheckEnergyDrained();
    }

    public void ActionStartFire()
    {
        if (currentEnergy < 10) return;

        FireSpread.Instance.StartFire();
        currentEnergy -= 10;
        Debug.Log($"Fire started! Energy left: {currentEnergy}");

        PlayerActionUI.Instance.RefreshButtons();
        CheckEnergyDrained();
    }


    public void ActionCutO2()
    {
        if (currentEnergy < 15 || GameManager.Instance.IsO2Triggered()) return;

        GameManager.Instance.TriggerO2();
        currentEnergy -= 15;
        Debug.Log($"O2 cut! Energy left: {currentEnergy}");

        PlayerActionUI.Instance.RefreshButtons();
        CheckEnergyDrained();
    }

    public void ActionHackRobot()
    {
        if (Robot.Instance == null) return;
        Robot.Instance.TryHack();
        PlayerActionUI.Instance.RefreshButtons();
    }

    public void ActionCloseDoor()
    {
        DoorManager.Instance.EnterDoorMode();
        PlayerActionUI.Instance.RefreshButtons();
    }


    public void ActionTriggerAirlock(string tileName)
    {
        AirlockManager.Instance.TryTriggerAirlock(tileName);
        PlayerActionUI.Instance.RefreshButtons();
    }

    public void ActionReleaseAlien()
    {
        if (currentEnergy < 5) return;
        GameManager.Instance.ReleaseAlien();
        currentEnergy -= 5;
        Debug.Log($"Alien released! Energy left: {currentEnergy}");
        PlayerActionUI.Instance.RefreshButtons();
        CheckEnergyDrained();
    }

    private void CheckEnergyDrained()
    {
        if (currentEnergy <= 0)
        {
            Debug.Log("Energy drained — auto ending player turn.");
            GameManager.Instance.EndPlayerTurn();
        }
    }

    private CharacterMovement GetCrewClosestToSwitch(List<CharacterMovement> crew)
    {
        CharacterMovement closest = null;
        int shortestPath = int.MaxValue;
        string switchTile = GameManager.Instance.GetMainSwitchTile();

        foreach (CharacterMovement cm in crew)
        {
            List<TileData> path = Pathfinder.Instance.FindPath(cm.GetCurrentTile(), switchTile);
            if (path != null && path.Count < shortestPath)
            {
                shortestPath = path.Count;
                closest = cm;
            }
        }
        return closest;
    }

    public bool IsAlreadyFeared(string tag) => fearedThisTurn.Contains(tag);

    // These are what DoorManager, AirlockManager and Robot.cs reference
    public int GetCurrentEnergy() => currentEnergy;
    public void SpendEnergy(int amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Max(0, currentEnergy);
        Debug.Log($"Energy spent: {amount} | Remaining: {currentEnergy}");
        PlayerActionUI.Instance.RefreshButtons();
        AirlockManager.Instance.RefreshButtons();
    }

    // Legacy getter kept so nothing else breaks
    public int GetEnergy() => currentEnergy;
    public string GetO2Tile() => o2Tile;
}
