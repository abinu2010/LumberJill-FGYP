using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DeliveryTruck : MonoBehaviour
{
    public DeliveryPanelUI deliveryPanel;

    private void OnMouseDown()
    {
        if (!deliveryPanel) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool panelOpen = deliveryPanel.gameObject.activeInHierarchy;

        if (!panelOpen && PlayerController.IsInputLocked) return;

        if (panelOpen)
            deliveryPanel.Close();
        else
            deliveryPanel.Open();
    }
}
