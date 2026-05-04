using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements; // Changed from UnityEngine.UI

[System.Serializable]
public class AirlockData
{
    public string airlockName;
    public List<string> tiles;

    [Tooltip("The exact name of the button in UI Builder (e.g., Airlock1)")]
    public string buttonName;

    public int cost = 4;

    // We keep a reference to the UI Toolkit button so we can enable/disable it later
    [HideInInspector]
    public Button uieButton;
}

public class AirlockManager : MonoBehaviour
{
    public static AirlockManager Instance;

    [Header("UI Toolkit Reference")]
    public UIDocument uiDocument; // Drag your HUD UI Document here

    [Header("Airlocks")]
    public List<AirlockData> airlocks = new List<AirlockData>();

    [Header("Pod GameObjects (one per airlock, same index order)")]
    public List<GameObject> podObjects = new List<GameObject>();

    [HideInInspector]
    public int airlockCost = 4;

    private HashSet<int> airlockWithPod = new HashSet<int>();
    private Dictionary<CharacterMovement, int> crewInSpace = new Dictionary<CharacterMovement, int>();

    private void Awake() => Instance = this;

    private void Start()
    {
        // Bind the UI Toolkit buttons
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            foreach (AirlockData airlock in airlocks)
            {
                if (!string.IsNullOrEmpty(airlock.buttonName))
                {
                    // Find the button by its string name from the UI Builder
                    airlock.uieButton = root.Q<Button>(airlock.buttonName);

                    if (airlock.uieButton != null)
                    {
                        // Capture the variable for the lambda expression
                        AirlockData captured = airlock;
                        airlock.uieButton.clicked += () => TryTriggerAirlock(captured);
                    }
                    else
                    {
                        Debug.LogWarning($"AirlockManager: Could not find button '{airlock.buttonName}' in UI Document.");
                    }
                }
            }
        }

        // All pods off by default
        foreach (GameObject pod in podObjects)
            if (pod != null) pod.SetActive(false);

