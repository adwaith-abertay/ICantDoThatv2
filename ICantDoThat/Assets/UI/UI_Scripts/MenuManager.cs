using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    [Tooltip("Drag your separate UI Document GameObjects here")]
    public UIDocument hudDocument;
    public UIDocument mainMenuDocument;
    public UIDocument pauseMenuDocument;

    private bool _isPaused = false;

    void Start()
    {
        // Bind the buttons for the main menu
        var mainStartBtn = mainMenuDocument.rootVisualElement.Q<Button>("StartButton");
        if (mainStartBtn != null) mainStartBtn.clicked += ResumeGame;

        var mainQuitBtn = mainMenuDocument.rootVisualElement.Q<Button>("QuitButton");
        if (mainQuitBtn != null) mainQuitBtn.clicked += Application.Quit;

        // Bind the buttons for the pause menu
        var pauseResumeBtn = pauseMenuDocument.rootVisualElement.Q<Button>("ResumeButton");
        if (pauseResumeBtn != null) pauseResumeBtn.clicked += ResumeGame;

        var pauseQuitBtn = pauseMenuDocument.rootVisualElement.Q<Button>("QuitButton");
        if (pauseQuitBtn != null) pauseQuitBtn.clicked += Application.Quit;

        // Start the game sitting at the main menu
        ShowMainMenu();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ah, we probably shouldn't trigger the pause menu if the main menu is already open
            if (mainMenuDocument.rootVisualElement.style.display == DisplayStyle.Flex) return;

            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f; // Freeze game logic

        // Hide HUD and Main Menu, show Pause Menu
        hudDocument.rootVisualElement.style.display = DisplayStyle.None;
        mainMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f; // Unfreeze game logic

        // Hide Menus, show HUD
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        mainMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        hudDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void ShowMainMenu()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        // Hide HUD and Pause Menu, show Main Menu
        hudDocument.rootVisualElement.style.display = DisplayStyle.None;
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        mainMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }
}