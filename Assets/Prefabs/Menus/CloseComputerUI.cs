using UnityEngine;
using UnityEngine.UI;

public class CloseComputerUI : MonoBehaviour
{
    [SerializeField] private GameObject computerUI;
    private Button button;

    private void Awake()
    {
        if (computerUI == null)
            computerUI = FindComputerPanel();

        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(CloseUI);
        }
    }

    private GameObject FindComputerPanel()
    {
        Transform t = transform;
        while (t != null)
        {
            if (t.name == "Computer_UI" || t.name == "Computer Panel" || t.name == "ComputerPanel")
                return t.gameObject;

            t = t.parent;
        }

        return transform.parent != null ? transform.parent.gameObject : gameObject;
    }

    private void CloseUI()
    {
        WorkshopComputer[] all = FindObjectsByType<WorkshopComputer>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            WorkshopComputer comp = all[i];
            if (comp != null && comp.computerPanel == computerUI)
            {
                comp.OnCloseComputerPanel();
                return;
            }
        }

        WorldShopBuilding[] worldComputers = FindObjectsByType<WorldShopBuilding>(FindObjectsSortMode.None);
        for (int i = 0; i < worldComputers.Length; i++)
        {
            WorldShopBuilding comp = worldComputers[i];
            if (comp != null)
            {
                comp.CloseComputer();
                return;
            }
        }

        if (computerUI != null)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.Close(computerUI);
            else
            {
                computerUI.SetActive(false);
                PlayerController.IsInputLocked = false;
            }
        }
    }
}
