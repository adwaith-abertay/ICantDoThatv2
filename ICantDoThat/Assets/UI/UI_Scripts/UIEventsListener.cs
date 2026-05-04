using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class EventDialoguePool
{
    [Range(0f, 100f)]
    public float triggerChance = 75f;
    [Space(5)]
    public string[] captainLines;
    public string[] engineerLines;
    public string[] scientistLines;
    public string[] soldierLines;
    public string[] injuredSoldierLines;
    public string[] robotLines;
}

public class UIEventsListener : MonoBehaviour
{
    private HashSet<string> deadCharacters = new HashSet<string>();
    private string lastPlayedLineID = "";

    // ==========================================
    // 1. THE MASTER EVENTS
    // ==========================================

    public static Action<string> OnFrightened;
    public static Action<string> OnTerrified;
    public static Action<string> OnCapDisabled;
    public static Action<string> OnAxePickedUp;
    public static Action<string> OnExtinguisherPickedUp;
    public static Action<string> OnTurnStarted;

    public static Action OnDoorShut;
    public static Action OnFireIgnited;
    public static Action OnAirlockOpened;
    public static Action OnRobotHacked;
    public static Action OnAlienReleased;
    public static Action OnOxygenDeactivated;

    public static Action<string, string> OnCharacterDeath;

    // ==========================================
    // 2. THE INSPECTOR MENUS
    // ==========================================

    [Header("--- GENERAL REACTIONS ---")]
    public EventDialoguePool reactFrightened;
    public EventDialoguePool reactDoorShut;
    public EventDialoguePool reactTerrified;
    public EventDialoguePool reactFireIgnited;
    public EventDialoguePool reactAirlockOpened;
    public EventDialoguePool reactRobotHacked;
    public EventDialoguePool reactAlienReleased;
    public EventDialoguePool reactOxygenDeactivated;

    [Header("--- DEATH TEXT REACTIONS ---")]
    public EventDialoguePool reactCaptainKilled;
    public EventDialoguePool reactEngineerKilled;
    public EventDialoguePool reactScientistKilled;
    public EventDialoguePool reactSoldierKilled;
    public EventDialoguePool reactRobotKilled;
    public EventDialoguePool reactAlienKilled;

    [Header("--- CREW ACTIONS ---")]
    public EventDialoguePool actionCapDisabled;
    public EventDialoguePool actionAxePickedUp;
    public EventDialoguePool actionExtinguisherPickedUp;

    [Header("--- AMBIENCE ---")]
    public EventDialoguePool ambTurnStarted;

    // ==========================================
    // 3. SUBSCRIPTIONS
    // ==========================================
    private void OnEnable()
    {
        OnFrightened += (charType) => ProcessTextEvent(reactFrightened, charType);
        OnTerrified += (charType) => ProcessTextEvent(reactTerrified, charType);
        OnCapDisabled += (charType) => ProcessTextEvent(actionCapDisabled, charType);
        OnAxePickedUp += (charType) => ProcessTextEvent(actionAxePickedUp, charType);
        OnExtinguisherPickedUp += (charType) => ProcessTextEvent(actionExtinguisherPickedUp, charType);
        OnTurnStarted += (charType) => ProcessTextEvent(ambTurnStarted, charType);

        // Send random events to the new smart handler
        OnDoorShut += () => ProcessRandomTextEvent(reactDoorShut);
        OnFireIgnited += () => ProcessRandomTextEvent(reactFireIgnited);
        OnAirlockOpened += () => ProcessRandomTextEvent(reactAirlockOpened);
        OnRobotHacked += () => ProcessRandomTextEvent(reactRobotHacked);
        OnAlienReleased += () => ProcessRandomTextEvent(reactAlienReleased);
        OnOxygenDeactivated += () => ProcessRandomTextEvent(reactOxygenDeactivated);

        OnCharacterDeath += HandleDeathRouting;
    }

