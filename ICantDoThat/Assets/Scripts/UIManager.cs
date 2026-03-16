using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject winPopup;
    public Text winText;

    private void Awake()
    {
        Instance = this;
        winPopup.SetActive(false);
    }

    public void ShowCrewWin(string role)
    {
        winText.text = $"Crew Wins!\n{role} reached the destination!";
        winPopup.SetActive(true);
    }
}
