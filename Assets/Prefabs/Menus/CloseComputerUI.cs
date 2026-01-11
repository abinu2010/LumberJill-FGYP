using UnityEngine;
using UnityEngine.UI;

public class CloseComputerUI : MonoBehaviour
{
    [SerializeField] private GameObject computerUI;
    private Button button;

    private void Awake()
    {
        if (computerUI == null)
            computerUI = transform.root.gameObject;

        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(CloseUI);
        }
    }

    private void CloseUI()
    {
        Debug.Log($"[CloseComputerUI] CloseUI called for gameObject={gameObject.name} root={computerUI?.name}");

        // Try to find a WorkshopComputer that owns this computer panel
        var all = FindObjectsOfType<WorkshopComputer>();
        foreach (var comp in all)
        {
            if (comp == null) continue;
            if (comp.computerPanel == computerUI)
            {
                Debug.Log("[CloseComputerUI] Found WorkshopComputer owner, calling OnCloseComputerPanel()");
                comp.OnCloseComputerPanel();
                return;
            }
        }

        // If not found, try to close via UIManager
        if (computerUI != null)
        {
            if (UIManager.Instance != null)
            {
                Debug.Log("[CloseComputerUI] No owner found; using UIManager to close panel");
                UIManager.Instance.Close(computerUI);
            }
            else
            {
                Debug.Log("[CloseComputerUI] No UIManager; deactivating panel and clearing input lock");
                computerUI.SetActive(false);
                PlayerController.IsInputLocked = false;
            }
        }
        else
        {
            Debug.LogWarning("[CloseComputerUI] No computerUI reference assigned and root not available.");
            PlayerController.IsInputLocked = false;
        }
    }
}
