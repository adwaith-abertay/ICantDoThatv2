using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

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
    public bool enableComics = true;
    public bool allowInputToSkip = true;

    [Range(0.5f, 10f)]
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
    private float timeSinceLastPanel = 0f;

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
        if (!enableComics || comicPanel == null) return;

        // Route the death cause to the correct dictionary
        DeathMethodComics selectedMethod = deathCause.ToLower() switch
        {
            "airlock" => airlockDeaths,
            "fire" => fireDeaths,
            "hack" => robotHackDeaths,
            "alien" => alienDeaths,
            _ => null
        };

        if (selectedMethod == null) return;

        // Fetch the specific sequence for the character who died
        activeSequence = selectedMethod.GetSequenceFor(characterType);

        if (activeSequence == null || activeSequence.panels == null || activeSequence.panels.Count == 0)
        {
            Debug.LogWarning($"[ComicManager] No panels found for {characterType} killed by {deathCause}");
            return;
        }

        currentPanelIndex = 0;
        timeSinceLastPanel = 0f;
        isShowingComic = true;

        UpdatePanelDisplay();
        comicPanel.style.display = DisplayStyle.Flex;
    }

    private void Update()
    {
        if (!isShowingComic) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndComic();
            return;
        }

        bool shouldAdvance = false;

        if (allowInputToSkip)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) shouldAdvance = true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) shouldAdvance = true;
        }

        if (!shouldAdvance)
        {
            timeSinceLastPanel += Time.deltaTime;
            if (timeSinceLastPanel >= autoplayTimer) shouldAdvance = true;
        }

        if (shouldAdvance) AdvanceComic();
    }

    private void AdvanceComic()
    {
        currentPanelIndex++;
        timeSinceLastPanel = 0f;

        if (currentPanelIndex < activeSequence.panels.Count)
        {
            UpdatePanelDisplay();
        }
        else
        {
            EndComic();
        }
    }

    private void UpdatePanelDisplay()
    {
        if (activeSequence.panels[currentPanelIndex] != null)
        {
            comicPanel.style.backgroundImage = new StyleBackground(activeSequence.panels[currentPanelIndex]);
        }
    }

    public void EndComic()
    {
        isShowingComic = false;
        activeSequence = null;
        if (comicPanel != null) comicPanel.style.display = DisplayStyle.None;
    }
}