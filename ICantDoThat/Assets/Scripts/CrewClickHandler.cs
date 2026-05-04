using UnityEngine;

public class CrewClickHandler : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (SIIDUIManager.Instance != null)
        {
            SIIDUIManager.Instance.OnCrewClicked(gameObject.tag);
        }

       
    }
}