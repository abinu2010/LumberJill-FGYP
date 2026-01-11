using UnityEngine;

public class ObjectDrag : MonoBehaviour
{
    private Vector3 offSet;
    private bool isDragging = false;
    private void OnMouseDown()
    {
        offSet=transform.position-BuildingSystem.GetMouseWorldPosition();
        isDragging = true;
    }
    private void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 pos = BuildingSystem.GetMouseWorldPosition() + offSet;
            transform.position = BuildingSystem.instance.SnapCoordinateToGrid(pos);

        }
        
    }
    private void OnMouseUp()
    {
        isDragging = false;
    }
    private void Update()
    {
        touchDrag();
    }
    private void touchDrag()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        if (touch.phase == UnityEngine.TouchPhase.Began)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    offSet = transform.position - BuildingSystem.GetMouseWorldPosition();
                    isDragging = true;
                }
            }
        }
        if (touch.phase == UnityEngine.TouchPhase.Moved && isDragging)
        {
            Vector3 pos = BuildingSystem.GetMouseWorldPosition() + offSet;
            transform.position = BuildingSystem.instance.SnapCoordinateToGrid(pos);
        }

        if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
        {
            isDragging = false;
        }

    }
    

}
