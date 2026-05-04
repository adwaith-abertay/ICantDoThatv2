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
        AirlockManager.Instance.RefreshButtons();
    }

    public int PredictNextTurnEnergy()
    {
        int gained = CapPointManager.Instance.GetActiveCount();
        if (GameManager.Instance.IsMainSwitchActive()) gained += 1;
        int predicted = Mathf.Min(currentEnergy + gained, 15);
        return predicted;
    }

    public void ActionFear(string tag)
    {
        if (currentEnergy < 1 || fearedThisTurn.Contains(tag)) return;

        // Robot has its own component — handle separately
        if (tag == "Robot")
        {
            if (Robot.Instance != null) Robot.Instance.ApplyFear();
            UIEventsListener.OnFrightened?.Invoke("Robot");
        }
        else
        {
            CharacterMovement.ApplyFearToTag(tag);
            UIEventsListener.OnFrightened?.Invoke(tag);
        }

        fearedThisTurn.Add(tag);
        currentEnergy -= 1;
        Debug.Log($"Fear applied to {tag}. Energy left: {currentEnergy}");

        CheckEnergyDrained();
    }

    public void ActionGreaterFear(string tag)
    {
        if (currentEnergy < 3 || fearedThisTurn.Contains(tag)) return;

        // Robot has its own component — handle separately
        if (tag == "Robot")
        {
            if (Robot.Instance != null) Robot.Instance.ApplyGreaterFear();
            UIEventsListener.OnTerrified?.Invoke("Robot");
        }
        else
        {
            GameObject obj = GameObject.FindWithTag(tag);
            if (obj != null)
            {
                CharacterMovement cm = obj.GetComponent<CharacterMovement>();
                if (cm != null) cm.ApplyGreaterFear();
                UIEventsListener.OnTerrified?.Invoke(tag);
            }
        }

        fearedThisTurn.Add(tag);
        currentEnergy -= 3;
        Debug.Log($"Greater Fear applied to {tag}. Energy left: {currentEnergy}");

     
        CheckEnergyDrained();
    }

    public void ActionStartFire()
    {
        if (currentEnergy < 4) return;

        FireSpread.Instance.StartFire();
        currentEnergy -= 4;
        Debug.Log($"Fire started! Energy left: {currentEnergy}");

        CheckEnergyDrained();
    }


    public void ActionCutO2()
    {
        if (currentEnergy < 8 || GameManager.Instance.IsO2Triggered()) return;

        GameManager.Instance.TriggerO2();
        currentEnergy -= 8;
        Debug.Log($"O2 cut! Energy left: {currentEnergy}");


        CheckEnergyDrained();
    }

    public void ActionHackRobot()
    {
        if (Robot.Instance == null) return;
        Robot.Instance.TryHack();
    
    }

    public void ActionCloseDoor()
    {
        DoorManager.Instance.EnterDoorMode();

    }


    public void ActionTriggerAirlock(string tileName)
    {
        AirlockManager.Instance.TryTriggerAirlock(tileName);

    }

    public void ActionReleaseAlien()
    {
        if (currentEnergy < 5) return;
        GameManager.Instance.ReleaseAlien();
        currentEnergy -= 5;
        Debug.Log($"Alien released! Energy left: {currentEnergy}");

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
  
        AirlockManager.Instance.RefreshButtons();
    }

    // Legacy getter kept so nothing else breaks
    public int GetEnergy() => currentEnergy;
    public string GetO2Tile() => o2Tile;
}
