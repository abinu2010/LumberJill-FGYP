using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WorldShopBuilding : MonoBehaviour
{
    [SerializeField] private GameObject computerUI;
    public UnityEvent Opened;

    private WorkshopComputer workshopComputer;
    private bool opened;

    private void Awake()
    {
        workshopComputer = GetComponent<WorkshopComputer>();
    }

    private void Start()
    {
        if (workshopComputer == null)
            workshopComputer = GetComponent<WorkshopComputer>();

        if (workshopComputer != null && workshopComputer.computerPanel != null)
            computerUI = workshopComputer.computerPanel;

        opened = computerUI != null && computerUI.activeInHierarchy;
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (UIManager.Instance != null) UIManager.Instance.ForceRefresh();
        if (opened && computerUI != null && computerUI.activeInHierarchy) return;
        if (PlayerController.IsInputLocked) return;
        OpenComputer();
    }

    public void OpenComputer()
    {
        if (workshopComputer != null)
        {
            workshopComputer.OpenComputerPanel();
            opened = true;
            Opened?.Invoke();
            return;
        }

        if (!computerUI) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Open(computerUI);
        else
        {
            computerUI.SetActive(true);
            PlayerController.IsInputLocked = true;
        }

        opened = true;
        Opened?.Invoke();
    }

    public void CloseComputer()
    {
        if (workshopComputer != null)
        {
            workshopComputer.CloseComputerPanel();
            opened = false;
            return;
        }

        if (!computerUI) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Close(computerUI);
        else
        {
            computerUI.SetActive(false);
            PlayerController.IsInputLocked = false;
        }

        opened = false;
    }
}
