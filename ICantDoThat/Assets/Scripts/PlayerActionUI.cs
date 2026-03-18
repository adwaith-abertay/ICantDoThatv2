using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerActionUI : MonoBehaviour
{
    public static PlayerActionUI Instance;

    [Header("Fear Buttons")]
    public Button fearCaptainBtn;
    public Button fearScientistBtn;
    public Button fearEngineerBtn;
    public Button fearSoldierBtn;

    [Header("Action Buttons")]
    public Button startFireBtn;
    public Button cutO2Btn;
    public Button endTurnBtn;

    private void Awake() => Instance = this;

    private void Start()
    {
        fearCaptainBtn.onClick.AddListener(() => PlayerActionManager.Instance.ActionFear("Captain"));
        fearScientistBtn.onClick.AddListener(() => PlayerActionManager.Instance.ActionFear("Scientist"));
        fearEngineerBtn.onClick.AddListener(() => PlayerActionManager.Instance.ActionFear("Engineer"));
        fearSoldierBtn.onClick.AddListener(() => PlayerActionManager.Instance.ActionFear("Soldier"));

        startFireBtn.onClick.AddListener(() => PlayerActionManager.Instance.ActionStartFire());
        cutO2Btn.onClick.AddListener(() => PlayerActionManager.Instance.ActionCutO2());
        endTurnBtn.onClick.AddListener(() => GameManager.Instance.EndPlayerTurn());

        StartCoroutine(InitButtons());
    }

    private IEnumerator InitButtons()
    {
        yield return null;
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        int energy = PlayerActionManager.Instance.GetEnergy();
        bool o2Active = GameManager.Instance.IsO2Triggered();

        fearCaptainBtn.interactable   = energy >= 1 && !PlayerActionManager.Instance.IsAlreadyFeared("Captain")   && IsCrewAlive("Captain");
        fearScientistBtn.interactable = energy >= 1 && !PlayerActionManager.Instance.IsAlreadyFeared("Scientist") && IsCrewAlive("Scientist");
        fearEngineerBtn.interactable  = energy >= 1 && !PlayerActionManager.Instance.IsAlreadyFeared("Engineer")  && IsCrewAlive("Engineer");
        fearSoldierBtn.interactable   = energy >= 1 && !PlayerActionManager.Instance.IsAlreadyFeared("Soldier")   && IsCrewAlive("Soldier");

        startFireBtn.interactable = energy >= 10 && !FireSpread.Instance.IsFireActive();
        cutO2Btn.interactable     = energy >= 15 && !o2Active;
        endTurnBtn.interactable   = true;
    }

    private bool IsCrewAlive(string tag)
    {
        foreach (CharacterMovement cm in GameManager.Instance.GetCrewMembers())
            if (cm.gameObject.tag == tag) return true;
        return false;
    }
}
