using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    public static Robot Instance;

    [Header("Settings")]
    public string spawnTile;
    public float moveSpeed = 5f;
    public int maxSteps = 6;
    public int hackCost = 3;

    private string currentTile;
    private bool isHacked = false;
    private bool missNextTurn = false;
    private bool isMoving = false;
    private float lockedZ;

    private bool isFeared = false;
    private bool isGreaterFeared = false;

    private void Awake() => Instance = this;

    private void Start()
    {
        lockedZ = transform.position.z;
        currentTile = spawnTile;

        GameObject spawnObj = GameObject.Find(spawnTile);
        if (spawnObj != null)
        {
            Renderer r = spawnObj.GetComponent<Renderer>();
            Vector3 pos = r != null ? r.bounds.center : spawnObj.transform.position;
            transform.position = new Vector3(pos.x, pos.y, lockedZ);
        }
    }

    // --- Fear ---
    public void ApplyFear()
    {
        isFeared = true;
        CrewIconManager.Instance.SetRobotFeared();
        Debug.Log("Robot is feared! Steps reduced next move.");
    }

    public void ApplyGreaterFear()
    {
        isFeared = true;
        isGreaterFeared = true;
        CrewIconManager.Instance.SetRobotFeared();
        Debug.Log("Robot is GREATER FEARED! Steps reduced to 1.");
    }

    public bool TryHack()
    {
        if (isHacked)
        {
            Debug.Log("Robot already hacked.");
            return false;
        }

        if (PlayerActionManager.Instance.GetCurrentEnergy() >= hackCost)
        {
            PlayerActionManager.Instance.SpendEnergy(hackCost);
            CrewIconManager.Instance.SetRobotHacked();
            isHacked = true;
            missNextTurn = true;
            Debug.Log("Robot hacked! Skipping next turn to allow crew to reposition.");
            UIEventsListener.OnRobotHacked?.Invoke();
            return true;
        }

        Debug.Log("Not enough energy to hack robot.");
        return false;
    }

    public IEnumerator TakeTurn()
    {
        if (isMoving) yield break;

        if (missNextTurn)
        {
            missNextTurn = false;
            Debug.Log("Robot is rebooting after hack — skipping this turn.");
            yield break;
        }

        // Resolve steps allowed this turn
        int stepsAllowed = isGreaterFeared ? 1
                         : isFeared ? maxSteps / 2
                         : maxSteps;

        // Clear fear flags after reading them
        isFeared = false;
        isGreaterFeared = false;

        // Revert icon only if not hacked (hacked icon is permanent)
        if (!isHacked)
            CrewIconManager.Instance.SetRobotNormal();

        isMoving = true;
        if (AudioManager.Instance != null)
    AudioManager.Instance.PlayRobotFootstep();

        if (isHacked)
        {
            CharacterMovement target = GetNearestCrew();
            if (target != null)
            {
                List<TileData> path = Pathfinder.Instance.FindPath(currentTile, target.GetCurrentTile());
                if (path != null && path.Count > 1)
                {
                    int steps = Mathf.Min(stepsAllowed, path.Count - 1);
                    for (int i = 1; i <= steps; i++)
                    {
                        yield return StartCoroutine(MoveToTile(path[i]));
                        CharacterMovement crew = GetCrewOnTile(currentTile);
                        if (crew != null)
                        {
                            Debug.Log($"Hacked Robot killed {crew.gameObject.tag}!");
                            GameManager.Instance.RemoveCrewMember(crew);
                            UIEventsListener.OnCharacterDeath?.Invoke(crew.gameObject.tag, "Hack");
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            string dest = AIBrain.Instance.GetBestDestination(currentTile, false);
            if (!string.IsNullOrEmpty(dest))
            {
                List<TileData> path = Pathfinder.Instance.FindPath(currentTile, dest, FireSpread.Instance.GetBurningTiles(), false);
                if (path != null && path.Count > 1)
                {
                    int steps = Mathf.Min(stepsAllowed, path.Count - 1);
                    for (int i = 1; i <= steps; i++)
                        yield return StartCoroutine(MoveToTile(path[i]));
                }
            }
        }

        StopMoving();
    }
    private void StopMoving()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopLoop();
        isMoving = false;
    }

    private IEnumerator MoveToTile(TileData tile)
    {
        Vector3 startPos = transform.position;
        GameObject tileObj = GameObject.Find(tile.tileName);
        Renderer r = tileObj?.GetComponent<Renderer>();
        Vector3 endPos = r != null ? new Vector3(r.bounds.center.x, r.bounds.center.y, lockedZ)
                                   : new Vector3(tileObj.transform.position.x, tileObj.transform.position.y, lockedZ);

        float duration = 1f / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        currentTile = tile.tileName;

        if (CapPointManager.Instance.IsCapPoint(currentTile))
        {
            CapPointManager.Instance.DestroyCapPoint(currentTile);
            Debug.Log($"Robot destroyed cap point at {currentTile}!");
        }

        if (currentTile == GameManager.Instance.GetMainSwitchTile())
            GameManager.Instance.CrewWins("Robot");

        Debug.Log($"Robot moved to: {currentTile}");
    }

    private CharacterMovement GetNearestCrew()
    {
        CharacterMovement nearest = null;
        int shortest = int.MaxValue;
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
        {
            List<TileData> path = Pathfinder.Instance.FindPath(currentTile, cm.GetCurrentTile());
            if (path != null && path.Count < shortest)
            {
                shortest = path.Count;
                nearest = cm;
            }
        }
        return nearest;
    }

    private CharacterMovement GetCrewOnTile(string tile)
    {
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
            if (cm.GetCurrentTile() == tile) return cm;
        return null;
    }

    public bool IsHacked() => isHacked;
    public string GetCurrentTile() => currentTile;
}