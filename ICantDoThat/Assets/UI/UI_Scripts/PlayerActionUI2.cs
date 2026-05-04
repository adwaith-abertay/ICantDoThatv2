using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerActionUI2 : MonoBehaviour
{
    public static PlayerActionUI2 Instance;

    public enum ActionMode
    {
        None,
        Fear,
        GreaterFear,
        LockDoor
    }

    private ActionMode activeMode = ActionMode.None;

    private UIDocument uiDoc;

    // --- Buttons ---
    private Button fearBtn;          // BTN1
    private Button lockDoorBtn;      // BTN2
    private Button greaterFearBtn;   // BTN3
    private Button startFireBtn;     // BTN4
    private Button ventAirlockBtn;   // BTN5
    private Button hackRobotBtn;     // BTN6
    private Button ventDockedPodBtn; // BTN7
    private Button unfreezeAlienBtn; // BTN8
    private Button cutO2Btn;         // BTN9
    private Button endTurnBtn;       // BTN0

    private Label activeModeLabel;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        fearBtn = root.Q<Button>("BTN1");
        lockDoorBtn = root.Q<Button>("BTN2");
        greaterFearBtn = root.Q<Button>("BTN3");
        startFireBtn = root.Q<Button>("BTN4");
        ventAirlockBtn = root.Q<Button>("BTN5");
        hackRobotBtn = root.Q<Button>("BTN6");
        ventDockedPodBtn = root.Q<Button>("BTN7");
        unfreezeAlienBtn = root.Q<Button>("BTN8");
        cutO2Btn = root.Q<Button>("BTN9");
        endTurnBtn = root.Q<Button>("BTN0");

        if (fearBtn != null) fearBtn.clicked += () => ToggleMode(ActionMode.Fear);
        if (greaterFearBtn != null) greaterFearBtn.clicked += () => ToggleMode(ActionMode.GreaterFear);
        if (lockDoorBtn != null) lockDoorBtn.clicked += () => ToggleMode(ActionMode.LockDoor);

        if (unfreezeAlienBtn != null) unfreezeAlienBtn.clicked += () =>
        {
            PlayerActionManager.Instance.ActionReleaseAlien();
            UIEventsListener.OnAlienReleased?.Invoke();
            RefreshButtons();
        };

        if (hackRobotBtn != null) hackRobotBtn.clicked += () =>
        {
            PlayerActionManager.Instance.ActionHackRobot();
            UIEventsListener.OnRobotHacked?.Invoke();
            RefreshButtons();
        };

        if (startFireBtn != null) startFireBtn.clicked += () =>
        {
            PlayerActionManager.Instance.ActionStartFire();
            UIEventsListener.OnFireIgnited?.Invoke();
        };

        if (cutO2Btn != null) cutO2Btn.clicked += () =>
        {
            PlayerActionManager.Instance.ActionCutO2();
            UIEventsListener.OnOxygenDeactivated?.Invoke();
        };

        /* // --- FUTURE LOGIC FOR VENTING ---
        if (ventAirlockBtn != null) ventAirlockBtn.clicked += () => 
        {
            // PlayerActionManager.Instance.ActionVentAirlock();
            // UIEventsListener.OnAirlockOpened?.Invoke(());
            // RefreshButtons();
        };

        if (ventDockedPodBtn != null) ventDockedPodBtn.clicked += () => 
        {
            // PlayerActionManager.Instance.ActionVentDockedPod();
            // RefreshButtons();
        };
        */

        if (endTurnBtn != null) endTurnBtn.clicked += () =>
        {
            CancelMode();
            DoorManager.Instance.OnTurnEnd();
            GameManager.Instance.EndPlayerTurn();
        };

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

            if (activeMode == ActionMode.LockDoor)
                DoorManager.Instance.EnterDoorMode();

            Debug.Log($"Action mode set: {activeMode}");
            RefreshButtons();
            UpdateModeLabel();
        }
    }

    public void CancelMode()
    {
        if (activeMode == ActionMode.LockDoor)
            DoorManager.Instance.OnTurnEnd();

        activeMode = ActionMode.None;
        RefreshButtons();
        UpdateModeLabel();
    }

    public void OnCrewClicked(string tag)
    {
        if (activeMode == ActionMode.None) return;

        switch (activeMode)
        {
            case ActionMode.Fear:
                PlayerActionManager.Instance.ActionFear(tag);
                UIEventsListener.OnFrightened?.Invoke(tag);
                CancelMode();
                break;

            case ActionMode.GreaterFear:
                PlayerActionManager.Instance.ActionGreaterFear(tag);
                UIEventsListener.OnTerrified?.Invoke(tag);
                CancelMode();
                break;

            case ActionMode.LockDoor:
                Debug.Log("Door mode active — click a door sprite, not a crew member.");
                break;
        }
    }

    public void OnDoorClicked(string doorName)
    {
        if (activeMode != ActionMode.LockDoor) return;

        DoorManager.Instance.OnDoorClicked(doorName);
        UIEventsListener.OnDoorShut?.Invoke();
        RefreshButtons();
    }

    private string GetCrewCurrentTile(string tag)
    {
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
        {
            if (cm == null || cm.gameObject == null) continue;
            if (cm.gameObject.CompareTag(tag)) return cm.GetCurrentTile();
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

        fearBtn?.SetEnabled(energy >= 1);
        greaterFearBtn?.SetEnabled(energy >= 3);
        lockDoorBtn?.SetEnabled(energy >= DoorManager.Instance.doorCloseCost);
        unfreezeAlienBtn?.SetEnabled(energy >= 5 && !alienReleased);
        hackRobotBtn?.SetEnabled(energy >= Robot.Instance.hackCost && !robotHacked);

        startFireBtn?.SetEnabled(energy >= 4 && !FireSpread.Instance.IsFireActive());
        cutO2Btn?.SetEnabled(energy >= 8 && !o2Active);
        endTurnBtn?.SetEnabled(true);

        SetButtonHighlight(fearBtn, activeMode == ActionMode.Fear);
        SetButtonHighlight(greaterFearBtn, activeMode == ActionMode.GreaterFear);
        SetButtonHighlight(lockDoorBtn, activeMode == ActionMode.LockDoor);
    }

    private void SetButtonHighlight(Button btn, bool highlight)
    {
        if (btn == null) return;

        if (highlight)
        {
            btn.style.backgroundColor = new StyleColor(new Color(1f, 0.8f, 0f));
        }
        else
        {
            btn.style.backgroundColor = new StyleColor(StyleKeyword.Null);
        }
    }

    private void UpdateModeLabel()
    {
        if (activeModeLabel == null) return;

        activeModeLabel.text = activeMode switch
        {
            ActionMode.None => "",
            ActionMode.LockDoor => "Mode: Lock Door — click a door",
            _ => $"Mode: {activeMode} — click a crew member"
        };
    }

    private bool IsCrewAlive(string tag)
    {
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
        {
            if (cm == null || cm.gameObject == null) continue;
            if (cm.gameObject.CompareTag(tag)) return true;
        }
        return false;
    }

    
}