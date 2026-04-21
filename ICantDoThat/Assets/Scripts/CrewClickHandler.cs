using UnityEngine;

public class CrewClickHandler : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (PlayerActionUI.Instance == null) return;
        PlayerActionUI.Instance.OnCrewClicked(gameObject.tag);
    }
}
