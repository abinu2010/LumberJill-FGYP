using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WorkshopComputer : MonoBehaviour
{
    public GameObject computerPanel;
    public GameShopPanelUI shopPanel;
    public StockMarket stockMarket;

    public bool closeComputerPanelWhenOpeningApps = false;

    bool panelOpen;

    public UnityEvent ShopOpened;
    public UnityEvent StockMarketOpened;

    void OnMouseDown()
    {
        if (PlayerController.IsInputLocked) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        ToggleComputerPanel();
    }

    public void ToggleComputerPanel()
    {
        panelOpen = !panelOpen;

        if (panelOpen)
        {
            UIManager.Instance.Open(computerPanel);
        }
        else
        {
            UIManager.Instance.Close(computerPanel);
        }
    }

    public void OnShopButtonClicked()
    {
        if (shopPanel != null)
        {
            shopPanel.Open();
            ShopOpened?.Invoke();
        }

        if (closeComputerPanelWhenOpeningApps)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.Close(computerPanel);

            panelOpen = false;
        }
    }

    public void OnStockMarketButtonClicked()
    {
        if (stockMarket != null)
        {
            stockMarket.toggleStockMarketUI();
            StockMarketOpened?.Invoke();
        }

        if (closeComputerPanelWhenOpeningApps)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.Close(computerPanel);

            panelOpen = false;
        }
    }

    public void OnCloseComputerPanel()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(computerPanel);

        panelOpen = false;
    }
}
