using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopRowUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI priceLabel;
    public Button buyButton;

    ShopItemSO data;
    GameShopPanelUI owner;
    bool owned;

    public void Bind(GameShopPanelUI owner, ShopItemSO data, bool owned)
    {
        this.owner = owner;
        this.data = data;
        this.owned = owned;

        if (nameLabel != null)
            nameLabel.text = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        if (icon != null)
            icon.sprite = data.icon;

        RefreshPriceAndButton();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

    }

    void RefreshPriceAndButton()
    {
        if (priceLabel != null)
        {
            if (data.singlePurchase && owned)
                priceLabel.text = "Owned";
            else
                priceLabel.text = data.price.ToString();
        }

        if (buyButton != null)
        {
            if (data.singlePurchase && owned)
                buyButton.interactable = false;
            else
                buyButton.interactable = true;
        }
    }

    void OnBuyClicked()
    {
        if (owner == null || data == null)
        {
            Debug.Log("add owner");
            return;
        }

        owner.HandleBuy(data);
    }
}
