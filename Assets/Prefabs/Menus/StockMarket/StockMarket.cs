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

    public bool IsOpen => stockMarketUIPanel != null && stockMarketUIPanel.activeInHierarchy;

    private void Start()
    {
        AutoWire();
        SetText(amountToBuyUI, amountToBuy.ToString());
        SetText(amountToSellUI, amountToSell.ToString());
        UpdatePanelValues();
        UpdateHUD();
        Close();
    }

    public void AddAmountSell(int amount)
    {
        AutoWire();
        if (inventory == null) return;

        maxSell = inventory.lumber;
        amountToSell = Mathf.Clamp(amountToSell + amount, 0, maxSell);
        SetText(amountToSellUI, amountToSell.ToString());
        UpdatePanelValues();
    }

    public void SubtractAmountSell(int amount)
    {
        amountToSell = Mathf.Max(amountToSell - amount, 0);
        SetText(amountToSellUI, amountToSell.ToString());
        UpdatePanelValues();
    }

    public void ExecuteSell()
    {
        AutoWire();
        if (inventory == null) return;
        if (amountToSell <= 0) return;
        if (inventory.lumber < amountToSell) return;

        float total = amountToSell * lumberLastPrice;
        inventory.ChangeLumber(-amountToSell);
        inventory.AddMoney(total);

        amountToSell = 0;
        SetText(amountToSellUI, "0");
        SetText(totalPriceSellUI, "0");

        UpdatePanelValues();
        UpdateHUD();
    }

    public void AddAmountBuy(int amount)
    {
        AutoWire();
        if (inventory == null) return;

        UpdatePriceAndMaxBuy();
        amountToBuy = Mathf.Clamp(amountToBuy + amount, 0, maxBuy);
        SetText(amountToBuyUI, amountToBuy.ToString());
        UpdatePanelValues();
    }

    public void SubtractAmountBuy(int amount)
    {
        amountToBuy = Mathf.Max(amountToBuy - amount, 0);
        SetText(amountToBuyUI, amountToBuy.ToString());
        UpdatePanelValues();
    }

    public void ExecuteBuy()
    {
        AutoWire();
        if (inventory == null) return;
        if (amountToBuy <= 0) return;

        float totalCost = amountToBuy * lumberLastPrice;
        if (!inventory.TrySpend(totalCost)) return;

        inventory.ChangeLumber(amountToBuy);

        amountToBuy = 0;
        SetText(amountToBuyUI, "0");
        SetText(totalPriceBuyUI, "0");

        UpdatePanelValues();
        UpdateHUD();
    }

    public void Open()
    {
        if (stockMarketUIPanel == null) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Open(stockMarketUIPanel);
        else
        {
            stockMarketUIPanel.SetActive(true);
            PlayerController.IsInputLocked = true;
        }

        panelOpen = true;
        UpdatePanelValues();
        UpdateHUD();
    }

    public void Close()
    {
        if (stockMarketUIPanel == null) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Close(stockMarketUIPanel);
        else
        {
            stockMarketUIPanel.SetActive(false);
            PlayerController.IsInputLocked = false;
        }

        panelOpen = false;
    }

    public void toggleStockMarketUI()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    private void UpdateHUD()
    {
        AutoWire();
        if (inventory == null) return;

        SetText(moneyUI, Mathf.RoundToInt(inventory.money).ToString());
        SetText(lumberUI, inventory.lumber.ToString());
        inventory.RefreshUI();
    }

    private void UpdatePanelValues()
    {
        AutoWire();
        if (inventory == null) return;

        maxSell = inventory.lumber;
        UpdatePriceAndMaxBuy();
        amountToSell = Mathf.Clamp(amountToSell, 0, maxSell);
        amountToBuy = Mathf.Clamp(amountToBuy, 0, maxBuy);

        SetText(amountToSellUI, amountToSell.ToString());
        SetText(amountToBuyUI, amountToBuy.ToString());
        SetText(totalPriceSellUI, (amountToSell * lumberLastPrice).ToString("F2"));
        SetText(totalPriceBuyUI, (amountToBuy * lumberLastPrice).ToString("F2"));
    }

    private void UpdatePriceAndMaxBuy()
    {
        AutoWire();

        if (realWorldData == null)
        {
            if (SimulatedRealWorldDataSet.tradeData != null && SimulatedRealWorldDataSet.tradeData.Length > 0)
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
            maxBuy = Mathf.FloorToInt(inventory.money / lumberLastPrice);
        else
            maxBuy = 0;
    }

    private void AutoWire()
    {
        if (inventory != null) return;

        gameManager = GameObject.FindWithTag("GameController");
        if (gameManager != null)
        {
            realWorldData = gameManager.GetComponent<RealWorldData>();
            inventory = gameManager.GetComponent<Inventory>();
        }

        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();

        if (realWorldData == null)
            realWorldData = FindFirstObjectByType<RealWorldData>();
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
