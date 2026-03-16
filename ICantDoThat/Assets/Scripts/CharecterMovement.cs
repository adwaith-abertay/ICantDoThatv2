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

    private string currentTile;
    private bool isMoving = false;
    private bool isFeared = false;
    private float lockedZ;

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
    }

    // Call from button
    public void MoveToTarget()
    {
        if (!isMoving)
            StartCoroutine(MoveAlongPath());
    }

    // Call these from buttons or testing
    public static void FearCaptain()    { ApplyFearToTag("Captain"); }
    public static void FearScientist()  { ApplyFearToTag("Scientist"); }
    public static void FearEngineer()   { ApplyFearToTag("Engineer"); }
    public static void FearSoldier()    { ApplyFearToTag("Soldier"); }

    private static void ApplyFearToTag(string tag)
    {
        GameObject target = GameObject.FindWithTag(tag);
        if (target != null)
        {
            CharacterMovement cm = target.GetComponent<CharacterMovement>();
            if (cm != null) cm.ApplyFear();
        }
    }



    private void ApplyFear()
    {
        isFeared = true;
        Debug.Log($"{gameObject.tag} is now feared! Steps reduced to {maxSteps / 2} for next move.");
    }

    private IEnumerator MoveAlongPath()
    {
        isMoving = true;

        // Determine step limit
        int stepsAllowed = isFeared ? maxSteps / 2 : maxSteps;
        isFeared = false; // Reset fear after consuming it

        List<TileData> path = Pathfinder.Instance.FindPath(currentTile, targetTile);

        if (path == null || path.Count <= 1)
        {
            Debug.LogWarning("No path found or already at destination.");
            isMoving = false;
            yield break;
        }

        // Clamp path to allowed steps
        int stepsToTake = Mathf.Min(stepsAllowed, path.Count - 1);

        for (int i = 1; i <= stepsToTake; i++)
        {
            yield return StartCoroutine(MoveToTile(path[i]));
        }

        // Check win — only if we actually reached the target
        if (currentTile == targetTile)
        {
            Debug.Log($"{gameObject.tag} arrived at destination!");
            UIManager.Instance.ShowCrewWin(gameObject.tag);
        }
        else
        {
            Debug.Log($"{gameObject.tag} stopped at {currentTile} after {stepsToTake} steps.");
        }

        isMoving = false;
    }

    private IEnumerator MoveToTile(TileData tile)
    {
        Vector3 startPos = transform.position;
        Vector3 center = GetTileCenter(tile.gameObject);
        Vector3 endPos = new Vector3(center.x, center.y, lockedZ);

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

        TileStatusManager.Instance.UpdateTile(currentTile, gameObject);
        Debug.Log($"{gameObject.tag} stepped to: {currentTile}");
    }

    private Vector3 GetTileCenter(GameObject tileObj)
    {
        Renderer r = tileObj.GetComponent<Renderer>();
        if (r != null) return r.bounds.center;
        return tileObj.transform.position;
    }
}
