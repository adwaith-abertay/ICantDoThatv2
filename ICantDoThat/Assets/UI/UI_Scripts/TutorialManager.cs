using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Data")]
    [Tooltip("Add your tutorial slide images here in order.")]
    public List<Texture2D> tutorialSlides = new List<Texture2D>();

    [Header("UI Setup")]
    public UIDocument uiDocument;

    [Tooltip("The names of your elements in the UI Builder")]
    public string containerName = "TutorialContainer";
    public string slidePanelName = "SlidePanel";
    public string nextBtnName = "NextButton";
    public string prevBtnName = "PrevButton";
    public string closeBtnName = "CloseButton";

    private VisualElement tutorialContainer;
    private VisualElement slidePanel;
    private Button nextBtn;
    private Button prevBtn;
    private Button closeBtn;

    private int currentSlideIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[TutorialManager] No UIDocument assigned!");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // Query all the UI elements
        tutorialContainer = root.Q<VisualElement>(containerName);
        slidePanel = root.Q<VisualElement>(slidePanelName);
        nextBtn = root.Q<Button>(nextBtnName);
        prevBtn = root.Q<Button>(prevBtnName);
        closeBtn = root.Q<Button>(closeBtnName);

        // Debug missing elements
        if (tutorialContainer == null) Debug.LogWarning($"[TutorialManager] Container '{containerName}' not found.");
        if (slidePanel == null) Debug.LogWarning($"[TutorialManager] Slide Panel '{slidePanelName}' not found.");

        // Bind button clicks
        if (nextBtn != null) nextBtn.clicked += NextSlide;
        else Debug.LogWarning($"[TutorialManager] Next Button '{nextBtnName}' not found.");

        if (prevBtn != null) prevBtn.clicked += PrevSlide;
        else Debug.LogWarning($"[TutorialManager] Prev Button '{prevBtnName}' not found.");

        if (closeBtn != null) closeBtn.clicked += CloseTutorial;
        else Debug.LogWarning($"[TutorialManager] Close Button '{closeBtnName}' not found.");

        // Hide the tutorial by default on startup
        if (tutorialContainer != null) tutorialContainer.style.display = DisplayStyle.None;

        Debug.Log("[TutorialManager] Initialized successfully.");
    }

    public void OpenTutorial()
    {
        Debug.Log("[TutorialManager] Opening Tutorial...");

        if (tutorialSlides == null || tutorialSlides.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] Cannot open: No slides assigned in the inspector!");
            return;
        }

        currentSlideIndex = 0;
        UpdateSlideDisplay();

        if (tutorialContainer != null) tutorialContainer.style.display = DisplayStyle.Flex;
    }

    public void CloseTutorial()
    {
        Debug.Log("[TutorialManager] Closing Tutorial.");
        if (tutorialContainer != null) tutorialContainer.style.display = DisplayStyle.None;
    }

    private void NextSlide()
    {
        if (currentSlideIndex < tutorialSlides.Count - 1)
        {
            currentSlideIndex++;
            Debug.Log($"[TutorialManager] Next Slide clicked. Moving to slide {currentSlideIndex + 1}/{tutorialSlides.Count}");
            UpdateSlideDisplay();
        }
    }

    private void PrevSlide()
    {
        if (currentSlideIndex > 0)
        {
            currentSlideIndex--;
            Debug.Log($"[TutorialManager] Prev Slide clicked. Moving to slide {currentSlideIndex + 1}/{tutorialSlides.Count}");
            UpdateSlideDisplay();
        }
    }

    private void UpdateSlideDisplay()
    {
        if (slidePanel != null && tutorialSlides[currentSlideIndex] != null)
        {
            slidePanel.style.backgroundImage = new StyleBackground(tutorialSlides[currentSlideIndex]);
            Debug.Log($"[TutorialManager] Displaying image for slide {currentSlideIndex}.");
        }
        else if (tutorialSlides[currentSlideIndex] == null)
        {
            Debug.LogError($"[TutorialManager] Slide at index {currentSlideIndex} is null!");
        }

        if (prevBtn != null) prevBtn.SetEnabled(currentSlideIndex > 0);
        if (nextBtn != null) nextBtn.SetEnabled(currentSlideIndex < tutorialSlides.Count - 1);
    }
}