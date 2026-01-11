using UnityEngine;
using UnityEngine.UI;

public static class MachineBlimpExtensions
{
    public static void InitForSquare(this MachineBlimp blimp, SquareCutter cutter, Button button)
    {
        var forwarder = new SquareBlimpForwarder { cutter = cutter };
        var holder = blimp.gameObject.AddComponent<SquareBlimpForwarderHolder>();
        holder.forwarder = forwarder;

        if (button != null)
            button.onClick.AddListener(forwarder.Collect);
    }
}

public class SquareBlimpForwarder
{
    public SquareCutter cutter;

    public void Collect()
    {
        if (cutter != null)
            cutter.CollectBlimp();
    }
}

public class SquareBlimpForwarderHolder : MonoBehaviour
{
    public SquareBlimpForwarder forwarder;
}
