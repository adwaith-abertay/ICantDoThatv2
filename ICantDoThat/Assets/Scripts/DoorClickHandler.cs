using UnityEngine;

public class DoorClickHandler : MonoBehaviour
{
    // No manual doorName field needed — reads from the GameObject name automatically
    private void OnMouseDown()
    {
        DoorManager.Instance.OnDoorClicked(gameObject.name);
    }
}