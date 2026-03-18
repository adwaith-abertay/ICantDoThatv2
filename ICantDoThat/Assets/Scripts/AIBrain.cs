using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIBrain : MonoBehaviour
{
    public static AIBrain Instance;

    private void Awake() => Instance = this;

    public IEnumerator RunCrewActions()
    {
        List<CharacterMovement> crew = GameManager.Instance.GetCrewMembers();
        if (crew.Count == 0) yield break;

        HashSet<string> fireTiles = FireSpread.Instance.GetBurningTiles();

        // If O2 triggered, send closest crew member to O2 tile
        CharacterMovement o2Responder = null;
        if (GameManager.Instance.IsO2Triggered())
        {
            o2Responder = GetClosestCrew(crew, PlayerActionManager.Instance.GetO2Tile(), fireTiles);
            if (o2Responder != null)
            {
                Debug.Log($"{o2Responder.gameObject.tag} rerouting to O2 tile!");
                o2Responder.SetTarget(PlayerActionManager.Instance.GetO2Tile());
            }
        }

        // Assign remaining crew to cap points then main switch
        List<string> activeCapPoints = GetActiveCapPoints();
        int capIndex = 0;

        foreach (CharacterMovement cm in crew)
        {
            if (cm == o2Responder) continue;

            if (capIndex < activeCapPoints.Count)
            {
                cm.SetTarget(activeCapPoints[capIndex]);
                capIndex++;
            }
            else
            {
                cm.SetTarget(GameManager.Instance.GetMainSwitchTile());
            }

            yield return new WaitForSeconds(0.1f);
            cm.MoveToTarget(fireTiles);
            yield return new WaitForSeconds(0.5f);
        }

        // Move O2 responder last
        if (o2Responder != null)
        {
            yield return new WaitForSeconds(0.1f);
            o2Responder.MoveToTarget(fireTiles);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private CharacterMovement GetClosestCrew(List<CharacterMovement> crew, string targetTile, HashSet<string> fireTiles)
    {
        CharacterMovement closest = null;
        int shortest = int.MaxValue;

        foreach (CharacterMovement cm in crew)
        {
            List<TileData> path = Pathfinder.Instance.FindPath(cm.GetCurrentTile(), targetTile, fireTiles);
            if (path != null && path.Count < shortest)
            {
                shortest = path.Count;
                closest = cm;
            }
        }

        return closest;
    }

    private List<string> GetActiveCapPoints()
    {
        List<string> active = new List<string>();
        foreach (string tile in CapPointManager.Instance.capPointTiles)
        {
            if (CapPointManager.Instance.IsCapPoint(tile))
                active.Add(tile);
        }
        return active;
    }
}
