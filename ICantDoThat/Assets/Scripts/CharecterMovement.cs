using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Tile Settings")]
    public string spawnTile;
    public string targetTile;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public int maxSteps = 6;

    [Header("Vent Settings")]
    public bool canTravelThroughVents = false;

    [Header("Role Settings")]
    public bool isScientist = false;
    public bool hasSoldierExtraLife = false;

    private string currentTile;
    public bool isMoving = false;
    public bool isFeared = false;
    public bool isGreaterFeared = false;
    private float lockedZ;
    private bool extraLifeUsed = false;
    private bool killedAlienThisStep = false;
    private int stepsUsedThisTurn = 0;
    private bool justReturnedFromSpace = false;

    private string capInProgress = "";
    private int capMovesRemaining = 0;

    private void Start()
    {
        lockedZ = transform.position.z;
        currentTile = spawnTile;

        GameObject spawnObj = GameObject.Find(spawnTile);
        if (spawnObj != null)
        {
            Vector3 center = GetTileCenter(spawnObj);
            transform.position = new Vector3(center.x, center.y, lockedZ);
        }

        TileStatusManager.Instance.UpdateTile(currentTile, gameObject);
        CollectibleManager.Instance.TryPickup(gameObject.tag, currentTile);
    }

    public void MoveToTarget()
    {
        if (!isMoving)
            StartCoroutine(MoveAlongPath());
    }

    public void MoveToTarget(HashSet<string> blockedTiles = null)
    {
        if (!isMoving)
            StartCoroutine(MoveAlongPath(blockedTiles));
    }

    public void MoveToTarget(HashSet<string> blockedTiles, int overrideSteps)
    {
        if (!isMoving)
            StartCoroutine(MoveAlongPath(blockedTiles, overrideSteps));
    }
    private void StopMoving()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopLoop();
        isMoving = false;
    }

    private IEnumerator MoveAlongPath(HashSet<string> blockedTiles = null, int overrideSteps = -1)
    {
        if (this == null || gameObject == null) yield break;

        isMoving = true;
        if (AudioManager.Instance != null)
    AudioManager.Instance.PlayCrewWalking();
        stepsUsedThisTurn = 0;

        int stepsAllowed = overrideSteps > 0 ? overrideSteps
                 : isGreaterFeared ? 1
                 : isFeared ? maxSteps / 2
                 : maxSteps;

        if (gameObject.CompareTag("Captain"))
        {
            isFeared = false;
            isGreaterFeared = false;
            stepsAllowed = overrideSteps > 0 ? overrideSteps : maxSteps;
            Debug.Log("Its Captain Mate");
        }
        else
        {
            isFeared = false;
            isGreaterFeared = false;
        }

        int stepsRemaining = stepsAllowed;

        if (!isScientist && !string.IsNullOrEmpty(capInProgress) && capMovesRemaining > 0)
        {
            if (this == null) { StopMoving(); yield break; }

            int movesSpent = Mathf.Min(stepsRemaining, capMovesRemaining);
            capMovesRemaining -= movesSpent;
            stepsRemaining -= movesSpent;
            stepsUsedThisTurn += movesSpent;

            Debug.Log($"{gameObject.tag} spending {movesSpent} moves on cap {capInProgress} — {capMovesRemaining} moves left.");
            yield return new WaitForSeconds(movesSpent * (1f / moveSpeed));

            if (this == null) { StopMoving(); yield break; }

            if (capMovesRemaining <= 0)
            {
                CapPointManager.Instance.DestroyCapPoint(capInProgress, gameObject.tag);
                Debug.Log($"{gameObject.tag} destroyed cap point {capInProgress}!");
                capInProgress = "";
                capMovesRemaining = 0;
            }

            if (stepsRemaining <= 0 || capMovesRemaining > 0)
            {
                StopMoving();
                yield break;
            }
        }

        if (this == null) { StopMoving(); yield break; }

        List<TileData> path = Pathfinder.Instance.FindPath(currentTile, targetTile, blockedTiles, canTravelThroughVents);

        if (path == null || path.Count <= 1)
        {
            Debug.LogWarning($"{gameObject.tag}: No path found or already at destination.");
            StopMoving();
            yield break;
        }

        int stepsToTake = Mathf.Min(stepsRemaining, path.Count - 1);

        for (int i = 1; i <= stepsToTake; i++)
        {
            if (this == null || gameObject == null) { StopMoving(); yield break; }

            killedAlienThisStep = false;
            yield return StartCoroutine(MoveToTile(path[i]));

            if (this == null || gameObject == null) { StopMoving(); yield break; }

            stepsUsedThisTurn++;

            if (killedAlienThisStep)
            {
                StopMoving();
                yield break;
            }

            // ← POD: crew entered pod mid-path — stop moving
            if (!gameObject.activeSelf)
            {
                StopMoving();
                yield break;
            }

            if (!isScientist && !string.IsNullOrEmpty(capInProgress) && capMovesRemaining > 0)
            {
                int stepsLeft = stepsToTake - i;
                int movesSpent = Mathf.Min(stepsLeft, capMovesRemaining);
                capMovesRemaining -= movesSpent;
                stepsUsedThisTurn += movesSpent;

                Debug.Log($"{gameObject.tag} using {movesSpent} remaining moves on cap — {capMovesRemaining} left.");
                yield return new WaitForSeconds(movesSpent * (1f / moveSpeed));

                if (this == null) { StopMoving(); yield break; }

                if (capMovesRemaining <= 0)
                {
                    CapPointManager.Instance.DestroyCapPoint(capInProgress);
                    Debug.Log($"{gameObject.tag} destroyed cap point {capInProgress}!");
                    capInProgress = "";
                    capMovesRemaining = 0;
                }

                break;
            }
        }

        if (this == null || gameObject == null) { StopMoving(); yield break; }

        if (currentTile == GameManager.Instance.GetMainSwitchTile())
            GameManager.Instance.DisableMainSwitch();

        StopMoving();
    }

    private IEnumerator MoveToTile(TileData tile)
    {
        if (this == null || gameObject == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 center = GetTileCenter(tile.gameObject);
        Vector3 endPos = new Vector3(center.x, center.y, lockedZ);

        float duration = 1f / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (this == null || gameObject == null) yield break;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (this == null || gameObject == null) yield break;

        transform.position = endPos;
        currentTile = tile.tileName;

        TileStatusManager.Instance.UpdateTile(currentTile, gameObject);
        CollectibleManager.Instance.TryPickup(gameObject.tag, currentTile);

        if (CollectibleManager.Instance.HasCollectible(gameObject.tag, CollectibleType.FireExtinguisher)
            && FireSpread.Instance.IsTileOnFire(currentTile))
        {
            FireSpread.Instance.ExtinguishTile(currentTile);
            Debug.Log($"{gameObject.tag} extinguished fire at {currentTile}!");
        }

        if (Alien.Instance != null && Alien.Instance.IsReleased() &&
            currentTile == Alien.Instance.GetCurrentTile())
        {
            if (CollectibleManager.Instance.HasCollectible(gameObject.tag, CollectibleType.Axe))
            {
                Debug.Log($"{gameObject.tag} killed the Alien with the Axe at {currentTile}!");
                Alien.Instance.gameObject.SetActive(false);
                killedAlienThisStep = true;
            }
            else
            {
                Debug.Log($"{gameObject.tag} walked into the Alien without an Axe and died!");
                GameManager.Instance.RemoveCrewMember(this);
                yield break;
            }
        }

        if (!killedAlienThisStep && CapPointManager.Instance.IsCapPoint(currentTile))
        {
            if (isScientist)
            {
                CapPointManager.Instance.DestroyCapPoint(currentTile);
                Debug.Log($"Scientist instantly destroyed cap point at {currentTile}!");
            }
            else if (capInProgress != currentTile)
            {
                capInProgress = currentTile;
                capMovesRemaining = 4;
                Debug.Log($"{gameObject.tag} started destroying cap {currentTile} — needs 4 movement tiles.");
            }
        }

        CapPointManager.Instance.CheckCapPoint(currentTile);

        if (currentTile == PlayerActionManager.Instance.GetO2Tile())
        {
            GameManager.Instance.ResetO2();
            Debug.Log($"{gameObject.tag} reached O2 tile and restored O2!");
        }

        Debug.Log($"{gameObject.tag} stepped to: {currentTile}");

        // ← POD: check if a pod is parked on this tile
        if (justReturnedFromSpace)
        {
            justReturnedFromSpace = false; // clear flag after first real step
        }
        else
        {
            AirlockManager.Instance.OnCrewSteppedOnTile(this, currentTile);
        }
    }

    public void TeleportToTile(string tileName)
    {
        StopMoving();
        justReturnedFromSpace = true; // ← ignore pod on this tile

        GameObject tileObj = GameObject.Find(tileName);
        if (tileObj != null)
        {
            Vector3 center = GetTileCenter(tileObj);
            transform.position = new Vector3(center.x, center.y, lockedZ);
        }
        currentTile = tileName;
        TileStatusManager.Instance.UpdateTile(currentTile, gameObject);
        Debug.Log($"{gameObject.tag} teleported to {tileName}");
    }

    public bool UseExtraLife()
    {
        if (hasSoldierExtraLife && !extraLifeUsed)
        {
            extraLifeUsed = true;
            return true;
        }
        return false;
    }

    public static void ApplyFearToTag(string tag)
    {
        GameObject target = GameObject.FindWithTag(tag);
        if (target != null)
        {
            CharacterMovement cm = target.GetComponent<CharacterMovement>();
            if (cm != null) cm.ApplyFear();
        }
    }

    public void ApplyFear()
    {
        // ✅ Captain is immune to fear
        if (gameObject.CompareTag("Captain"))
        {
            Debug.Log("Captain is immune to fear — ignored!");
            return;
        }

        isFeared = true;
        CrewIconManager.Instance.SetFeared(gameObject.tag);
        Debug.Log($"{gameObject.tag} is feared! Steps reduced next move.");
    }

    public void ApplyGreaterFear()
    {
        // ✅ Captain is immune to greater fear too
        if (gameObject.CompareTag("Captain"))
        {
            Debug.Log("Captain is immune to greater fear — ignored!");
            return;
        }

        isFeared = true;
        isGreaterFeared = true;
        CrewIconManager.Instance.SetFeared(gameObject.tag);
        Debug.Log($"{gameObject.tag} is GREATER FEARED! Steps reduced to 1.");
    }

    private Vector3 GetTileCenter(GameObject tileObj)
    {
        Renderer r = tileObj.GetComponent<Renderer>();
        if (r != null) return r.bounds.center;
        return tileObj.transform.position;
    }

    public string GetCurrentTile() => currentTile;
    public void SetTarget(string tile) => targetTile = tile;
    public bool CanUseVents() => canTravelThroughVents;
    public bool IsMoving() => isMoving;
    public int GetMaxSteps() => maxSteps;
    public int GetStepsUsedThisTurn() => stepsUsedThisTurn;
}