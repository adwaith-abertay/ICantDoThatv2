using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIBrain : MonoBehaviour
{
    public static AIBrain Instance;

    private void Awake() => Instance = this;

public IEnumerator RunCrewActions()
{
    HashSet<string> fireTiles = FireSpread.Instance.GetBurningTiles();
    bool alienReleased = Alien.Instance.IsReleased();
    bool fireActive = fireTiles.Count > 0;

    string[] turnOrder = { "Captain", "Scientist", "Soldier", "Engineer" };

    List<CharacterMovement> orderedCrew = new List<CharacterMovement>();
    foreach (string tag in turnOrder)
    {
        GameObject obj = GameObject.FindWithTag(tag);
        if (obj == null) continue;
        CharacterMovement cm = obj.GetComponent<CharacterMovement>();
        if (cm != null) orderedCrew.Add(cm);
    }

    Dictionary<CharacterMovement, string> assignments = AssignDestinations(orderedCrew, fireTiles, alienReleased, fireActive);

    for (int idx = 0; idx < orderedCrew.Count; idx++)
    {
        CharacterMovement cm = orderedCrew[idx];
        if (cm == null) continue;
        if (!assignments.ContainsKey(cm)) continue;

        string dest = assignments[cm];
        if (string.IsNullOrEmpty(dest)) continue;

        string crewTag = cm.gameObject.tag;
        bool isAxeHunter = alienReleased &&
            CollectibleManager.Instance.GetCollectible(crewTag) == CollectibleType.Axe;
        bool isExtinguisher = CollectibleManager.Instance.GetCollectible(crewTag) == CollectibleType.FireExtinguisher;

        // Leg 1 — move to assigned destination
        cm.SetTarget(dest);
        Debug.Log($"{crewTag} assigned to: {dest}");

        yield return new WaitForSeconds(0.1f);
        if (cm == null) continue;

        // Engineer can walk into fire tiles, everyone else avoids them
        cm.MoveToTarget(isExtinguisher ? null : fireTiles);

        CharacterMovement capturedCm = cm;
        yield return new WaitUntil(() => capturedCm == null || !capturedCm.gameObject.activeInHierarchy || !capturedCm.IsMoving());

        // Leg 2 — Axe holder killed Alien, continue with remaining steps
        if (cm != null && isAxeHunter &&
            (Alien.Instance == null || !Alien.Instance.gameObject.activeInHierarchy))
        {
            int stepsLeft = cm.GetMaxSteps() - cm.GetStepsUsedThisTurn();
            Debug.Log($"{crewTag} killed Alien — {stepsLeft} steps remaining");

            if (stepsLeft > 0)
            {
                string nextDest = GetBestUniqueDestination(cm.GetCurrentTile(), cm.CanUseVents());
                if (!string.IsNullOrEmpty(nextDest))
                {
                    Debug.Log($"{crewTag} continuing to {nextDest} with {stepsLeft} steps");
                    cm.SetTarget(nextDest);

                    yield return new WaitForSeconds(0.1f);
                    if (cm == null) continue;

                    cm.MoveToTarget(fireTiles, stepsLeft);

                    CharacterMovement capturedCm2 = cm;
                    yield return new WaitUntil(() => capturedCm2 == null || !capturedCm2.gameObject.activeInHierarchy || !capturedCm2.IsMoving());
                }
            }
        }

        // Leg 3 — Engineer retreat if still on or adjacent to fire after extinguishing
        if (cm != null && isExtinguisher)
        {
            string engineerTile = cm.GetCurrentTile();
            HashSet<string> updatedFire = FireSpread.Instance.GetBurningTiles();

            // Check if on fire or adjacent to fire
            bool inDanger = updatedFire.Contains(engineerTile);
            if (!inDanger)
            {
                List<TileData> neighbours = GridManager.Instance.GetAllNeighbours(engineerTile);
                foreach (TileData n in neighbours)
                {
                    if (updatedFire.Contains(n.tileName)) { inDanger = true; break; }
                }
            }

            if (inDanger)
            {
                // Find nearest safe walkable neighbour
                string safeTile = null;
                List<TileData> neighbours = GridManager.Instance.GetAllNeighbours(engineerTile);
                foreach (TileData n in neighbours)
                {
                    if (!updatedFire.Contains(n.tileName) && n.isWalkable)
                    {
                        safeTile = n.tileName;
                        break;
                    }
                }

                if (safeTile != null)
                {
                    Debug.Log($"{crewTag} retreating from fire to {safeTile}");
                    cm.SetTarget(safeTile);

                    yield return new WaitForSeconds(0.1f);
                    if (cm == null) continue;

                    cm.MoveToTarget(null, 1); // 1 step retreat, no blocked tiles

                    CharacterMovement capturedRetreat = cm;
                    yield return new WaitUntil(() => capturedRetreat == null || !capturedRetreat.gameObject.activeInHierarchy || !capturedRetreat.IsMoving());
                }
                else
                {
                    Debug.Log($"{crewTag} is trapped — no safe tile to retreat to!");
                }
            }
        }
    }

    
}

    private Dictionary<CharacterMovement, string> AssignDestinations(
        List<CharacterMovement> crew,
        HashSet<string> fireTiles,
        bool alienReleased,
        bool fireActive)
    {
        Dictionary<CharacterMovement, string> assignments = new Dictionary<CharacterMovement, string>();
        HashSet<string> claimedTargets = new HashSet<string>();

        foreach (CharacterMovement cm in crew)
        {
            if (cm == null) continue;

            string tag = cm.gameObject.tag;
            CollectibleType held = CollectibleManager.Instance.GetCollectible(tag);

            // Priority 1: Axe holder hunts alien
            if (held == CollectibleType.Axe && alienReleased)
            {
                if (Alien.Instance == null) goto defaultDest;
                string alienTile = Alien.Instance.GetCurrentTile();
                assignments[cm] = alienTile;
                Debug.Log($"{tag} has Axe — hunting Alien at {alienTile}");
                continue;
            }

            // Priority 2: Extinguisher holder goes to nearest fire
            if (held == CollectibleType.FireExtinguisher && fireActive)
            {
                string nearestFire = GetNearestTile(cm.GetCurrentTile(), new List<string>(fireTiles), claimedTargets);
                if (nearestFire != null)
                {
                    assignments[cm] = nearestFire;
                    //claimedTargets.Add(nearestFire);
                    Debug.Log($"{tag} has Extinguisher — heading to fire at {nearestFire}");
                    continue;
                }
            }

            // Priority 3: Closest crew member goes to O2 if triggered
            if (GameManager.Instance.IsO2Triggered() && !claimedTargets.Contains(PlayerActionManager.Instance.GetO2Tile()))
            {
                CharacterMovement o2Runner = GetClosestCrewToTile(crew, PlayerActionManager.Instance.GetO2Tile(), fireTiles);
                if (o2Runner == cm)
                {
                    string o2Tile = PlayerActionManager.Instance.GetO2Tile();
                    assignments[cm] = o2Tile;
                    claimedTargets.Add(o2Tile);
                    Debug.Log($"{tag} rerouting to O2 tile!");
                    continue;
                }
            }

            // Priority 4: Unique nearest destroyable point
            defaultDest:
            string dest = GetBestUniqueDestination(cm.GetCurrentTile(), cm.CanUseVents(), claimedTargets);
            assignments[cm] = dest;
            if (!string.IsNullOrEmpty(dest))
                claimedTargets.Add(dest);
        }

        return assignments;
    }

    public string GetBestUniqueDestination(string fromTile, bool canUseVents, HashSet<string> claimed = null)
    {
        HashSet<string> fire = FireSpread.Instance.GetBurningTiles();
        string best = null;
        int shortest = int.MaxValue;

        foreach (string cap in CapPointManager.Instance.capPointTiles)
        {
            if (!CapPointManager.Instance.IsCapPoint(cap)) continue;
            if (claimed != null && claimed.Contains(cap)) continue;

            List<TileData> path = Pathfinder.Instance.FindPath(fromTile, cap, fire, canUseVents);
            if (path != null && path.Count < shortest)
            {
                shortest = path.Count;
                best = cap;
            }
        }

        string mainSwitch = GameManager.Instance.GetMainSwitchTile();
        bool mainSwitchClaimed = claimed != null && claimed.Contains(mainSwitch);

        if (!mainSwitchClaimed || best == null)
        {
            List<TileData> switchPath = Pathfinder.Instance.FindPath(fromTile, mainSwitch, fire, canUseVents);
            if (switchPath != null && switchPath.Count < shortest)
                best = mainSwitch;
        }

        return best ?? mainSwitch;
    }

    public string GetBestDestination(string fromTile, bool canUseVents)
    {
        return GetBestUniqueDestination(fromTile, canUseVents, null);
    }

    private string GetNearestTile(string from, List<string> candidates, HashSet<string> claimed = null)
    {
        string nearest = null;
        int shortest = int.MaxValue;

        foreach (string candidate in candidates)
        {
            if (claimed != null && claimed.Contains(candidate)) continue;
            List<TileData> path = Pathfinder.Instance.FindPath(from, candidate);
            if (path != null && path.Count < shortest)
            {
                shortest = path.Count;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private CharacterMovement GetClosestCrewToTile(List<CharacterMovement> crew, string tile, HashSet<string> fireTiles)
    {
        CharacterMovement closest = null;
        int shortest = int.MaxValue;

        foreach (CharacterMovement cm in crew)
        {
            if (cm == null) continue;

            List<TileData> path = Pathfinder.Instance.FindPath(cm.GetCurrentTile(), tile, fireTiles, cm.CanUseVents());
            if (path != null && path.Count < shortest)
            {
                shortest = path.Count;
                closest = cm;
            }
        }

        return closest;
    }
}