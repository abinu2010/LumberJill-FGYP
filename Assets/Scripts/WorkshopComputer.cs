using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WorkshopComputer : MonoBehaviour
{
    public GameObject computerPanel;
    public GameShopPanelUI shopPanel;
    public StockMarket stockMarket;
    public bool closeComputerPanelWhenOpeningApps = false;

    private bool panelOpen;

    public UnityEvent ComputerOpened;
    public UnityEvent ComputerClosed;
    public UnityEvent ShopOpened;
    public UnityEvent StockMarketOpened;

    private void Start()
    {
        SyncPanelState();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (UIManager.Instance != null) UIManager.Instance.ForceRefresh();

        SyncPanelState();
        if (panelOpen) return;
        if (PlayerController.IsInputLocked) return;

        OpenComputerPanel();
    }

    public void ToggleComputerPanel()
    {
        if (UIManager.Instance != null) UIManager.Instance.ForceRefresh();
        SyncPanelState();

        if (panelOpen)
            CloseComputerPanel();
        else
            OpenComputerPanel();
    }

    public void OpenComputerPanel()
    {
        if (!computerPanel) return;

        bool wasOpen = computerPanel.activeInHierarchy;

        if (UIManager.Instance != null)
            UIManager.Instance.Open(computerPanel);
        else
        {
            computerPanel.SetActive(true);
            PlayerController.IsInputLocked = true;
        }

        panelOpen = true;

        if (!wasOpen)
            ComputerOpened?.Invoke();
    }

    public void CloseComputerPanel()
    {
        if (!computerPanel) return;

        if (shopPanel != null && shopPanel.IsOpen)
            shopPanel.Close();

        if (stockMarket != null && stockMarket.IsOpen)
            stockMarket.Close();

        if (UIManager.Instance != null)
            UIManager.Instance.Close(computerPanel);
        else
        {
            computerPanel.SetActive(false);
            PlayerController.IsInputLocked = false;
        }

        panelOpen = false;
        ComputerClosed?.Invoke();
    }

    public void OnShopButtonClicked()
    {
        if (shopPanel == null) return;

        if (stockMarket != null && stockMarket.IsOpen)
            stockMarket.Close();

        shopPanel.Open();
        ShopOpened?.Invoke();

        if (closeComputerPanelWhenOpeningApps)
            CloseOnlyComputerPanel();
    }

    public void OnStockMarketButtonClicked()
    {
        if (stockMarket == null) return;

        if (shopPanel != null && shopPanel.IsOpen)
            shopPanel.Close();

        stockMarket.Open();
        StockMarketOpened?.Invoke();

        if (closeComputerPanelWhenOpeningApps)
            CloseOnlyComputerPanel();
    }

    public void OnCloseComputerPanel()
    {
        CloseComputerPanel();
    }

    private void CloseOnlyComputerPanel()
    {
        if (!computerPanel) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Close(computerPanel);
        else
            computerPanel.SetActive(false);

        panelOpen = false;
        ComputerClosed?.Invoke();
    }

    private void SyncPanelState()
    {
        panelOpen = computerPanel != null && computerPanel.activeInHierarchy;
    }
}