        StartCoroutine(InitButtons());
        RandomlyParkPods();
    }

    private void RandomlyParkPods()
    {
        if (airlocks.Count < 2)
        {
            Debug.LogWarning("Need at least 2 airlocks to park pods!");
            return;
        }

        List<int> indices = new List<int>();
        for (int i = 0; i < airlocks.Count; i++) indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
        }

        ParkPodAt(indices[0]);
        ParkPodAt(indices[1]);

        Debug.Log($"Pods parked at: {airlocks[indices[0]].airlockName} and {airlocks[indices[1]].airlockName}");
    }

    // Enable the pod visual at this airlock index
    private void ParkPodAt(int index)
    {
        airlockWithPod.Add(index);
        if (index < podObjects.Count && podObjects[index] != null)
            podObjects[index].SetActive(true);
    }

    // Disable the pod visual at this airlock index
    private void RemovePodFrom(int index)
    {
        airlockWithPod.Remove(index);
        if (index < podObjects.Count && podObjects[index] != null)
            podObjects[index].SetActive(false);
    }

    public void OnCrewSteppedOnTile(CharacterMovement crew, string tileName)
    {
        int airlockIndex = GetAirlockIndex(tileName);
        if (airlockIndex < 0) return;                        // not an airlock tile
        if (!airlockWithPod.Contains(airlockIndex)) return;  // no pod parked here
        if (crewInSpace.ContainsKey(crew)) return;           // already in space

        // Predict next turn energy — if player can afford to flush, don't use pod
        int nextEnergy = PlayerActionManager.Instance.PredictNextTurnEnergy();
        if (nextEnergy >= 8)
        {
            Debug.Log($"{crew.gameObject.tag} skipped pod — player will have {nextEnergy} energy next turn, can flush!");
            return;
        }

        // Safe to use pod — player can't flush next turn
        crewInSpace[crew] = airlockIndex;
        RemovePodFrom(airlockIndex);

        crew.gameObject.SetActive(false);
        Debug.Log($"{crew.gameObject.tag} used pod safely — player will only have {nextEnergy} energy, can't flush.");
    }

    public void ReturnCrewFromSpace()
    {
        Debug.Log($"ReturnCrewFromSpace called — {crewInSpace.Count} crew in space");
        if (crewInSpace.Count == 0) return;

        List<CharacterMovement> toReturn = new List<CharacterMovement>(crewInSpace.Keys);

        foreach (CharacterMovement cm in toReturn)
        {
            int fromIndex = crewInSpace[cm];
            int destIndex = GetRandomAirlockWithoutPod(fromIndex);

            if (destIndex < 0)
            {
                Debug.LogWarning($"No valid airlock to return {cm.gameObject.tag} to!");
                continue;
            }

            AirlockData dest = airlocks[destIndex];
            string spawnTile = dest.tiles[Random.Range(0, dest.tiles.Count)];

            cm.TeleportToTile(spawnTile);
            cm.gameObject.SetActive(true);

            ParkPodAt(destIndex); // pod arrives at destination — show it
            crewInSpace.Remove(cm);

            Debug.Log($"{cm.gameObject.tag} returned from space → {spawnTile} ({dest.airlockName}), pod now parked there.");
        }
    }

    public bool IsCrewInSpace(string tag)
    {
        foreach (CharacterMovement cm in crewInSpace.Keys)
            if (cm.gameObject.CompareTag(tag)) return true;
        return false;
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
        AudioManager.Instance.PlayOpenAirlock();
        UIEventsListener.OnAirlockOpened?.Invoke();

        List<CharacterMovement> crewSnapshot = new List<CharacterMovement>(GameManager.Instance.GetCrewMembers());
        List<CharacterMovement> toKill = new List<CharacterMovement>();

        foreach (CharacterMovement cm in crewSnapshot)
        {
            if (cm == null || cm.gameObject == null) continue;

            string tile = cm.GetCurrentTile();
            Debug.Log($"Checking {cm.gameObject.tag} on tile '{tile}'");

            if (airlock.tiles.Contains(tile))
            {
                if (IsCrewInSpace(cm.gameObject.tag))
                {
                    Debug.Log($"{cm.gameObject.tag} is in space — safe from airlock flush!");
                    continue;
                }

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
            UIEventsListener.OnCharacterDeath?.Invoke(cm.gameObject.tag, "Airlock");
        }

        if (Robot.Instance != null && Robot.Instance.gameObject.activeInHierarchy)
        {
            string robotTile = Robot.Instance.GetCurrentTile();
            if (airlock.tiles.Contains(robotTile))
            {
                Debug.Log($"Robot flushed out of {airlock.airlockName}!");
                Destroy(Robot.Instance.gameObject);
            }
        }

        if (Alien.Instance != null && Alien.Instance.IsReleased()
            && airlock.tiles.Contains(Alien.Instance.GetCurrentTile()))
        {
            Debug.Log("Alien flushed out of the airlock!");
            Alien.Instance.gameObject.SetActive(false);
        }

        RefreshButtons();

        // --- NEW: Automatically close the airlock menu if it was open! ---
        if (SIIDUIManager.Instance != null)
        {
            SIIDUIManager.Instance.HideAirlockMenu();
        }

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
            if (airlock.uieButton != null)
            {
                // UI Toolkit uses SetEnabled() instead of .interactable
                airlock.uieButton.SetEnabled(energy >= airlock.cost);
            }
        }
    }

    private int GetAirlockIndex(string tileName)
    {
        for (int i = 0; i < airlocks.Count; i++)
            if (airlocks[i].tiles.Contains(tileName)) return i;
        return -1;
    }

    private int GetRandomAirlockWithoutPod(int excludeIndex)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < airlocks.Count; i++)
            if (i != excludeIndex && !airlockWithPod.Contains(i))
                candidates.Add(i);
        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
    }

    // Called by the flush pod button
    public void FlushCrewInSpace()
    {
        if (crewInSpace.Count == 0) return;

        List<CharacterMovement> toFlush = new List<CharacterMovement>(crewInSpace.Keys);
        foreach (CharacterMovement cm in toFlush)
        {
            Debug.Log($"{cm.gameObject.tag} flushed out of pod — killed in space!");
            crewInSpace.Remove(cm);
            GameManager.Instance.RemoveCrewMember(cm);
        }

        PlayerActionManager.Instance.SpendEnergy(8);
    }

    // Used by SIIDUIManager to check if buttons should be active
    public bool IsAnyoneInSpace() => crewInSpace.Count > 0;
}