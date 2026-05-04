using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public enum ConsoleLogType { Standard, Error, VoiceLine, Flavor, DevInfo, PlayerAid }

public class ConsoleMessage
{
    public string fullText;
    public string displayedText;
    public ConsoleLogType type;
}

public class RetroConsole : MonoBehaviour
{
    public static RetroConsole Instance { get; private set; }

    [Header("Inspector Setup")]
    public UIDocument uiDocument;
    public string portraitElementName = "PortraitDisplay";

    [Header("Developer Settings")]
    public bool showDevLogs = true;
    [Range(10, 100)] public int fontSize = 35;

    public Font customFont;

    [Header("Typewriter Settings")]
    public bool useTypewriterEffect = true;
    [Range(0.001f, 0.2f)] public float typeSpeed = 0.02f;

    // --- UPDATED: Generalized Delay Settings ---
    [Header("Event Settings")]
    [Tooltip("Delay applied to Unity events and Voice lines to prevent them from jumping the queue.")]
    [Range(0.0f, 2.0f)] public float eventLogDelay = 0.2f;

    [Header("Console Text Colors")]
    public Color standardColor = new Color(0.78f, 0.78f, 0.78f);
    public Color errorColor = new Color(1f, 0.2f, 0.2f);
    public Color voiceColor = new Color(0.4f, 0.78f, 1f);
    public Color flavorColor = new Color(1f, 0.78f, 0.2f);
    public Color devColor = Color.white;
    public Color playerAidColor = new Color(0.6f, 1f, 0.6f);

    // --- PORTRAIT FIELDS ---
    [Header("System Default")]
    public Texture2D staticScreen;

    [Header("Captain Portraits (1.x)")]
    public Texture2D captNeutral;
    public Texture2D captFearful;
    public Texture2D captHappy;
    public Texture2D captAngry;

    [Header("Soldier Portraits (2.x)")]
    public Texture2D soldierNeutral;
    public Texture2D soldierFearful;
    public Texture2D soldierHappy;
    public Texture2D soldierAngry;

    [Header("Soldier INJURED (3.x)")]
    public Texture2D injuredNeutral;
    public Texture2D injuredFearful;
    public Texture2D injuredHappy;
    public Texture2D injuredAngry;

    [Header("Robot Portraits (4.x)")]
    public Texture2D robotNormal;
    public Texture2D robotHacked;

    [Header("Player Portrait (5.x)")]
    public Texture2D playerImage;

    [Header("Engineer Portraits (6.x)")]
    public Texture2D engNeutral;
    public Texture2D engFearful;
    public Texture2D engHappy;
    public Texture2D engAngry;

    [Header("Scientist Portraits (7.x)")]
    public Texture2D sciNeutral;
    public Texture2D sciFearful;
    public Texture2D sciHappy;
    public Texture2D sciAngry;
    // ---------------------------

    private ListView consoleListView;
    private VisualElement portraitElement;
    private readonly List<ConsoleMessage> consoleLogs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleUnityLog;

        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        consoleListView = root.Q<ListView>("SIIDConsole");
        portraitElement = root.Q<VisualElement>(portraitElementName);

        if (consoleListView != null)
        {
            consoleListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            consoleListView.itemsSource = consoleLogs;

            consoleListView.makeItem = () =>
            {
                var label = new Label();
                label.enableRichText = true;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginTop = 10;
                label.style.marginBottom = 10;
                return label;
            };

            consoleListView.bindItem = (element, index) =>
            {
                if (element is Label label)
                {
                    var msg = consoleLogs[index];
                    label.text = msg.displayedText;

                    label.ClearClassList();
                    label.AddToClassList("console-text-base");
                    label.style.fontSize = new StyleLength(fontSize);

                    if (customFont != null)
                    {
                        label.style.unityFont = customFont;
                        label.style.unityFontDefinition = new StyleFontDefinition(StyleKeyword.None);
                    }

                    label.style.unityTextAlign = TextAnchor.UpperLeft;

                    switch (msg.type)
                    {
                        case ConsoleLogType.Standard:
                            label.AddToClassList("console-standard");
                            label.style.color = standardColor;
                            break;
                        case ConsoleLogType.Error:
                            label.AddToClassList("console-error");
                            label.style.color = errorColor;
                            break;
                        case ConsoleLogType.VoiceLine:
                            label.AddToClassList("console-voice");
                            label.style.color = voiceColor;
                            break;
                        case ConsoleLogType.Flavor:
                            label.AddToClassList("console-flavor");
                            label.style.color = flavorColor;
                            break;
                        case ConsoleLogType.DevInfo:
                            label.AddToClassList("console-dev");
                            label.style.color = devColor;
                            break;
                        case ConsoleLogType.PlayerAid:
                            label.AddToClassList("console-playeraid");
                            label.style.color = playerAidColor;
                            label.style.unityTextAlign = TextAnchor.MiddleCenter;
                            break;
                    }
                }
            };
        }
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    // --- UPDATED: Pushes the Unity log into the delayed queue ---
    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (!showDevLogs) return;

