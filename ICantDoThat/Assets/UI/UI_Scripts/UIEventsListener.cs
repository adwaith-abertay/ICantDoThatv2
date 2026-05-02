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
    // Tracks who is no longer allowed to speak
    private HashSet<string> deadCharacters = new HashSet<string>();

    // Tracks the last spoken line to prevent annoying repeats
    private string lastPlayedLineID = "";

    // ==========================================
    // 1. THE MASTER EVENTS
    // ==========================================

    // STANDARD EVENTS (Pass the character reacting, e.g., "Soldier")
    public static Action<string> OnFrightened;
    public static Action<string> OnDoorShut;
    public static Action<string> OnTerrified;
    public static Action<string> OnFireIgnited;
    public static Action<string> OnAirlockOpened;
    public static Action<string> OnRobotHacked;
    public static Action<string> OnAlienReleased;
    public static Action<string> OnOxygenDeactivated;
    public static Action<string> OnCapDisabled;
    public static Action<string> OnAxePickedUp;
    public static Action<string> OnExtinguisherPickedUp;
    public static Action<string> OnTurnStarted;

    // DEATH EVENT (Pass: Who Died, How They Died, Who is Reacting)
    public static Action<string, string, string> OnCharacterDeath;


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
        OnDoorShut += (charType) => ProcessTextEvent(reactDoorShut, charType);
        OnTerrified += (charType) => ProcessTextEvent(reactTerrified, charType);
        OnFireIgnited += (charType) => ProcessTextEvent(reactFireIgnited, charType);
        OnAirlockOpened += (charType) => ProcessTextEvent(reactAirlockOpened, charType);
        OnRobotHacked += (charType) => ProcessTextEvent(reactRobotHacked, charType);
        OnAlienReleased += (charType) => ProcessTextEvent(reactAlienReleased, charType);
        OnOxygenDeactivated += (charType) => ProcessTextEvent(reactOxygenDeactivated, charType);

        OnCapDisabled += (charType) => ProcessTextEvent(actionCapDisabled, charType);
        OnAxePickedUp += (charType) => ProcessTextEvent(actionAxePickedUp, charType);
        OnExtinguisherPickedUp += (charType) => ProcessTextEvent(actionExtinguisherPickedUp, charType);
        OnTurnStarted += (charType) => ProcessTextEvent(ambTurnStarted, charType);

        // The unified death router
        OnCharacterDeath += HandleDeathRouting;
    }

    private void OnDisable()
    {
        OnFrightened -= (charType) => ProcessTextEvent(reactFrightened, charType);
        OnDoorShut -= (charType) => ProcessTextEvent(reactDoorShut, charType);
        OnTerrified -= (charType) => ProcessTextEvent(reactTerrified, charType);
        OnFireIgnited -= (charType) => ProcessTextEvent(reactFireIgnited, charType);
        OnAirlockOpened -= (charType) => ProcessTextEvent(reactAirlockOpened, charType);
        OnRobotHacked -= (charType) => ProcessTextEvent(reactRobotHacked, charType);
        OnAlienReleased -= (charType) => ProcessTextEvent(reactAlienReleased, charType);
        OnOxygenDeactivated -= (charType) => ProcessTextEvent(reactOxygenDeactivated, charType);

        OnCapDisabled -= (charType) => ProcessTextEvent(actionCapDisabled, charType);
        OnAxePickedUp -= (charType) => ProcessTextEvent(actionAxePickedUp, charType);
        OnExtinguisherPickedUp -= (charType) => ProcessTextEvent(actionExtinguisherPickedUp, charType);
        OnTurnStarted -= (charType) => ProcessTextEvent(ambTurnStarted, charType);

        OnCharacterDeath -= HandleDeathRouting;
    }


    // ==========================================
    // 4. THE ROUTING LOGIC
    // ==========================================

    private void HandleDeathRouting(string deadCharacter, string deathCause, string reactingCharacter)
    {
        if (string.IsNullOrEmpty(deadCharacter) || deadCharacter == "None") return;

        string deadLower = deadCharacter.ToLower();

        // 1. SILENCE THEM permanently
        deadCharacters.Add(deadLower);

        // 2. TRIGGER THE COMIC
        if (DeathComicManager.Instance != null)
        {
            DeathComicManager.Instance.PlayComic(deadCharacter, deathCause);
        }

        // 3. TRIGGER THE TEXT REACTION
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
            ProcessTextEvent(textPool, reactingCharacter);
        }
    }

    private void ProcessTextEvent(EventDialoguePool pool, string speakingCharacter)
    {
        if (string.IsNullOrEmpty(speakingCharacter) || speakingCharacter == "None") return;

        string lowerCaseChar = speakingCharacter.ToLower();

        // If the person trying to talk is dead, do nothing!
        if (deadCharacters.Contains(lowerCaseChar)) return;

        // Roll the percentage chance
        if (UnityEngine.Random.Range(0f, 100f) > pool.triggerChance) return;

        string[] availableLines = lowerCaseChar switch
        {
            "captain" => pool.captainLines,
            "engineer" => pool.engineerLines,
            "scientist" => pool.scientistLines,
            "soldier" => pool.soldierLines,
            "injured" => pool.injuredSoldierLines,
            "robot" => pool.robotLines,
            _ => null
        };

        if (availableLines == null || availableLines.Length == 0) return;

        // --- THE ANTI-REPEAT LOGIC ---
        // Convert the array to a list so we can temporarily remove items
        List<string> validLines = new List<string>(availableLines);

        // If they have more than one line option, and our list contains the one we JUST played...
        if (validLines.Count > 1 && validLines.Contains(lastPlayedLineID))
        {
            // ... remove it from the raffle!
            validLines.Remove(lastPlayedLineID);
        }

        // Pick a random line from the remaining safe options
        string chosenID = validLines[UnityEngine.Random.Range(0, validLines.Count)];

        // Save this new line to our memory for next time
        lastPlayedLineID = chosenID;

        // Send it to the console!
        ConsoleDatabase.Instance.TriggerMessage(chosenID);
    }
}