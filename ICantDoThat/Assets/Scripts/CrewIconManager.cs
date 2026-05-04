using UnityEngine;

public class CrewIconManager : MonoBehaviour
{
    public static CrewIconManager Instance;

    [Header("Captain")]
    public SpriteRenderer captainIcon;
    public Sprite captainNormal;
    public Sprite captainFeared;
    public Vector3 captainScale = Vector3.one;

    [Header("Scientist")]
    public SpriteRenderer scientistIcon;
    public Sprite scientistNormal;
    public Sprite scientistFeared;
    public Vector3 scientistScale = Vector3.one;

    [Header("Engineer")]
    public SpriteRenderer engineerIcon;
    public Sprite engineerNormal;
    public Sprite engineerFeared;
    public Vector3 engineerScale = Vector3.one;

    [Header("Soldier")]
    public SpriteRenderer soldierIcon;
    public Sprite soldierNormal;
    public Sprite soldierFeared;
    public Sprite soldierLostLife;
    public Sprite soldierFearedLostLife;
    public Vector3 soldierScale = Vector3.one;

    [Header("Robot")]
    public SpriteRenderer robotIcon;
    public Sprite robotNormal;
    public Sprite robotFeared;
    public Sprite robotHacked;
    public Vector3 robotScale = Vector3.one;

    private bool soldierHasLostLife = false;

    private void Awake() => Instance = this;

    // --- Core helper — swaps sprite and enforces assigned scale ---
    // --- Core helper — swaps sprite and enforces assigned scale ---
    private void SetSprite(SpriteRenderer sr, Sprite sprite, Vector3 scale)
    {
        if (sr == null) return; // silently ignore if destroyed
        sr.sprite = sprite;
        sr.transform.localScale = scale;
    }

    // --- Revert feared icons after one turn ---
    public void RevertFearIcons()
    {
        if (captainIcon != null && captainIcon.sprite == captainFeared)
            SetCaptainNormal();

        if (scientistIcon != null && scientistIcon.sprite == scientistFeared)
            SetScientistNormal();

        if (engineerIcon != null && engineerIcon.sprite == engineerFeared)
            SetEngineerNormal();

        if (robotIcon != null && robotIcon.sprite == robotFeared)
            SetRobotNormal();

        if (soldierIcon != null && (soldierIcon.sprite == soldierFeared || soldierIcon.sprite == soldierFearedLostLife))
            SetSprite(soldierIcon, soldierHasLostLife ? soldierLostLife : soldierNormal, soldierScale);
    }

    // --- Captain ---
    public void SetCaptainFeared() => SetSprite(captainIcon, captainFeared, captainScale);
    public void SetCaptainNormal() => SetSprite(captainIcon, captainNormal, captainScale);

    // --- Scientist ---
    public void SetScientistFeared() => SetSprite(scientistIcon, scientistFeared, scientistScale);
    public void SetScientistNormal() => SetSprite(scientistIcon, scientistNormal, scientistScale);

    // --- Engineer ---
    public void SetEngineerFeared() => SetSprite(engineerIcon, engineerFeared, engineerScale);
    public void SetEngineerNormal() => SetSprite(engineerIcon, engineerNormal, engineerScale);

    // --- Soldier ---
    public void SetSoldierFeared()
    {
        SetSprite(soldierIcon, soldierHasLostLife ? soldierFearedLostLife : soldierFeared, soldierScale);
    }
    public void SetSoldierNormal()   => SetSprite(soldierIcon, soldierNormal, soldierScale);
    public void SetSoldierLostLife()
    {
        soldierHasLostLife = true;
        SetSprite(soldierIcon, soldierLostLife, soldierScale);
    }

    // --- Robot ---
    public void SetRobotFeared() => SetSprite(robotIcon, robotFeared, robotScale);
    public void SetRobotNormal() => SetSprite(robotIcon, robotNormal, robotScale);
    public void SetRobotHacked() => SetSprite(robotIcon, robotHacked, robotScale);

    // --- Generic setter by tag ---
    public void SetFeared(string tag)
    {
        switch (tag)
        {
            case "Captain":   SetCaptainFeared();   break;
            case "Scientist": SetScientistFeared();  break;
            case "Engineer":  SetEngineerFeared();   break;
            case "Soldier":   SetSoldierFeared();    break;
            case "Robot":     SetRobotFeared();      break;
        }
    }

    
}