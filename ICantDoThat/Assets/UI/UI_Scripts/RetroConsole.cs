using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public enum ConsoleLogType { Standard, Error, VoiceLine, Flavor, DevInfo }

public struct ConsoleMessage
{
    public string text;
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

    [Header("Console Text Colors")]
    public Color standardColor = new Color(0.78f, 0.78f, 0.78f);
    public Color errorColor = new Color(1f, 0.2f, 0.2f);
    public Color voiceColor = new Color(0.4f, 0.78f, 1f);
    public Color flavorColor = new Color(1f, 0.78f, 0.2f);
    public Color devColor = Color.white;

    // --- PORTRAIT FIELDS ---
    [Header("System Default")]
    public Texture2D staticScreen; // 0.0

    [Header("Captain Portraits (1.x)")]
    public Texture2D captNeutral; // 1.1
    public Texture2D captFearful; // 1.2
    public Texture2D captHappy;   // 1.3
    public Texture2D captAngry;   // 1.4

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
            // Forces the UI Toolkit to measure text height so it doesn't overlap!
            consoleListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            consoleListView.itemsSource = consoleLogs;

            consoleListView.makeItem = () =>
            {
                var label = new Label();
                label.style.whiteSpace = WhiteSpace.Normal;

                // Adds breathing room between the lines
                label.style.marginTop = 10;
                label.style.marginBottom = 10;

                return label;
            };

            consoleListView.bindItem = (element, index) =>
            {
                if (element is Label label)
                {
                    var msg = consoleLogs[index];
                    label.text = msg.text;

                    label.ClearClassList();
                    label.AddToClassList("console-text-base");
                    label.style.fontSize = new StyleLength(fontSize);

                    switch (msg.type)
                    {
                        case ConsoleLogType.Standard:
                            label.AddToClassList("console-standard");
                            label.style.color = standardColor; break;
                        case ConsoleLogType.Error:
                            label.AddToClassList("console-error");
                            label.style.color = errorColor; break;
                        case ConsoleLogType.VoiceLine:
                            label.AddToClassList("console-voice");
                            label.style.color = voiceColor; break;
                        case ConsoleLogType.Flavor:
                            label.AddToClassList("console-flavor");
                            label.style.color = flavorColor; break;
                        case ConsoleLogType.DevInfo:
                            label.AddToClassList("console-dev");
                            label.style.color = devColor; break;
                    }
                }
            };
        }
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (!showDevLogs) return;

        ConsoleLogType customType = ConsoleLogType.DevInfo;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) customType = ConsoleLogType.Error;
        else if (type == LogType.Warning) customType = ConsoleLogType.Flavor;

        Log($"[DEV] {logString}", customType, "");
    }

    public void Log(string message, ConsoleLogType logType = ConsoleLogType.Standard, string portraitID = "0.0")
    {
        string formattedMessage = logType switch
        {
            ConsoleLogType.VoiceLine => $"[COMMS] {message}",
            ConsoleLogType.Error => $"[ERR] {message}",
            ConsoleLogType.Flavor => $"> {message}",
            ConsoleLogType.DevInfo => message,
            _ => $"sys:// {message}"
        };

        consoleLogs.Add(new ConsoleMessage { text = formattedMessage, type = logType });

        if (consoleListView != null)
        {
            consoleListView.RefreshItems();
            consoleListView.ScrollToItem(consoleLogs.Count - 1);
        }

        UpdatePortrait(portraitID);
    }

    public void LogError(string msg) => Log(msg, ConsoleLogType.Error, "0.0");
    public void LogVoice(string msg, string portraitID) => Log(msg, ConsoleLogType.VoiceLine, portraitID);

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