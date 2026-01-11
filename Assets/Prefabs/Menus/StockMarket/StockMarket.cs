using UnityEngine;
using TMPro;

public class StockMarket : MonoBehaviour
{
    [Header("Data Sources")]
    private GameObject gameManager;
    private RealWorldData realWorldData;
    private Inventory inventory;

    [Header("Stock Market Panel")]
    [SerializeField] private GameObject stockMarketUIPanel;
    private bool panelOpen = false;

    [Header("Sell Panel")]
    [SerializeField] private TMP_Text amountToSellUI;
    [SerializeField] private TMP_Text totalPriceSellUI;
    [SerializeField] private int amountToSell = 0;
    private int maxSell;
    private float lumberLastPrice;

    [Header("Buy Panel")]
    [SerializeField] private TMP_Text amountToBuyUI;
    [SerializeField] private TMP_Text totalPriceBuyUI;
    [SerializeField] private int amountToBuy = 0;
    private int maxBuy;

    [Header("HUD References")]
    [SerializeField] private TMP_Text moneyUI;
    [SerializeField] private TMP_Text lumberUI;

    private void Start()
    {
        gameManager = GameObject.FindWithTag("GameController");
        if (gameManager != null)
        {
            realWorldData = gameManager.GetComponent<RealWorldData>();
            inventory = gameManager.GetComponent<Inventory>();
        }


        amountToBuyUI.text = amountToBuy.ToString();
        amountToSellUI.text = amountToSell.ToString();

        maxSell = inventory.lumber;

        UpdatePanelValues();
        UpdateHUD();
    }

    public void AddAmountSell(int amount)
    {
        if (inventory == null) return;

        maxSell = inventory.lumber;
        amountToSell = Mathf.Clamp(amountToSell + amount, 0, maxSell);
        amountToSellUI.text = amountToSell.ToString();
        UpdatePanelValues();
    }

    public void SubtractAmountSell(int amount)
    {
        amountToSell = Mathf.Max(amountToSell - amount, 0);
        amountToSellUI.text = amountToSell.ToString();
        UpdatePanelValues();
    }

    public void ExecuteSell()
    {
        if (inventory == null) return;

        if (amountToSell > 0 && inventory.lumber >= amountToSell)
        {
            float total = amountToSell * lumberLastPrice;
            inventory.money += total;
            inventory.lumber -= amountToSell;

            amountToSell = 0;
            amountToSellUI.text = "0";
            totalPriceSellUI.text = "0";

            UpdatePanelValues();
            UpdateHUD();
            inventory.RefreshUI();
        }
    }

    public void AddAmountBuy(int amount)
    {
        if (inventory == null) return;

        UpdatePriceAndMaxBuy();
        amountToBuy = Mathf.Clamp(amountToBuy + amount, 0, maxBuy);
        amountToBuyUI.text = amountToBuy.ToString();
        UpdatePanelValues();
    }

    public void SubtractAmountBuy(int amount)
    {
        amountToBuy = Mathf.Max(amountToBuy - amount, 0);
        amountToBuyUI.text = amountToBuy.ToString();
        UpdatePanelValues();
    }

    public void ExecuteBuy()
    {
        if (inventory == null) return;

        if (amountToBuy > 0)
        {
            float totalCost = amountToBuy * lumberLastPrice;
            if (inventory.money >= totalCost)
            {
                inventory.money -= totalCost;
                inventory.lumber += amountToBuy;

                amountToBuy = 0;
                amountToBuyUI.text = "0";
                totalPriceBuyUI.text = "0";

                UpdatePanelValues();
                UpdateHUD();
                inventory.RefreshUI();
            }
        }
    }

    private void UpdateHUD()
    {
        if (inventory == null) return;

        if (moneyUI != null)
            moneyUI.text = Mathf.RoundToInt(inventory.money).ToString();
        if (lumberUI != null)
            lumberUI.text = inventory.lumber.ToString();
    }

    private void UpdatePanelValues()
    {
        if (inventory == null) return;

        maxSell = inventory.lumber;

        UpdatePriceAndMaxBuy();

        if (totalPriceSellUI != null)
            totalPriceSellUI.text = (amountToSell * lumberLastPrice).ToString("F2");
        if (totalPriceBuyUI != null)
            totalPriceBuyUI.text = (amountToBuy * lumberLastPrice).ToString("F2");
    }

    private void UpdatePriceAndMaxBuy()
    {
        if (realWorldData == null)
        {
            if (SimulatedRealWorldDataSet.tradeData != null &&
                SimulatedRealWorldDataSet.tradeData.Length > 0)
            {
                int lastIndex = SimulatedRealWorldDataSet.tradeData.GetLength(0) - 1;
                lumberLastPrice = SimulatedRealWorldDataSet.tradeData[lastIndex, 1];
            }
        }
        else
        {
            lumberLastPrice = realWorldData.costLumber;
        }

        if (inventory != null && lumberLastPrice > 0f)
        {
            maxBuy = Mathf.FloorToInt(inventory.money / lumberLastPrice);
        }
        else
        {
            maxBuy = 0;
        }
    }

    public void toggleStockMarketUI()
    {
        if (stockMarketUIPanel == null) return;

        panelOpen = !panelOpen;
        stockMarketUIPanel.SetActive(panelOpen);
    }
}
