using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

public class SIIDUIManager : MonoBehaviour
{
    public static SIIDUIManager Instance;

    public enum ActionMode
    {
        None,
        Fear,         // BTN1
        GreaterFear,  // BTN3
        LockDoor,     // BTN2
        VentAirlock,  // BTN5
        VentPod       // BTN7
    }

    [Header("Inspector Setup")]
    public UIDocument uiDocument;

    [Header("Button Art (Drag your PNGs here)")]
    public Texture2D defaultArt;
    public Texture2D hoverArt;
    public Texture2D clickArt;
    public Texture2D disabledArt;

    [System.Serializable]
    public struct ButtonPowerRequirement
    {
        [Tooltip("The exact name of the button in UI Builder (e.g., BTN1)")]
        public string buttonName;
        [Tooltip("How much power is needed for this button to be active")]
        public int requiredPower;
        [Tooltip("The Player Aid text that appears in the console when clicked")]
        [TextArea(2, 3)]
        public string consoleLogText;
    }

    [Header("Power & Text Settings")]
    public List<ButtonPowerRequirement> powerRequirements = new();

    // Internal state
    private int currentPowerLevel = 10;
    private List<VisualElement> powerIndicators = new();
    private ActionMode activeMode = ActionMode.None;
    private VisualElement airlocksHolder;

    private class ButtonData
    {
        public VisualElement img1Default;
        public VisualElement img2Hover;
        public VisualElement img3Click;
        public VisualElement img4Disabled;
        public bool interactable = true;
        public bool isFocused = false; // <-- NEW: Tracks if the button is locked "on"
    }

    private readonly Dictionary<Button, ButtonData> buttonRegistry = new();
    private bool isDebugEnabled = true;

    private void Awake() => Instance = this;

    void OnEnable()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        // 1. Setup all the buttons visuals
        var allButtons = root.Query<Button>().ToList();
        foreach (var btn in allButtons)
        {
            var img1 = btn.Q<VisualElement>("IMG1");
            var img2 = btn.Q<VisualElement>("IMG2");
            var img3 = btn.Q<VisualElement>("IMG3");
            var img4 = btn.Q<VisualElement>("IMG4");

            if (img1 != null && img2 != null && img3 != null && img4 != null)
            {
                if (defaultArt != null) img1.style.backgroundImage = new StyleBackground(defaultArt);
                if (hoverArt != null) img2.style.backgroundImage = new StyleBackground(hoverArt);
                if (clickArt != null) img3.style.backgroundImage = new StyleBackground(clickArt);
                if (disabledArt != null) img4.style.backgroundImage = new StyleBackground(disabledArt);

                SetFade(img1, 0.5f);
                SetFade(img2, 0.5f);
                SetFade(img3, 0.5f);
                SetFade(img4, 0.5f);

                var data = new ButtonData
                {
                    img1Default = img1,
                    img2Hover = img2,
                    img3Click = img3,
                    img4Disabled = img4
                };

                buttonRegistry.Add(btn, data);

                btn.RegisterCallback<PointerEnterEvent, ButtonData>(OnHoverEnter, data);
                btn.RegisterCallback<PointerLeaveEvent, ButtonData>(OnHoverExit, data);
                btn.RegisterCallback<PointerDownEvent, ButtonData>(OnClickDown, data, TrickleDown.TrickleDown);
                btn.RegisterCallback<PointerUpEvent, ButtonData>(OnClickUp, data, TrickleDown.TrickleDown);

                SetState(data, 1);
            }
        }

        // 2. Setup the Power Indicators
        powerIndicators.Clear();
        for (int i = 1; i <= 10; i++)
        {
            var indicator = root.Q<VisualElement>($"SiidPower{i}");
            if (indicator != null) powerIndicators.Add(indicator);
        }

