using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum TurnPhase { PlayerTurn, CrewTurn, FireSpread, O2Spread }
    public TurnPhase currentPhase = TurnPhase.PlayerTurn;

    [Header("Win/Lose")]
    public string mainSwitchTile;

    private List<CharacterMovement> crewMembers = new List<CharacterMovement>();
    private int o2TurnsRemaining = -1;
    private bool o2Triggered = false;

    private void Awake() => Instance = this;

    private void Start()
    {
        foreach (string tag in new string[] { "Captain", "Scientist", "Engineer", "Soldier" })
        {
            GameObject obj = GameObject.FindWithTag(tag);
            if (obj != null)
                crewMembers.Add(obj.GetComponent<CharacterMovement>());
        }
    }

    public void EndPlayerTurn()
    {
        if (currentPhase != TurnPhase.PlayerTurn) return;
        StartCoroutine(RunTurnCycle());
    }

    private IEnumerator RunTurnCycle()
    {
        // Crew Turn
        currentPhase = TurnPhase.CrewTurn;
        Debug.Log("--- Crew Turn ---");
        yield return StartCoroutine(AIBrain.Instance.RunCrewActions());

        // Fire Spread
        currentPhase = TurnPhase.FireSpread;
        Debug.Log("--- Fire Spread ---");
        FireSpread.Instance.SpreadFireTurn();
        yield return new WaitForSeconds(0.5f);
        CheckFireDeaths();

        // O2 Countdown
        if (o2Triggered)
        {
            currentPhase = TurnPhase.O2Spread;
            o2TurnsRemaining--;
            Debug.Log($"--- O2 Countdown | Turns remaining: {o2TurnsRemaining} ---");

            // At turn 2 — no oxygen left, fire cannot burn
            if (o2TurnsRemaining == 2)
            {
                FireSpread.Instance.ExtinguishAllFire();
                Debug.Log("O2 critically low — all fire extinguished!");
            }

            if (o2TurnsRemaining <= 0)
            {
                AIWins("O2 ran out!");
                yield break;
            }
        }

        // Generate energy for next player turn
        PlayerActionManager.Instance.GenerateEnergy();

        // Back to player turn
        currentPhase = TurnPhase.PlayerTurn;
        Debug.Log("--- Player Turn ---");
    }

    public void TriggerO2()
    {
        if (!o2Triggered)
        {
            o2Triggered = true;
            o2TurnsRemaining = 5;
            Debug.Log("O2 CUT! Crew has 5 turns!");
        }
    }

    public void ResetO2()
    {
        o2Triggered = false;
        o2TurnsRemaining = -1;
        Debug.Log("O2 restored! Timer cleared. Fire continues if still active.");
        PlayerActionUI.Instance.RefreshButtons();
    }

    public void RemoveCrewMember(CharacterMovement crew)
    {
        crewMembers.Remove(crew);
        Destroy(crew.gameObject);
        Debug.Log($"Crew member eliminated! {crewMembers.Count} remaining.");

        if (crewMembers.Count == 0)
            AIWins("All crew eliminated!");
    }

    public void CrewWins(string role)
    {
        Debug.Log($"CREW WINS! {role} reached the main switch!");
        UIManager.Instance.ShowCrewWin(role);
        Time.timeScale = 0;
    }

    public void AIWins(string reason)
    {
        Debug.Log($"AI WINS! {reason}");
        UIManager.Instance.ShowAIWin(reason);
        Time.timeScale = 0;
    }

    private void CheckFireDeaths()
    {
        List<CharacterMovement> toRemove = new List<CharacterMovement>();

        foreach (CharacterMovement crew in crewMembers)
        {
            if (FireSpread.Instance.IsTileOnFire(crew.GetCurrentTile()))
            {
                Debug.Log($"{crew.gameObject.tag} burned to death!");
                toRemove.Add(crew);
            }
        }

        foreach (CharacterMovement crew in toRemove)
            RemoveCrewMember(crew);
    }

    public bool IsO2Triggered() => o2Triggered;
    public int GetO2Turns() => o2TurnsRemaining;
    public string GetMainSwitchTile() => mainSwitchTile;
    public List<CharacterMovement> GetCrewMembers() => crewMembers;
}