        ConsoleLogType customType = ConsoleLogType.DevInfo;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) customType = ConsoleLogType.Error;
        else if (type == LogType.Warning) customType = ConsoleLogType.Flavor;

        StartCoroutine(DelayedLogRoutine($"[DEV] {logString}", customType, ""));
    }

    public void Log(string message, ConsoleLogType logType = ConsoleLogType.Standard, string portraitID = "0.0")
    {
        string formattedMessage = logType switch
        {
            ConsoleLogType.VoiceLine => $"[COMMS] {message}",
            ConsoleLogType.Error => $"[ERR] {message}",
            ConsoleLogType.Flavor => $"> {message}",
            ConsoleLogType.DevInfo => message,
            ConsoleLogType.PlayerAid => $"--- {message} ---",
            _ => $"sys:// {message}"
        };

        var newMessage = new ConsoleMessage
        {
            fullText = formattedMessage,
            displayedText = useTypewriterEffect ? $"<color=#00000000>{formattedMessage}</color>" : formattedMessage,
            type = logType
        };

        consoleLogs.Add(newMessage);
        UpdatePortrait(portraitID);

        if (useTypewriterEffect)
        {
            StartCoroutine(TypeText(newMessage));
        }
        else if (consoleListView != null)
        {
            consoleListView.RefreshItems();
            consoleListView.ScrollToItem(consoleLogs.Count - 1);
        }
    }

    private IEnumerator TypeText(ConsoleMessage msg)
    {
        for (int i = 0; i <= msg.fullText.Length; i++)
        {
            string visiblePart = msg.fullText.Substring(0, i);
            string hiddenPart = msg.fullText.Substring(i);

            msg.displayedText = $"{visiblePart}<color=#00000000>{hiddenPart}</color>";

            if (consoleListView != null)
            {
                consoleListView.RefreshItems();
                consoleListView.ScrollToItem(consoleLogs.Count - 1);
            }

            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public void LogError(string msg) => Log(msg, ConsoleLogType.Error, "0.0");

    // --- UPDATED: Uses the generalized delay routine ---
    public void LogVoice(string msg, string portraitID)
    {
        StartCoroutine(DelayedLogRoutine(msg, ConsoleLogType.VoiceLine, portraitID));
    }

    // --- NEW: Generalized delay coroutine for any event-driven log ---
    private IEnumerator DelayedLogRoutine(string msg, ConsoleLogType type, string portraitID)
    {
        // We check if the delay is actually greater than 0 before yielding to avoid a 1-frame skip if it's set to 0.
        if (eventLogDelay > 0)
        {
            yield return new WaitForSeconds(eventLogDelay);
        }

        Log(msg, type, portraitID);
    }

    private void UpdatePortrait(string portraitID)
    {
        if (portraitElement == null) return;
        if (string.IsNullOrEmpty(portraitID)) return;

        Texture2D selectedImage = portraitID switch
        {
            "1.1" => captNeutral,
            "1.2" => captFearful,
            "1.3" => captHappy,
            "1.4" => captAngry,
            "2.1" => soldierNeutral,
            "2.2" => soldierFearful,
            "2.3" => soldierHappy,
            "2.4" => soldierAngry,
            "3.1" => injuredNeutral,
            "3.2" => injuredFearful,
            "3.3" => injuredHappy,
            "3.4" => injuredAngry,
            "4.1" => robotNormal,
            "4.2" => robotHacked,
            "5.0" => playerImage,
            "6.1" => engNeutral,
            "6.2" => engFearful,
            "6.3" => engHappy,
            "6.4" => engAngry,
            "7.1" => sciNeutral,
            "7.2" => sciFearful,
            "7.3" => sciHappy,
            "7.4" => sciAngry,
            _ => staticScreen
        };

        if (selectedImage != null)
        {
            portraitElement.style.backgroundImage = new StyleBackground(selectedImage);
        }
    }
}