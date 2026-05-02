using UnityEngine;

using UnityEngine.UIElements;

using System.Collections.Generic;

using UnityEngine.InputSystem;

using System.Collections; // Needed for IEnumerator



public class SIIDUIManager : MonoBehaviour

{

    public static SIIDUIManager Instance;



    public enum ActionMode

    {

        None,

        Fear,

        GreaterFear,

        LockDoor

    }



    [Header("Inspector Setup")]

    public UIDocument uiDocument;



    [Header("Button Art (Drag your PNGs here)")]

    public Texture2D defaultArt;

    public Texture2D hoverArt;

    public Texture2D clickArt;

    public Texture2D disabledArt;



    // --- NEW POWER SYSTEM SETUP ---

    [System.Serializable]

    public struct ButtonPowerRequirement

    {

        [Tooltip("The exact name of the button in UI Builder (e.g., BTN1)")]

        public string buttonName;

        [Tooltip("How much power is needed for this button to be active")]

        public int requiredPower;

    }



    [Header("Power Settings")]

    public List<ButtonPowerRequirement> powerRequirements = new();



    // Internal state

    private int currentPowerLevel = 10;

    private List<VisualElement> powerIndicators = new();

    private ActionMode activeMode = ActionMode.None;

    // ------------------------------



    private class ButtonData

    {

        public VisualElement img1Default;

        public VisualElement img2Hover;

        public VisualElement img3Click;

        public VisualElement img4Disabled;

        public bool interactable = true;

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



        // 2. Setup the Power Indicators (SiidPower1 through SiidPower10)

        powerIndicators.Clear();

        for (int i = 1; i <= 10; i++)

        {

            var indicator = root.Q<VisualElement>($"SiidPower{i}");

            if (indicator != null)

            {

                powerIndicators.Add(indicator);

            }

        }



        // ==========================================

        // --- GAME LOGIC HOOKUPS ---

        // ==========================================



        // Grab the specific buttons by their names in UI Builder

        var fearBtn = root.Q<Button>("BTN1");

        var lockDoorBtn = root.Q<Button>("BTN2");

        var greaterFearBtn = root.Q<Button>("BTN3");

        var startFireBtn = root.Q<Button>("BTN4");

        var hackRobotBtn = root.Q<Button>("BTN6");

        var unfreezeAlienBtn = root.Q<Button>("BTN8");

        var cutO2Btn = root.Q<Button>("BTN9");

        var endTurnBtn = root.Q<Button>("BTN0");



        if (fearBtn != null) fearBtn.clicked += () => { if (buttonRegistry[fearBtn].interactable) ToggleMode(ActionMode.Fear); };

        if (greaterFearBtn != null) greaterFearBtn.clicked += () => { if (buttonRegistry[greaterFearBtn].interactable) ToggleMode(ActionMode.GreaterFear); };

        if (lockDoorBtn != null) lockDoorBtn.clicked += () => { if (buttonRegistry[lockDoorBtn].interactable) ToggleMode(ActionMode.LockDoor); };



        if (unfreezeAlienBtn != null) unfreezeAlienBtn.clicked += () =>

        {

            if (buttonRegistry[unfreezeAlienBtn].interactable)

            {

                PlayerActionManager.Instance.ActionReleaseAlien();

                // Power level visually updates automatically in Update() now

            }

        };



        if (hackRobotBtn != null) hackRobotBtn.clicked += () =>

        {

            if (buttonRegistry[hackRobotBtn].interactable)

            {

                PlayerActionManager.Instance.ActionHackRobot();

            }

        };



        if (startFireBtn != null) startFireBtn.clicked += () =>

        {

            if (buttonRegistry[startFireBtn].interactable) PlayerActionManager.Instance.ActionStartFire();

        };



        if (cutO2Btn != null) cutO2Btn.clicked += () =>

        {

            if (buttonRegistry[cutO2Btn].interactable) PlayerActionManager.Instance.ActionCutO2();

        };



        if (endTurnBtn != null) endTurnBtn.clicked += () =>

        {

            if (buttonRegistry[endTurnBtn].interactable)

            {

                CancelMode();

                DoorManager.Instance.OnTurnEnd();

                GameManager.Instance.EndPlayerTurn();

            }

        };



        // Initialize everything to max power visually to start

