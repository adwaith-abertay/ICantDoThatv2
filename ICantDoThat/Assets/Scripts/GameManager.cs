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
    private bool gameOver = false;
    private bool mainSwitchActive = true;
    public bool IsMainSwitchActive() => mainSwitchActive;

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
        if (gameOver) return;
        StartCoroutine(RunTurnCycle());
    }

    public void DisableMainSwitch()
    {
        mainSwitchActive = false;
        Debug.Log("Main switch disabled!");
        CrewWins("Main switch turned off!");
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
                // O2 ran out — player (AI of the ship) wins
                PlayerWins("O2 ran out!");
                yield break;
            }
        }

        // --- Fire Phase ---
        currentPhase = TurnPhase.FirePhase;
        Debug.Log("--- Fire Phase ---");
        FireSpread.Instance.SpreadFireTurn();
        yield return new WaitForSeconds(0.5f);
        CheckFireDeaths();
        if (gameOver) yield break;

        // --- Alien Phase ---
        currentPhase = TurnPhase.AlienPhase;
        if (Alien.Instance.IsReleased())
        {
            Debug.Log("--- Alien Phase ---");
            yield return StartCoroutine(Alien.Instance.TakeTurn());
            if (gameOver) yield break;
        }

        // --- Crew Turn ---
        currentPhase = TurnPhase.CrewTurn;
        Debug.Log("--- Crew Turn ---");
        yield return StartCoroutine(AIBrain.Instance.RunCrewActions());
        if (gameOver) yield break;

        // Robot takes its turn after crew
        if (Robot.Instance != null && Robot.Instance.gameObject.activeInHierarchy)
            yield return StartCoroutine(Robot.Instance.TakeTurn());
        if (gameOver) yield break;

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

        // All crew dead — player (AI of the ship) wins
        if (crewMembers.Count == 0)
            PlayerWins("All crew eliminated!");
    }

    // Crew reached main switch OR all cap points destroyed — crew wins
    public void CrewWins(string role)
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log($"CREW WINS! {role}");
        SceneManager.LoadScene("CrewWinScene");
    }

    // All crew dead or O2 out — you (the player) win
    public void PlayerWins(string reason)
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log($"YOU WIN! {reason}");
        SceneManager.LoadScene("AIWinScene");
    }

    private void CheckFireDeaths()
    {
        List<CharacterMovement> snapshot = new List<CharacterMovement>(crewMembers);

        foreach (CharacterMovement crew in snapshot)
        {
            if (crew == null || crew.gameObject == null) continue;

            if (FireSpread.Instance.IsTileOnFire(crew.GetCurrentTile())
                && FireSpread.Instance.ShouldTakeFirDamage(crew.gameObject))
            {
                Debug.Log($"{crew.gameObject.tag} burned to death!");
                RemoveCrewMember(crew);
                if (gameOver) return;
            }
        }

        // Robot is immune — skip fire death check for it entirely
    }

    public bool IsO2Triggered() => o2Triggered;
    public int GetO2Turns() => o2TurnsRemaining;
    public string GetMainSwitchTile() => mainSwitchTile;
    public bool IsGameOver() => gameOver;

    public List<CharacterMovement> GetCrewMembers()
    {
        crewMembers.RemoveAll(c => c == null || c.gameObject == null);
        return new List<CharacterMovement>(crewMembers);
    }
}