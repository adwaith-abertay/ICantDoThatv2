using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public enum TestableEvent
{
    Frightened,
    Terrified,
    DoorShut,
    FireIgnited,
    AirlockOpened,
    RobotHacked,
    AlienReleased,
    OxygenDeactivated,
    CapDisabled,
    AxePickedUp,
    ExtinguisherPickedUp,
    TurnStarted,
    CharacterDeath
}

public enum CharacterType
{
    None,
    Random,
    Captain,
    Engineer,
    Scientist,
    Soldier,
    Injured,
    Robot,
    Alien
}

public enum DeathCauseType
{
    None,
    Airlock,
    Fire,
    Hack,
    Alien
}

[Serializable]
public class EventTestBinding
{
    public string note = "Test Event";

    [Space(5)]
    public Key triggerKey;
    public TestableEvent eventToFire;

    [Header("Parameters")]
    [Tooltip("Who is doing the action, getting scared, or dying?")]
    public CharacterType characterName = CharacterType.Captain;

    [Tooltip("ONLY USED FOR DEATH: How did they die?")]
    public DeathCauseType deathCause = DeathCauseType.Alien;

    [Tooltip("ONLY USED FOR DEATH: Who is saying the voice line about it?")]
    public CharacterType reactingCharacter = CharacterType.Random;
}

public class EventTester : MonoBehaviour
{
    [Header("Key Bindings")]
    [Tooltip("Click + to add a new test button mapping.")]
    public List<EventTestBinding> testBindings = new List<EventTestBinding>();

    private void Update()
    {
        if (Keyboard.current == null) return;

        foreach (var binding in testBindings)
        {
            if (Keyboard.current[binding.triggerKey].wasPressedThisFrame)
            {
                FireEvent(binding);
            }
        }
    }

    private void FireEvent(EventTestBinding binding)
    {
        string charStr = binding.characterName.ToString();
        string causeStr = binding.deathCause.ToString();
        string reactorStr = binding.reactingCharacter.ToString();

        string[] possibleCrew = { "Captain", "Engineer", "Scientist", "Soldier" };

        // --- THE FIX ---
        // If the main character is set to Random, pick a real crew member!
        if (binding.characterName == CharacterType.Random)
        {
            charStr = possibleCrew[UnityEngine.Random.Range(0, possibleCrew.Length)];
        }

        // If the reactor is set to Random, pick a real crew member!
        if (binding.reactingCharacter == CharacterType.Random)
        {
            reactorStr = possibleCrew[UnityEngine.Random.Range(0, possibleCrew.Length)];
        }

        Debug.Log($"[EventTester] Firing {binding.eventToFire} using {charStr}. Reactor: {reactorStr}");

        switch (binding.eventToFire)
        {
            case TestableEvent.Frightened:
                UIEventsListener.OnFrightened?.Invoke(charStr); break;

            case TestableEvent.Terrified:
                UIEventsListener.OnTerrified?.Invoke(charStr); break;

            case TestableEvent.DoorShut:
                UIEventsListener.OnDoorShut?.Invoke(charStr); break;

            case TestableEvent.FireIgnited:
                UIEventsListener.OnFireIgnited?.Invoke(charStr); break;

            case TestableEvent.AirlockOpened:
                UIEventsListener.OnAirlockOpened?.Invoke(charStr); break;

            case TestableEvent.RobotHacked:
                UIEventsListener.OnRobotHacked?.Invoke("Robot"); break;

            case TestableEvent.AlienReleased:
                UIEventsListener.OnAlienReleased?.Invoke(charStr); break;

            case TestableEvent.OxygenDeactivated:
                UIEventsListener.OnOxygenDeactivated?.Invoke(charStr); break;

            case TestableEvent.CapDisabled:
                UIEventsListener.OnCapDisabled?.Invoke(charStr); break;

            case TestableEvent.AxePickedUp:
                UIEventsListener.OnAxePickedUp?.Invoke(charStr); break;

            case TestableEvent.ExtinguisherPickedUp:
                UIEventsListener.OnExtinguisherPickedUp?.Invoke(charStr); break;

            case TestableEvent.TurnStarted:
                UIEventsListener.OnTurnStarted?.Invoke(charStr); break;

            case TestableEvent.CharacterDeath:
                UIEventsListener.OnCharacterDeath?.Invoke(charStr, causeStr, reactorStr);
                break;
        }
    }
}