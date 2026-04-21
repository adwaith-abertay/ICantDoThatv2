using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AirlockData
{
    public string airlockName;
    public List<string> tiles;
    public Button airlockButton;
    public int cost = 4;
}

public class AirlockManager : MonoBehaviour
{
    public static AirlockManager Instance;

    [Header("Airlocks")]
    public List<AirlockData> airlocks = new List<AirlockData>();

    [HideInInspector]
    public int airlockCost = 4;

    private void Awake() => Instance = this;

    private void Start()
    {
        foreach (AirlockData airlock in airlocks)
        {
            AirlockData captured = airlock;
            if (captured.airlockButton != null)
                captured.airlockButton.onClick.AddListener(() => TryTriggerAirlock(captured));
        }

        StartCoroutine(InitButtons());
    }

    private IEnumerator InitButtons()
    {
        yield return null;
        yield return null;
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        int energy = PlayerActionManager.Instance != null
            ? PlayerActionManager.Instance.GetCurrentEnergy()
            : 0;

        foreach (AirlockData airlock in airlocks)
        {
            if (airlock.airlockButton != null)
                airlock.airlockButton.interactable = energy >= airlock.cost;
        }
    }

    public bool TryTriggerAirlock(AirlockData airlock)
    {
        if (GameManager.Instance.currentPhase != GameManager.TurnPhase.PlayerTurn)
        {
            Debug.Log("Can't use airlock during crew turn!");
            return false;
        }

        if (PlayerActionManager.Instance.GetCurrentEnergy() < airlock.cost)
        {
            Debug.Log($"Not enough energy to trigger {airlock.airlockName}.");
            return false;
        }

        PlayerActionManager.Instance.SpendEnergy(airlock.cost);
        Debug.Log($"{airlock.airlockName} triggered! Flushing tiles: {string.Join(", ", airlock.tiles)}");

        // --- Check crew members ---
        List<CharacterMovement> crewSnapshot = new List<CharacterMovement>(GameManager.Instance.GetCrewMembers());
        List<CharacterMovement> toKill = new List<CharacterMovement>();

        foreach (CharacterMovement cm in crewSnapshot)
        {
            if (cm == null || cm.gameObject == null) continue;

            string tile = cm.GetCurrentTile();
            Debug.Log($"Checking {cm.gameObject.tag} on tile '{tile}'");

            if (airlock.tiles.Contains(tile))
            {
                if (cm.gameObject.tag == "Soldier" && cm.UseExtraLife())
                {
                    Debug.Log("Soldier survived the airlock with their extra life!");
                    continue;
                }
                toKill.Add(cm);
            }
        }

        foreach (CharacterMovement cm in toKill)
        {
            if (cm == null || cm.gameObject == null) continue;
            Debug.Log($"KILLING: {cm.gameObject.tag} flushed out of {airlock.airlockName}!");
            GameManager.Instance.RemoveCrewMember(cm);
        }

        // --- Check Robot ---
        if (Robot.Instance != null && Robot.Instance.gameObject.activeInHierarchy)
        {
            string robotTile = Robot.Instance.GetCurrentTile();
            Debug.Log($"Checking Robot on tile '{robotTile}'");

            if (airlock.tiles.Contains(robotTile))
            {
                Debug.Log($"Robot flushed out of {airlock.airlockName}!");
                Destroy(Robot.Instance.gameObject);
            }
        }

        // --- Check Alien ---
        if (Alien.Instance != null && Alien.Instance.IsReleased()
            && airlock.tiles.Contains(Alien.Instance.GetCurrentTile()))
        {
            Debug.Log("Alien flushed out of the airlock!");
            Alien.Instance.gameObject.SetActive(false);
        }

        PlayerActionUI.Instance.RefreshButtons();
        RefreshButtons();
        return true;
    }

    public bool TryTriggerAirlock(string tileName)
    {
        foreach (AirlockData airlock in airlocks)
        {
            if (airlock.tiles.Contains(tileName))
                return TryTriggerAirlock(airlock);
        }
        Debug.Log($"No airlock found covering tile {tileName}.");
        return false;
    }
}