        // 3. Setup Airlocks Menu Visually
        airlocksHolder = root.Q<VisualElement>("AirlocksHolder");
        if (airlocksHolder != null)
        {
            airlocksHolder.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName> { new("opacity") });
            airlocksHolder.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new(0.2f, TimeUnit.Second) });

            airlocksHolder.style.opacity = 0f;
            airlocksHolder.style.display = DisplayStyle.None;

            for (int i = 1; i <= 5; i++)
            {
                int captured = i;

                // ✅ Use the exact names from your UXML
                string btnName = captured == 5 ? "Airlock5" : $"Airlock{captured}BTN";

                var airlockBtn = root.Q<Button>(btnName);

                if (airlockBtn != null)
                {
                    airlockBtn.clicked += () => OnAirlockTargetSelected(captured);
                    Debug.Log($"✅ Hooked up: {btnName}");
                }
                else
                {
                    Debug.LogWarning($"❌ Could not find: {btnName}");
                }
            }
            for (int i = 1; i <= 5; i++)
            {
                var testBtn = root.Q<Button>($"Airlock{i}");
                Debug.Log($"Airlock{i} found: {testBtn != null}");
            }
                        var allBtns = root.Query<Button>().ToList();
            foreach (var b in allBtns)
                Debug.Log($"Button found: '{b.name}'");
        }

        // ==========================================
        // --- GAME LOGIC HOOKUPS ---
        // ==========================================

        var fearBtn = root.Q<Button>("BTN1");
        var lockDoorBtn = root.Q<Button>("BTN2");
        var greaterFearBtn = root.Q<Button>("BTN3");
        var startFireBtn = root.Q<Button>("BTN4");
        var ventAirlockBtn = root.Q<Button>("BTN5");
        var hackRobotBtn = root.Q<Button>("BTN6");
        var ventPodBtn = root.Q<Button>("BTN7");
        var unfreezeAlienBtn = root.Q<Button>("BTN8");
        var cutO2Btn = root.Q<Button>("BTN9");
        var endTurnBtn = root.Q<Button>("BTN0");

        if (fearBtn != null) fearBtn.clicked += () =>
        {
            if (buttonRegistry[fearBtn].interactable)
            {
                ToggleMode(ActionMode.Fear);
                if (activeMode == ActionMode.Fear) RetroConsole.Instance.Log(GetLogText("BTN1", "Select a crew member to terrify."), ConsoleLogType.PlayerAid);
            }
        };

        if (greaterFearBtn != null) greaterFearBtn.clicked += () =>
        {
            if (buttonRegistry[greaterFearBtn].interactable)
            {
                ToggleMode(ActionMode.GreaterFear);
                if (activeMode == ActionMode.GreaterFear) RetroConsole.Instance.Log(GetLogText("BTN3", "Select a crew member to inflict Greater Fear."), ConsoleLogType.PlayerAid);
            }
        };

        if (lockDoorBtn != null) lockDoorBtn.clicked += () =>
        {
            if (buttonRegistry[lockDoorBtn].interactable)
            {
                ToggleMode(ActionMode.LockDoor);
                if (activeMode == ActionMode.LockDoor) RetroConsole.Instance.Log(GetLogText("BTN2", "Select a door to lock it down."), ConsoleLogType.PlayerAid);
            }
        };

        // --- NEW: Vent modes now use the Toggle system ---
        if (ventAirlockBtn != null) ventAirlockBtn.clicked += () =>
        {
            if (buttonRegistry[ventAirlockBtn].interactable)
            {
                ToggleMode(ActionMode.VentAirlock);
                if (activeMode == ActionMode.VentAirlock) RetroConsole.Instance.Log(GetLogText("BTN5", "Select an airlock to vent."), ConsoleLogType.PlayerAid);
            }
        };

        if (ventPodBtn != null) ventPodBtn.clicked += () =>
        {
            if (buttonRegistry[ventPodBtn].interactable)
            {
                ToggleMode(ActionMode.VentPod);
                if (activeMode == ActionMode.VentPod) RetroConsole.Instance.Log(GetLogText("BTN7", "Select a docked pod to flush."), ConsoleLogType.PlayerAid);
            }
        };

        if (unfreezeAlienBtn != null) unfreezeAlienBtn.clicked += () =>
        {
            if (buttonRegistry[unfreezeAlienBtn].interactable)
            {
                PlayerActionManager.Instance.ActionReleaseAlien();
                RetroConsole.Instance.Log(GetLogText("BTN8", "Alien stasis lock disengaged."), ConsoleLogType.PlayerAid);
            }
        };

        if (hackRobotBtn != null) hackRobotBtn.clicked += () =>
        {
            if (buttonRegistry[hackRobotBtn].interactable)
            {
                PlayerActionManager.Instance.ActionHackRobot();
                RetroConsole.Instance.Log(GetLogText("BTN6", "Service robot targeting systems corrupted."), ConsoleLogType.PlayerAid);
            }
        };

        if (startFireBtn != null) startFireBtn.clicked += () =>
        {
            if (buttonRegistry[startFireBtn].interactable)
            {
                PlayerActionManager.Instance.ActionStartFire();
                RetroConsole.Instance.Log(GetLogText("BTN4", "Engine thermal safeties disabled. Fire started."), ConsoleLogType.PlayerAid);
            }
        };

        if (cutO2Btn != null) cutO2Btn.clicked += () =>
        {
            if (buttonRegistry[cutO2Btn].interactable)
            {
                PlayerActionManager.Instance.ActionCutO2();
                RetroConsole.Instance.Log(GetLogText("BTN9", "Life support systems deactivated."), ConsoleLogType.PlayerAid);
            }
        };

        if (endTurnBtn != null) endTurnBtn.clicked += () =>
        {
            if (buttonRegistry[endTurnBtn].interactable)
            {
                CancelMode();
                DoorManager.Instance.OnTurnEnd();
                GameManager.Instance.EndPlayerTurn();
                RetroConsole.Instance.Log(GetLogText("BTN0", "Turn ended. Executing crew protocols."), ConsoleLogType.PlayerAid);
            }
        };

        UpdatePowerLevel(10);
    }

    private string GetLogText(string buttonName, string defaultText)
    {
        foreach (var req in powerRequirements)
        {
            if (req.buttonName == buttonName && !string.IsNullOrEmpty(req.consoleLogText))
                return req.consoleLogText;
        }
        return defaultText;
    }

    void Update()
    {
        if (PlayerActionManager.Instance != null)
        {
            int realEnergy = PlayerActionManager.Instance.GetEnergy();
            if (currentPowerLevel != realEnergy) UpdatePowerLevel(realEnergy);
        }

        if (Keyboard.current == null) return;
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            isDebugEnabled = !isDebugEnabled;
            foreach (var kvp in buttonRegistry)
            {
                var data = kvp.Value;
                data.interactable = isDebugEnabled;
                SetState(data, isDebugEnabled ? (data.isFocused ? 3 : 1) : 4);
            }
        }
    }

    // ==========================================
    // --- AIRLOCK MENU VISUALS ---
    // ==========================================

    private void ShowAirlockMenu()
    {
        if (airlocksHolder == null) return;
        airlocksHolder.style.display = DisplayStyle.Flex;
        airlocksHolder.schedule.Execute(() => airlocksHolder.style.opacity = 1f).StartingIn(10);
    }

    private void OnAirlockTargetSelected(int index)
    {
        if (activeMode == ActionMode.VentAirlock || activeMode == ActionMode.VentPod)
        {
            string airlockName = $"AirLock{index}";
            AirlockData target = AirlockManager.Instance.airlocks
                .Find(a => a.airlockName == airlockName);

            if (target != null)
                AirlockManager.Instance.TryTriggerAirlock(target);
            else
                Debug.LogWarning($"No AirlockData found with name '{airlockName}'");
        }

        CancelMode();
    }

    public void HideAirlockMenu()
    {
        if (airlocksHolder == null || airlocksHolder.style.display == DisplayStyle.None) return;

        airlocksHolder.style.opacity = 0f;
        airlocksHolder.schedule.Execute(() =>
        {
            airlocksHolder.style.display = DisplayStyle.None;
        }).StartingIn(200);

        // If the menu is forced closed (like from AirlockManager), ensure the UI modes sync up
        if (activeMode == ActionMode.VentAirlock || activeMode == ActionMode.VentPod)
        {
            activeMode = ActionMode.None;
            UpdateFocusStates();
        }
    }

    // ==========================================
    // --- MODE LOGIC & FOCUS ---
    // ==========================================

    private void ToggleMode(ActionMode mode)
    {
        if (activeMode == mode)
        {
            CancelMode(); // If they click it again, toggle it off!
        }
        else
        {
            CancelMode(); // Clean up previous mode first
            activeMode = mode;

            if (activeMode == ActionMode.LockDoor) DoorManager.Instance.EnterDoorMode();
            else if (activeMode == ActionMode.VentAirlock || activeMode == ActionMode.VentPod) ShowAirlockMenu();

            Debug.Log($"Action mode set: {activeMode}");
        }

        UpdateFocusStates();
    }

    public void CancelMode()
    {
        if (activeMode == ActionMode.LockDoor) DoorManager.Instance.OnTurnEnd();
        if (activeMode == ActionMode.VentAirlock || activeMode == ActionMode.VentPod) HideAirlockMenu();

        activeMode = ActionMode.None;
        UpdateFocusStates();
    }

    // --- NEW: Tells specific buttons to visually lock their state ---
    private void UpdateFocusStates()
    {
        SetButtonFocus("BTN1", activeMode == ActionMode.Fear);
        SetButtonFocus("BTN2", activeMode == ActionMode.LockDoor);
        SetButtonFocus("BTN3", activeMode == ActionMode.GreaterFear);
        SetButtonFocus("BTN5", activeMode == ActionMode.VentAirlock);
        SetButtonFocus("BTN7", activeMode == ActionMode.VentPod);
    }

    private void SetButtonFocus(string btnName, bool focus)
    {
        var btn = uiDocument.rootVisualElement.Q<Button>(btnName);
        if (btn != null && buttonRegistry.ContainsKey(btn))
        {
            var data = buttonRegistry[btn];
            data.isFocused = focus;

            // Re-apply the visuals. If focused, it stays at index 3 (Clicked)
            if (!data.interactable) SetState(data, 4);
            else if (data.isFocused) SetState(data, 3);
            else SetState(data, 1);
        }
    }

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
        }
    }

    public void OnDoorClicked(string doorName)
    {
        if (activeMode == ActionMode.LockDoor) DoorManager.Instance.OnDoorClicked(doorName);
    }

    // ==========================================
    // --- UI TOOLKIT VISUAL LOGIC ---
    // ==========================================

    public void UpdatePowerLevel(int newPower)
    {
        currentPowerLevel = Mathf.Clamp(newPower, 0, 10);
        for (int i = 0; i < powerIndicators.Count; i++)
        {
            powerIndicators[i].SetEnabled(i < currentPowerLevel);
        }

        foreach (var req in powerRequirements)
        {
            bool hasEnoughPower = currentPowerLevel >= req.requiredPower;
            bool extraConditionMet = CheckExtraGameConditions(req.buttonName);
            SetButtonEnabled(req.buttonName, hasEnoughPower && extraConditionMet);
        }
    }

    private bool CheckExtraGameConditions(string btnName)
    {
        if (GameManager.Instance == null || PlayerActionManager.Instance == null) return true;
        return btnName switch
        {
            "BTN8" => Alien.Instance == null || !Alien.Instance.IsReleased(),
            "BTN6" => Robot.Instance == null || !Robot.Instance.IsHacked(),
            "BTN4" => FireSpread.Instance == null || !FireSpread.Instance.IsFireActive(),
            "BTN9" => !GameManager.Instance.IsO2Triggered(),
            _ => true,
        };
    }

    private void OnHoverEnter(PointerEnterEvent evt, ButtonData data)
    {
        if (data.interactable && !data.isFocused) SetState(data, 2);
    }

    private void OnHoverExit(PointerLeaveEvent evt, ButtonData data)
    {
        if (data.interactable && !data.isFocused) SetState(data, 1);
    }

    private void OnClickDown(PointerDownEvent evt, ButtonData data)
    {
        if (evt.button == 0 && data.interactable) SetState(data, 3);
    }

    private void OnClickUp(PointerUpEvent evt, ButtonData data)
    {
        if (data.interactable && !data.isFocused) SetState(data, 2);
    }

    private void SetState(ButtonData data, int activeIndex)
    {
        if (activeIndex == 3)
        {
            SetFade(data.img2Hover, 0f);
            SetFade(data.img3Click, 0.05f);
        }
        else
        {
            SetFade(data.img2Hover, 0.5f);
            SetFade(data.img3Click, 0.5f);
        }

        data.img1Default.style.opacity = 1f;
        data.img2Hover.style.opacity = (activeIndex == 2) ? 1f : 0f;
        data.img3Click.style.opacity = (activeIndex == 3) ? 1f : 0f;
        data.img4Disabled.style.opacity = (activeIndex == 4) ? 1f : 0f;
    }

    public void SetButtonEnabled(string exactButtonName, bool isEnabled)
    {
        var root = uiDocument.rootVisualElement;
        var btn = root.Q<Button>(exactButtonName);

        if (btn != null && buttonRegistry.ContainsKey(btn))
        {
            var data = buttonRegistry[btn];
            data.interactable = isEnabled;

            // Ensure the state respects if the button is currently focused or not
            SetState(data, isEnabled ? (data.isFocused ? 3 : 1) : 4);
        }
    }

    private void SetFade(VisualElement element, float duration)
    {
        element.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName> { new("opacity") });
        element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new(duration, TimeUnit.Second) });
    }
}