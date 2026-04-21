using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum TurnPhase { PlayerTurn, O2Phase, FirePhase, AlienPhase, CrewTurn }
    public TurnPhase currentPhase = TurnPhase.PlayerTurn;

    [Header("Win/Lose")]
    public string mainSwitchTile;

    [Header("Alien")]
    public string alienSpawnTile;

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
        // --- O2 Phase ---
        currentPhase = TurnPhase.O2Phase;
        if (o2Triggered)
        {
            o2TurnsRemaining--;
            Debug.Log($"--- O2 Phase | {o2TurnsRemaining} turns left ---");

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

        // --- Fire Phase ---
        currentPhase = TurnPhase.FirePhase;
        Debug.Log("--- Fire Phase ---");
        FireSpread.Instance.SpreadFireTurn();
        yield return new WaitForSeconds(0.5f);
        CheckFireDeaths();

        // --- Alien Phase ---
        currentPhase = TurnPhase.AlienPhase;
        if (Alien.Instance.IsReleased())
        {
            Debug.Log("--- Alien Phase ---");
            yield return StartCoroutine(Alien.Instance.TakeTurn());
        }

        // --- Crew Turn ---
        currentPhase = TurnPhase.CrewTurn;
        Debug.Log("--- Crew Turn ---");
        yield return StartCoroutine(AIBrain.Instance.RunCrewActions());

        // Robot takes its turn after crew      

        // Generate energy for next player turn
        PlayerActionManager.Instance.GenerateEnergy();
        DoorManager.Instance.UnlockAllDoors();

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
        Debug.Log("O2 restored!");
        PlayerActionUI.Instance.RefreshButtons();
    }

    public void ReleaseAlien()
    {
        Alien.Instance.Release(alienSpawnTile);
    }

    public void RemoveCrewMember(CharacterMovement crew)
    {
        if (crew == null) return;

        crewMembers.Remove(crew);

        if (crew.gameObject != null)
            Destroy(crew.gameObject);

        crewMembers.RemoveAll(c => c == null || c.gameObject == null);

        Debug.Log($"Crew member eliminated! {crewMembers.Count} remaining.");

        if (crewMembers.Count == 0)
            AIWins("All crew eliminated!");
    }

    public void CrewWins(string role)
    {
        Debug.Log($"CREW WINS! {role} reached the main switch!");
        SceneManager.LoadScene("CrewWinScene");
    }

    public void AIWins(string reason)
    {
        Debug.Log($"AI WINS! {reason}");
        SceneManager.LoadScene("AIWinScene");
    }

    private void CheckFireDeaths()
    {
        List<CharacterMovement> snapshot = new List<CharacterMovement>(crewMembers);

        foreach (CharacterMovement crew in snapshot)
        {
            if (crew == null || crew.gameObject == null) continue;

            if (FireSpread.Instance.IsTileOnFire(crew.GetCurrentTile()))
            {
                Debug.Log($"{crew.gameObject.tag} burned to death!");
                RemoveCrewMember(crew);
            }
        }

        // Also check Robot for fire death
        if (Robot.Instance != null && Robot.Instance.gameObject.activeInHierarchy)
        {
            if (FireSpread.Instance.IsTileOnFire(Robot.Instance.GetCurrentTile()))
            {
                Debug.Log("Robot burned to death!");
                Destroy(Robot.Instance.gameObject);
            }
        }
    }

    public bool IsO2Triggered() => o2Triggered;
    public int GetO2Turns() => o2TurnsRemaining;
    public string GetMainSwitchTile() => mainSwitchTile;

    public List<CharacterMovement> GetCrewMembers()
    {
        crewMembers.RemoveAll(c => c == null || c.gameObject == null);
        return new List<CharacterMovement>(crewMembers);
    }
}