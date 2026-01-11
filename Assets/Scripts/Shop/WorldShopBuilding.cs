using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WorldShopBuilding : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject computerUI;
    public UnityEvent Opened;

    private void OnMouseDown()
    {
        if (PlayerController.IsInputLocked)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;
              
        computerUI.SetActive(true);
        Opened?.Invoke();
        Debug.Log("Computer Opened");
        PlayerController.IsInputLocked = true;
    }
}
