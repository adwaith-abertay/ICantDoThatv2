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

    private IEnumerator MoveAlongPath(HashSet<string> blockedTiles = null)
    {
        isMoving = true;

        int stepsAllowed = isFeared ? maxSteps / 2 : maxSteps;
        isFeared = false;

        List<TileData> path = Pathfinder.Instance.FindPath(currentTile, targetTile, blockedTiles, canTravelThroughVents);

        if (path == null || path.Count <= 1)
        {
            Debug.LogWarning($"{gameObject.tag}: No path found or already at destination.");
            isMoving = false;
            yield break;
        }

        int stepsToTake = Mathf.Min(stepsAllowed, path.Count - 1);

        for (int i = 1; i <= stepsToTake; i++)
        {
            yield return StartCoroutine(MoveToTile(path[i]));
        }

        if (currentTile == GameManager.Instance.GetMainSwitchTile())
            GameManager.Instance.CrewWins(gameObject.tag);

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
        CapPointManager.Instance.CheckCapPoint(currentTile);

        if (currentTile == PlayerActionManager.Instance.GetO2Tile())
        {
            GameManager.Instance.ResetO2();
            Debug.Log($"{gameObject.tag} reached O2 tile and restored O2!");
        }

        Debug.Log($"{gameObject.tag} stepped to: {currentTile}");
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
        isFeared = true;
        Debug.Log($"{gameObject.tag} is feared! Steps reduced to {maxSteps / 2} next move.");
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
}
