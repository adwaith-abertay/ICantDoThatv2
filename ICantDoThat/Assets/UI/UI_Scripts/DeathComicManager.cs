using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

// 1. Updated to use a dynamic list of panels!
[Serializable]
public class ComicSequence
{
    [Tooltip("Add as many or as few panels as you need for this specific death.")]
    public List<Texture2D> panels = new List<Texture2D>();
}

[Serializable]
public class DeathMethodComics
{
    public ComicSequence captain;
    public ComicSequence soldier;
    public ComicSequence injuredSoldier;
    public ComicSequence engineer;
    public ComicSequence scientist;
    public ComicSequence robot;

    public ComicSequence GetSequenceFor(string characterType) => characterType.ToLower() switch
    {
        "captain" => captain,
        "soldier" => soldier,
        "injured" => injuredSoldier,
        "engineer" => engineer,
        "scientist" => scientist,
        "robot" => robot,
        _ => null
    };
}

public class DeathComicManager : MonoBehaviour
{
    public static DeathComicManager Instance { get; private set; }

    [Header("Global Settings")]
    [Tooltip("Uncheck this to completely disable death comics (for a future Settings Menu).")]
    public bool enableComics = true;

    [Tooltip("If true, clicking or pressing Space skips to the next panel immediately.")]
    public bool allowInputToSkip = true;

    [Range(0.5f, 10f)]
    [Tooltip("How many seconds to wait before automatically advancing to the next panel.")]
    public float autoplayTimer = 2.0f;

    [Header("UI Setup")]
    public UIDocument uiDocument;
    public string panelName = "ComicPanel";

    [Header("Death Methods")]
    public DeathMethodComics airlockDeaths;
    public DeathMethodComics fireDeaths;
    public DeathMethodComics robotHackDeaths;
    public DeathMethodComics alienDeaths;

    private VisualElement comicPanel;
    private bool isShowingComic = false;
    private int currentPanelIndex = 0;
    private ComicSequence activeSequence;
    private float timeSinceLastPanel = 0f; // Tracks the autoplay timer

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            comicPanel = uiDocument.rootVisualElement.Q<VisualElement>(panelName);
            if (comicPanel != null) comicPanel.style.display = DisplayStyle.None;
        }
    }

    public void PlayComic(string characterType, string deathCause)
    {
        // 1. Master Toggle Check
        if (!enableComics || comicPanel == null) return;

        DeathMethodComics selectedMethod = deathCause.ToLower() switch
        {
            "airlock" => airlockDeaths,
            "fire" => fireDeaths,
            "hack" => robotHackDeaths,
            "alien" => alienDeaths,
            _ => null
        };

        if (selectedMethod == null) return;

        activeSequence = selectedMethod.GetSequenceFor(characterType);

        // 2. Safety Check: Make sure they actually added panels to the list!
        if (activeSequence == null || activeSequence.panels == null || activeSequence.panels.Count == 0) return;

        currentPanelIndex = 0;
        timeSinceLastPanel = 0f; // Reset the timer
        isShowingComic = true;

        comicPanel.style.backgroundImage = new StyleBackground(activeSequence.panels[currentPanelIndex]);
        comicPanel.style.display = DisplayStyle.Flex;
    }

    private void Update()
    {
        if (!isShowingComic) return;

        // 1. ESCAPE KEY (Ends the comic immediately)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndComic();
            return;
        }

        bool shouldAdvance = false;

        // 2. INPUT SKIPPING (Left Click or Spacebar)
        if (allowInputToSkip)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) shouldAdvance = true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) shouldAdvance = true;
        }

        // 3. AUTOPLAY TIMER
        if (!shouldAdvance)
        {
            timeSinceLastPanel += Time.deltaTime;
            if (timeSinceLastPanel >= autoplayTimer)
            {
                shouldAdvance = true;
            }
        }

        // Advance if either the timer popped or the player clicked
        if (shouldAdvance)
        {
            AdvanceComic();
        }
    }

    private void AdvanceComic()
    {
        currentPanelIndex++;
        timeSinceLastPanel = 0f; // Reset the timer for the new panel

        // Check against the dynamic list length instead of a hardcoded 4
        if (currentPanelIndex < activeSequence.panels.Count)
        {
            comicPanel.style.backgroundImage = new StyleBackground(activeSequence.panels[currentPanelIndex]);
        }
        else
        {
            EndComic();
        }
    }

    public void EndComic()
    {
        isShowingComic = false;
        activeSequence = null;
        if (comicPanel != null) comicPanel.style.display = DisplayStyle.None;
    }
}