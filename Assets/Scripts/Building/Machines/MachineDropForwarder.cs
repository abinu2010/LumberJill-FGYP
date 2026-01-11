using UnityEngine;
using UnityEngine.EventSystems;

public class MachineDropForwarder : MonoBehaviour, IDropHandler
{
    public Machine target;

    public void OnDrop(PointerEventData e)
    {
        if (target != null)
            target.OnDrop(e);
    }
}
