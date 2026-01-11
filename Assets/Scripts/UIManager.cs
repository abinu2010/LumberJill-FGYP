using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;
        PlayerController.IsInputLocked = false; // always unlocked at start
    }
    public void Open(GameObject panel)
    {
        if (!panel) return;
        panel.SetActive(true);
        PlayerController.IsInputLocked = true;
    }

    public void Close(GameObject panel)
    {
        if (!panel) return;
        panel.SetActive(false);
        PlayerController.IsInputLocked = false;
    }
}
