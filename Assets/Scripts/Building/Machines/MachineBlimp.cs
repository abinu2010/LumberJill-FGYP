using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MachineBlimp : MonoBehaviour, IPointerClickHandler
{
    private Machine machine;
    private TextMeshProUGUI countText;
    private Button backgroundButton;

    public void Init(Machine m, TextMeshProUGUI text, Button button)
    {
        machine = m;
        countText = text;
        backgroundButton = button;

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(Collect);
    }

    public void SetCount(int n)
    {
        if (countText != null)
            countText.text = "x" + n;
    }

    public void FaceCamera(Camera cam)
    {
        if (cam == null) return;

        Transform t = transform;
        Vector3 dir = t.position - cam.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(dir);
    }

    public void OnPointerClick(PointerEventData e)
    {
        Collect();
    }

    private void Collect()
    {
        if (machine != null)
            machine.CollectBlimp();
    }
}
