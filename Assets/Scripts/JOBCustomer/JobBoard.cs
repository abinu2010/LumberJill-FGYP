using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class JobBoard : MonoBehaviour
{
    public JobBoardUI jobBoard;

    private void OnMouseDown()
    {
        if (!jobBoard) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (PlayerController.IsInputLocked) return;

        if (jobBoard.gameObject.activeSelf)
            jobBoard.Close();
        else
            jobBoard.Open();
    }
}
