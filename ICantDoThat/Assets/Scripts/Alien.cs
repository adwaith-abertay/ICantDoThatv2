using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alien : MonoBehaviour
{
    public static Alien Instance;

    [Header("Settings")]
    public int maxSteps = 6;
    public float moveSpeed = 5f;

    private string currentTile;
    private bool isReleased = false;
    private bool isMoving = false;
    private float lockedZ;
    public GameObject cryopodclosed;
    public GameObject cryopodopen;

    private void Awake() => Instance = this;

    private void Start()
    {
        lockedZ = transform.position.z;
        gameObject.SetActive(false); // Hidden until released
    }

    public void Release(string spawnTile)
    {
        
        isReleased = true;
        currentTile = spawnTile;
        cryopodclosed.SetActive(false);
        cryopodopen.SetActive(true);
        gameObject.SetActive(true);

        GameObject spawnObj = GameObject.Find(spawnTile);
        if (spawnObj != null)
        {
            Renderer r = spawnObj.GetComponent<Renderer>();
            Vector3 pos = r != null ? r.bounds.center : spawnObj.transform.position;
            transform.position = new Vector3(pos.x, pos.y, lockedZ);
        }

        Debug.Log("Alien released!");
    }

    public IEnumerator TakeTurn()
    {
        if (!isReleased || isMoving) yield break;

        CharacterMovement target = GetNearestCrew();
        if (target == null) yield break;

        isMoving = true;
        HashSet<string> blocked = new HashSet<string>(); // Alien ignores fire
        List<TileData> path = Pathfinder.Instance.FindPath(currentTile, target.GetCurrentTile(), blocked, false);

        if (path == null || path.Count <= 1)
        {
            isMoving = false;
            yield break;
        }

        int steps = Mathf.Min(maxSteps, path.Count - 1);

        for (int i = 1; i <= steps; i++)
        {
            yield return StartCoroutine(MoveToTile(path[i]));

            // Kill crew member if on same tile
            CharacterMovement crew = GetCrewOnTile(currentTile);
            if (crew != null)
            {
                Debug.Log($"Alien killed {crew.gameObject.tag}!");
                GameManager.Instance.RemoveCrewMember(crew);
                break;
            }
        }

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
        Debug.Log($"Alien moved to: {currentTile}");
    }

    private CharacterMovement GetNearestCrew()
    {
        List<CharacterMovement> crew = GameManager.Instance.GetCrewMembers();
        CharacterMovement nearest = null;
        int shortest = int.MaxValue;

        foreach (CharacterMovement cm in crew)
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

    public bool IsReleased() => isReleased;
    public string GetCurrentTile() => currentTile;
}
