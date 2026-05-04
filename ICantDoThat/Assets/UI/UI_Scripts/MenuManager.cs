using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Tooltip("Drag your separate UI Document GameObjects here")]
    public UIDocument hudDocument;
    public UIDocument pauseMenuDocument;

    [Tooltip("The exact name of your Main Menu scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool _isPaused = false;

    void Start()
    {
        if (pauseMenuDocument == null || hudDocument == null) return;

        var pauseRoot = pauseMenuDocument.rootVisualElement;

        // Bind the buttons for the pause menu[cite: 7]
        var pauseResumeBtn = pauseRoot.Q<Button>("ResumeButton");
        if (pauseResumeBtn != null) pauseResumeBtn.clicked += ResumeGame;

        var pauseQuitToMenuBtn = pauseRoot.Q<Button>("QuitToMenuButton");
        if (pauseQuitToMenuBtn != null) pauseQuitToMenuBtn.clicked += QuitToMainMenu;

        var pauseQuitBtn = pauseRoot.Q<Button>("QuitButton");
        if (pauseQuitBtn != null) pauseQuitBtn.clicked += Application.Quit;

        // Start the game unpaused and with the HUD visible[cite: 7]
        ResumeGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f; // Freeze game logic[cite: 7]

        // Hide HUD, show Pause Menu[cite: 7]
        hudDocument.rootVisualElement.style.display = DisplayStyle.None;
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f; // Unfreeze game logic[cite: 7]

        // Hide Pause Menu, show HUD[cite: 7]
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        hudDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void QuitToMainMenu()
    {
        // CRITICAL: Always reset the time scale to 1 before changing scenes.[cite: 7]
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}