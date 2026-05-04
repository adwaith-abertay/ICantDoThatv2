using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Documents")]
    public UIDocument mainMenuDocument;
    public UIDocument tutorialDocument;
    public UIDocument futureMenu1Document;
    public UIDocument futureMenu2Document;

    [Header("Button Element Names")]
    public string startBtnName = "StartButton";
    public string quitBtnName = "QuitButton";
    public string tutorialBtnName = "TutorialButton";
    public string futureMenu1BtnName = "FutureMenu1Button";
    public string futureMenu2BtnName = "FutureMenu2Button";
    public string backBtnName = "BackButton";

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;

    [Header("Settings")]
    public string gameSceneName = "GameScene";

    void Start()
    {
        Debug.Log("[MainMenuManager] Starting initialization...");
        Time.timeScale = 1f;

        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            if (!musicSource.isPlaying)
            {
                Debug.Log("[MainMenuManager] Playing background music.");
                musicSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("[MainMenuManager] Music source or clip is missing.");
        }

        // Disable the other UI docs on start
        if (tutorialDocument != null) tutorialDocument.rootVisualElement.style.display = DisplayStyle.None;
        if (futureMenu1Document != null) futureMenu1Document.rootVisualElement.style.display = DisplayStyle.None;
        if (futureMenu2Document != null) futureMenu2Document.rootVisualElement.style.display = DisplayStyle.None;

        if (mainMenuDocument == null)
        {
            Debug.LogError("[MainMenuManager] Main Menu UIDocument is null!");
            return;
        }

        var mainRoot = mainMenuDocument.rootVisualElement;

        var startBtn = mainRoot.Q<Button>(startBtnName);
        var quitBtn = mainRoot.Q<Button>(quitBtnName);
        var tutorialBtn = mainRoot.Q<Button>(tutorialBtnName);
        var futureMenu1Btn = mainRoot.Q<Button>(futureMenu1BtnName);
        var futureMenu2Btn = mainRoot.Q<Button>(futureMenu2BtnName);

        // Bind Start
        if (startBtn != null)
        {
            startBtn.clicked += () => {
                Debug.Log($"[MainMenuManager] Start Button clicked. Loading scene: {gameSceneName}");
                SceneManager.LoadScene(gameSceneName);
            };
        }
        else Debug.LogWarning($"[MainMenuManager] '{startBtnName}' not found.");

        // Bind Quit
        if (quitBtn != null)
        {
            quitBtn.clicked += () => {
                Debug.Log("[MainMenuManager] Quit Button clicked. Exiting application.");
                Application.Quit();
            };
        }
        else Debug.LogWarning($"[MainMenuManager] '{quitBtnName}' not found.");

        // Bind Sub-menus
        if (tutorialBtn != null) tutorialBtn.clicked += () => OpenMenu(tutorialDocument, "Tutorial");
        if (futureMenu1Btn != null) futureMenu1Btn.clicked += () => OpenMenu(futureMenu1Document, "FutureMenu1");
        if (futureMenu2Btn != null) futureMenu2Btn.clicked += () => OpenMenu(futureMenu2Document, "FutureMenu2");

        // Bind Back Buttons
        BindBackButton(tutorialDocument, "Tutorial");
        BindBackButton(futureMenu1Document, "FutureMenu1");
        BindBackButton(futureMenu2Document, "FutureMenu2");

        Debug.Log("[MainMenuManager] Initialization complete.");
    }

    private void OpenMenu(UIDocument menuToOpen, string menuLogName)
    {
        Debug.Log($"[MainMenuManager] Opening Sub-Menu: {menuLogName}");

        if (menuToOpen == null || mainMenuDocument == null)
        {
            Debug.LogError($"[MainMenuManager] Failed to open {menuLogName} - missing document reference.");
            return;
        }

        mainMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        menuToOpen.rootVisualElement.style.display = DisplayStyle.Flex;

        if (menuToOpen == tutorialDocument && TutorialManager.Instance != null)
        {
            Debug.Log("[MainMenuManager] Triggering TutorialManager.OpenTutorial()");
            TutorialManager.Instance.OpenTutorial();
        }
    }

    private void BindBackButton(UIDocument subMenu, string menuLogName)
    {
        if (subMenu == null) return;

        var backBtn = subMenu.rootVisualElement.Q<Button>(backBtnName);
        if (backBtn != null)
        {
            backBtn.clicked += () => CloseMenu(subMenu, menuLogName);
        }
        else
        {
            Debug.LogWarning($"[MainMenuManager] Could not find back button '{backBtnName}' in {subMenu.name} ({menuLogName})");
        }
    }

    private void CloseMenu(UIDocument menuToClose, string menuLogName)
    {
        Debug.Log($"[MainMenuManager] Closing Sub-Menu: {menuLogName} and returning to Main Menu.");

        if (menuToClose == null || mainMenuDocument == null) return;

        menuToClose.rootVisualElement.style.display = DisplayStyle.None;
        mainMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }
}