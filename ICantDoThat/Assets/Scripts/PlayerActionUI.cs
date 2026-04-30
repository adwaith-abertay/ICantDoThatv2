using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerActionUI : MonoBehaviour
{
    public static PlayerActionUI Instance;

    public enum ActionMode
    {
        None,
        Fear,
        GreaterFear,
        LockDoor
    }

    private ActionMode activeMode = ActionMode.None;

    [Header("Mode Buttons")]
    public Button fearBtn;
    public Button greaterFearBtn;
    public Button lockDoorBtn;

    [Header("Action Buttons")]
    public Button unfreezeAlienBtn;
    public Button hackRobotBtn;
    public Button startFireBtn;
    public Button cutO2Btn;
    public Button endTurnBtn;
    public Button flushPodBtn;

    [Header("Mode Indicator")]
    public TextMeshProUGUI activeModeLabel;

    private void Awake() => Instance = this;

    private void Start()
    {
        fearBtn.onClick.AddListener(() => ToggleMode(ActionMode.Fear));
        greaterFearBtn.onClick.AddListener(() => ToggleMode(ActionMode.GreaterFear));
        lockDoorBtn.onClick.AddListener(() => ToggleMode(ActionMode.LockDoor));

        unfreezeAlienBtn.onClick.AddListener(() =>
        {
            PlayerActionManager.Instance.ActionReleaseAlien();
            RefreshButtons();
        });

        hackRobotBtn.onClick.AddListener(() =>
        {
            PlayerActionManager.Instance.ActionHackRobot();
            RefreshButtons();
        });

        startFireBtn.onClick.AddListener(() => PlayerActionManager.Instance.ActionStartFire());
        cutO2Btn.onClick.AddListener(() => PlayerActionManager.Instance.ActionCutO2());
        endTurnBtn.onClick.AddListener(() =>
        {
            CancelMode();
            DoorManager.Instance.OnTurnEnd();
            GameManager.Instance.EndPlayerTurn();
        });

        flushPodBtn.onClick.AddListener(() =>
        {
            AirlockManager.Instance.FlushCrewInSpace();
            RefreshButtons();
        });

        StartCoroutine(InitButtons());
    }

    private IEnumerator InitButtons()
    {
        yield return null;
        yield return null;
        RefreshButtons();
    }

    private void ToggleMode(ActionMode mode)
    {
        if (activeMode == mode)
        {
            CancelMode();
        }
        else
        {
            activeMode = mode;

            // If entering door mode, tell DoorManager to show door sprites
            if (activeMode == ActionMode.LockDoor)
                DoorManager.Instance.EnterDoorMode();

            Debug.Log($"Action mode set: {activeMode}");
            RefreshButtons();
            UpdateModeLabel();
        }
    }

    public void CancelMode()
    {
        // If cancelling out of door mode without end turn, hide unlocked doors
        if (activeMode == ActionMode.LockDoor)
            DoorManager.Instance.OnTurnEnd();

        activeMode = ActionMode.None;
        RefreshButtons();
        UpdateModeLabel();
    }

    // Called by CrewClickHandler when a crew member is clicked
    public void OnCrewClicked(string tag)
    {
        if (activeMode == ActionMode.None) return;

        switch (activeMode)
        {
            case ActionMode.Fear:
                PlayerActionManager.Instance.ActionFear(tag);
                CancelMode();
                break;

            case ActionMode.GreaterFear:
                PlayerActionManager.Instance.ActionGreaterFear(tag);
                CancelMode();
                break;

            case ActionMode.LockDoor:
                // Crew click ignored in door mode — player should click a door sprite
                Debug.Log("Door mode active — click a door sprite, not a crew member.");
                break;
        }
    }

    // Called by DoorClickHandler when a door sprite is clicked
    public void OnDoorClicked(string doorName)
    {
        if (activeMode != ActionMode.LockDoor) return;

        DoorManager.Instance.OnDoorClicked(doorName);
        // Don't cancel mode — allow player to lock multiple doors per activation
        RefreshButtons();
    }

    private string GetCrewCurrentTile(string tag)
    {
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
        {
            if (cm == null || cm.gameObject == null) continue;
            if (cm.gameObject.tag == tag) return cm.GetCurrentTile();
        }
        return "";
    }

    public void RefreshButtons()
    {
        if (PlayerActionManager.Instance == null || GameManager.Instance == null || FireSpread.Instance == null)
            return;

        int energy = PlayerActionManager.Instance.GetEnergy();
        bool o2Active = GameManager.Instance.IsO2Triggered();
        bool alienReleased = Alien.Instance != null && Alien.Instance.IsReleased();
        bool robotHacked = Robot.Instance != null && Robot.Instance.IsHacked();

        fearBtn.interactable          = energy >= 1;
        greaterFearBtn.interactable   = energy >= 3;
        lockDoorBtn.interactable      = energy >= DoorManager.Instance.doorCloseCost;
        unfreezeAlienBtn.interactable = energy >= 5 && !alienReleased;
        hackRobotBtn.interactable     = energy >= Robot.Instance.hackCost && !robotHacked;

        startFireBtn.interactable = energy >= 4 && !FireSpread.Instance.IsFireActive();
        cutO2Btn.interactable     = energy >= 8 && !o2Active;
        flushPodBtn.interactable = energy >= 8 && AirlockManager.Instance.IsAnyoneInSpace();
        endTurnBtn.interactable   = true;

        // Highlight active mode button
        SetButtonHighlight(fearBtn,        activeMode == ActionMode.Fear);
        SetButtonHighlight(greaterFearBtn, activeMode == ActionMode.GreaterFear);
        SetButtonHighlight(lockDoorBtn,    activeMode == ActionMode.LockDoor);
    }

    private void SetButtonHighlight(Button btn, bool highlight)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = highlight ? new Color(1f, 0.8f, 0f) : Color.white;
        btn.colors = cb;
    }

    private void UpdateModeLabel()
    {
        if (activeModeLabel == null) return;

        activeModeLabel.text = activeMode switch
        {
            ActionMode.None       => "",
            ActionMode.LockDoor   => "Mode: Lock Door — click a door",
            _                     => $"Mode: {activeMode} — click a crew member"
        };
    }

    private bool IsCrewAlive(string tag)
    {
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
        {
            if (cm == null || cm.gameObject == null) continue;
            if (cm.gameObject.tag == tag) return true;
        }
        return false;
    }
}