    private void OnDisable()
    {
        OnFrightened -= (charType) => ProcessTextEvent(reactFrightened, charType);
        OnTerrified -= (charType) => ProcessTextEvent(reactTerrified, charType);
        OnCapDisabled -= (charType) => ProcessTextEvent(actionCapDisabled, charType);
        OnAxePickedUp -= (charType) => ProcessTextEvent(actionAxePickedUp, charType);
        OnExtinguisherPickedUp -= (charType) => ProcessTextEvent(actionExtinguisherPickedUp, charType);
        OnTurnStarted -= (charType) => ProcessTextEvent(ambTurnStarted, charType);

        OnDoorShut -= () => ProcessRandomTextEvent(reactDoorShut);
        OnFireIgnited -= () => ProcessRandomTextEvent(reactFireIgnited);
        OnAirlockOpened -= () => ProcessRandomTextEvent(reactAirlockOpened);
        OnRobotHacked -= () => ProcessRandomTextEvent(reactRobotHacked);
        OnAlienReleased -= () => ProcessRandomTextEvent(reactAlienReleased);
        OnOxygenDeactivated -= () => ProcessRandomTextEvent(reactOxygenDeactivated);

        OnCharacterDeath -= HandleDeathRouting;
    }

    // ==========================================
    // 4. THE ROUTING LOGIC
    // ==========================================

    private void HandleDeathRouting(string deadCharacter, string deathCause)
    {
        if (string.IsNullOrEmpty(deadCharacter) || deadCharacter == "None") return;

        string deadLower = deadCharacter.ToLower();
        deadCharacters.Add(deadLower);

        if (DeathComicManager.Instance != null)
        {
            DeathComicManager.Instance.PlayComic(deadCharacter, deathCause);
        }

        EventDialoguePool textPool = deadLower switch
        {
            "captain" => reactCaptainKilled,
            "engineer" => reactEngineerKilled,
            "scientist" => reactScientistKilled,
            "soldier" => reactSoldierKilled,
            "robot" => reactRobotKilled,
            "alien" => reactAlienKilled,
            _ => null
        };

        if (textPool != null)
        {
            // Use the smart random handler for death reactions too!
            ProcessRandomTextEvent(textPool);
        }
    }

    // --- NEW: Helper method to grab the array of lines ---
    private string[] GetAvailableLines(EventDialoguePool pool, string lowerCaseChar)
    {
        if (pool == null) return null;
        return lowerCaseChar switch
        {
            "captain" => pool.captainLines,
            "engineer" => pool.engineerLines,
            "scientist" => pool.scientistLines,
            "soldier" => pool.soldierLines,
            "injured" => pool.injuredSoldierLines,
            "robot" => pool.robotLines,
            _ => null
        };
    }

    // --- NEW: Smart Random Reactor Selection ---
    private void ProcessRandomTextEvent(EventDialoguePool pool)
    {
        if (pool == null || GameManager.Instance == null) return;

        var crew = GameManager.Instance.GetCrewMembers();
        if (crew == null || crew.Count == 0) return;

        List<string> validReactors = new List<string>();

        // 1. Loop through everyone who is currently alive
        foreach (var member in crew)
        {
            string tag = member.gameObject.tag;
            string lowerTag = tag.ToLower();

            // 2. Double check they aren't dead, and check if they actually have lines written for this!
            if (!deadCharacters.Contains(lowerTag))
            {
                string[] lines = GetAvailableLines(pool, lowerTag);
                if (lines != null && lines.Length > 0)
                {
                    validReactors.Add(tag);
                }
            }
        }

        // 3. If NO ONE has lines for this event, do nothing
        if (validReactors.Count == 0) return;

        // 4. Pick randomly from the people who actually have dialogue prepared
        string chosenReactor = validReactors[UnityEngine.Random.Range(0, validReactors.Count)];

        ProcessTextEvent(pool, chosenReactor);
    }

    private void ProcessTextEvent(EventDialoguePool pool, string speakingCharacter)
    {
        if (string.IsNullOrEmpty(speakingCharacter) || speakingCharacter == "None") return;

        string lowerCaseChar = speakingCharacter.ToLower();

        if (deadCharacters.Contains(lowerCaseChar)) return;

        if (UnityEngine.Random.Range(0f, 100f) > pool.triggerChance) return;

        string[] availableLines = GetAvailableLines(pool, lowerCaseChar);

        if (availableLines == null || availableLines.Length == 0) return;

        List<string> validLines = new List<string>(availableLines);

        if (validLines.Count > 1 && validLines.Contains(lastPlayedLineID))
        {
            validLines.Remove(lastPlayedLineID);
        }

        string chosenID = validLines[UnityEngine.Random.Range(0, validLines.Count)];
        lastPlayedLineID = chosenID;

        ConsoleDatabase.Instance.TriggerMessage(chosenID);
    }
}