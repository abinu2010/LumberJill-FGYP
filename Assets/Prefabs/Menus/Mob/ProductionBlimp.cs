using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductionBlimp : MonoBehaviour
{
    private ProductionMachine machine;
    private TextMeshProUGUI countText;
    private Button button;

    public void Init(ProductionMachine m, TextMeshProUGUI text, Button bgButton)
    {
        machine = m;
        countText = text;
        button = bgButton;

        if (button != null)
            button.onClick.AddListener(Collect);
    }

    public void SetCount(int n)
    {
        if (countText != null)
            countText.text = "x" + n.ToString();
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

    private void Collect()
    {
        if (machine != null)
            machine.CollectBlimp();
    }
}
