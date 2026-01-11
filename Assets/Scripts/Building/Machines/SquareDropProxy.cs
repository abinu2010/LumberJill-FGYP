using UnityEngine;
using UnityEngine.EventSystems;

public class SquareDropProxy : MonoBehaviour, IDropHandler
{
    public SquareCutter square;

    public void OnDrop(PointerEventData eventData)
    {
        if (square != null)
            square.OnDrop(eventData);
    }
}