        UpdatePowerLevel(10);

    }



    void Update()

    {

        // Automatically sync the UI power level with the game manager's energy

        if (PlayerActionManager.Instance != null)

        {

            int realEnergy = PlayerActionManager.Instance.GetEnergy(); // Assuming GetEnergy() is the method from your old script



            if (currentPowerLevel != realEnergy)

            {

                UpdatePowerLevel(realEnergy);

            }

        }



        if (Keyboard.current == null) return;



        // Press P to toggle the whole board (Debug)

        if (Keyboard.current.pKey.wasPressedThisFrame)

        {

            isDebugEnabled = !isDebugEnabled;

            foreach (var kvp in buttonRegistry)

            {

                var data = kvp.Value;

                data.interactable = isDebugEnabled;

                SetState(data, isDebugEnabled ? 1 : 4);

            }

        }

    }



    // ==========================================

    // --- MODE LOGIC FROM OLD SCRIPT ---

    // ==========================================



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

        }

    }



    public void CancelMode()

    {

        if (activeMode == ActionMode.LockDoor)

            DoorManager.Instance.OnTurnEnd();



        activeMode = ActionMode.None;

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



            case ActionMode.LockDoor:

                Debug.Log("Door mode active — click a door sprite, not a crew member.");

                break;

        }

    }



    public void OnDoorClicked(string doorName)

    {

        if (activeMode != ActionMode.LockDoor) return;



        DoorManager.Instance.OnDoorClicked(doorName);

    }



    // ==========================================

    // --- UI TOOLKIT VISUAL LOGIC ---

    // ==========================================



    public void UpdatePowerLevel(int newPower)

    {

        currentPowerLevel = Mathf.Clamp(newPower, 0, 10);



        // 1. Toggle the power indicator visuals

        for (int i = 0; i < powerIndicators.Count; i++)

        {

            powerIndicators[i].SetEnabled(i < currentPowerLevel);

        }



        // 2. Loop through the inspector requirements and toggle buttons

        foreach (var req in powerRequirements)

        {

            bool hasEnoughPower = currentPowerLevel >= req.requiredPower;



            // Hmmm, we also need to make sure we don't enable buttons that are one-time uses (like releasing the alien)

            bool extraConditionMet = CheckExtraGameConditions(req.buttonName);



            SetButtonEnabled(req.buttonName, hasEnoughPower && extraConditionMet);

        }

    }



    // This handles the extra logic from your old RefreshButtons method

    private bool CheckExtraGameConditions(string btnName)

    {

        if (GameManager.Instance == null || PlayerActionManager.Instance == null) return true;



        return btnName switch

        {

            // Unfreeze Alien

            "BTN8" => Alien.Instance == null || !Alien.Instance.IsReleased(),

            // Hack Robot

            "BTN6" => Robot.Instance == null || !Robot.Instance.IsHacked(),

            // Start Fire

            "BTN4" => FireSpread.Instance == null || !FireSpread.Instance.IsFireActive(),

            // Cut O2

            "BTN9" => !GameManager.Instance.IsO2Triggered(),

            _ => true,

        };

    }



    private void OnHoverEnter(PointerEnterEvent evt, ButtonData data)

    {

        if (data.interactable) SetState(data, 2);

    }



    private void OnHoverExit(PointerLeaveEvent evt, ButtonData data)

    {

        if (data.interactable) SetState(data, 1);

    }



    private void OnClickDown(PointerDownEvent evt, ButtonData data)

    {

        if (evt.button == 0 && data.interactable) SetState(data, 3);

    }



    private void OnClickUp(PointerUpEvent evt, ButtonData data)

    {

        if (data.interactable) SetState(data, 2);

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



    private void SetFade(VisualElement element, float duration)

    {

        element.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName> { new("opacity") });

        element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new(duration, TimeUnit.Second) });

    }



    public void SetButtonEnabled(string exactButtonName, bool isEnabled)

    {

        var root = uiDocument.rootVisualElement;

        var btn = root.Q<Button>(exactButtonName);



        if (btn != null && buttonRegistry.ContainsKey(btn))

        {

            var data = buttonRegistry[btn];

            data.interactable = isEnabled;

            SetState(data, isEnabled ? 1 : 4);

        }

    }

}