using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameShopPanelUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;

    [Header("List Setup")]
    public Transform contentRoot;
    public ShopRowUI rowPrefab;
    public ShopItemSO[] items;

    [Header("External")]
    public StorageManager storage;
    public Inventory inventory;
    public GameObject computerPanel;

    [Header("Controls")]
    public Button closeButton;
    public ScrollRect scrollRect;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackLabel;

    [Header("Debug")]
    public bool debugRaycastOnClick = false;

    public UnityEvent<ShopItemSO> Purchased = new UnityEvent<ShopItemSO>();

    private const string PrefOwnedPrefix = "ShopOwned_";
    private CanvasGroup canvasGroup;

    public bool IsOpen
    {
        get
        {
            GameObject root = GetRoot();
            return root != null && root.activeInHierarchy;
        }
    }

    private void Awake()
    {
        AutoWire();
        WireCloseButton();
    }

    private void Start()
    {
        BuildList();
        Close();
    }

    private void OnEnable()
    {
        AutoWire();
        WireCloseButton();
    }

    private void Update()
    {
        if (!debugRaycastOnClick) return;
        if (!IsOpen) return;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
                DebugRaycastAt(t.position);
        }

        if (Input.GetMouseButtonDown(0))
            DebugRaycastAt(Input.mousePosition);
    }

    public void Open()
    {
        AutoWire();
        WireCloseButton();

        GameObject root = GetRoot();
        if (!root) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Open(root);
        else
        {
            root.SetActive(true);
            PlayerController.IsInputLocked = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (scrollRect != null)
        {
            scrollRect.enabled = true;
            scrollRect.StopMovement();
        }

        if (feedbackLabel != null)
            feedbackLabel.text = string.Empty;
    }

    public void Close()
    {
        GameObject root = GetRoot();
        if (!root) return;

        if (UIManager.Instance != null)
            UIManager.Instance.Close(root);
        else
        {
            root.SetActive(false);
            PlayerController.IsInputLocked = false;
        }
    }

    public void HandleBuy(ShopItemSO item)
    {
        if (item == null) return;
        AutoWire();
        WireCloseButton();

        if (inventory == null)
        {
            SetFeedback("Inventory missing.");
            return;
        }

        if (item.singlePurchase && IsOwned(item))
        {
            SetFeedback("Owned.");
            BuildList();
            return;
        }

        if (item.price > 0 && !inventory.TrySpend(item.price))
        {
            SetFeedback("Not enough money.");
            return;
        }

        bool success = false;

        try
        {
            switch (item.type)
            {
                case ShopItemType.BuyItemToStorage:
                    success = BuyItemToStorage(item);
                    break;
                case ShopItemType.BuyMachineToPlace:
                case ShopItemType.BuyFieldToPlace:
                    success = BuyPlaceable(item);
                    break;
                case ShopItemType.BuyRecipe:
                    success = BuyRecipe(item);
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Shop buy failed: " + ex.Message);
            success = false;
        }

        if (!success)
        {
            if (item.price > 0)
                inventory.AddMoney(item.price);

            SetFeedback("Buy failed.");
            return;
        }

        if (item.singlePurchase)
            SetOwned(item);

        SetFeedback("Bought " + GetDisplayName(item));
        Purchased.Invoke(item);
        BuildList();

        if (item.type == ShopItemType.BuyMachineToPlace || item.type == ShopItemType.BuyFieldToPlace)
        {
            Close();

            if (computerPanel != null)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.Close(computerPanel);
                else
                    computerPanel.SetActive(false);
            }
        }
    }

    public bool IsOwned(ShopItemSO item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return false;
        if (PlayerPrefs.GetInt(PrefOwnedPrefix + item.id, 0) == 1) return true;
        if (PlayerPrefs.GetInt("RecipeUnlocked_" + item.id, 0) == 1) return true;
        if (PlayerPrefs.GetInt("MachineOwned_" + item.id, 0) == 1) return true;
        return false;
    }

    private bool BuyItemToStorage(ShopItemSO item)
    {
        if (storage == null)
            storage = FindFirstObjectByType<StorageManager>();

        if (storage == null) return false;
        if (item.item == null || item.itemCount <= 0) return false;

        storage.Put(item.item, item.itemCount);
        return true;
    }

    private bool BuyPlaceable(ShopItemSO item)
    {
        if (item.prefabToPlace == null) return false;
        if (BuildingSystem.instance == null) return false;

        BuildingSystem.instance.StartPlacement(item);
        return true;
    }

    private bool BuyRecipe(ShopItemSO item)
    {
        if (item.recipeToUnlock == null) return false;
        if (string.IsNullOrEmpty(item.recipeToUnlock.id)) return false;

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.UnlockRecipe(item.recipeToUnlock);
        else
        {
            PlayerPrefs.SetInt("RecipeUnlocked_" + item.recipeToUnlock.id, 1);
            PlayerPrefs.Save();
        }

        return true;
    }

    private void BuildList()
    {
        AutoWire();
        if (contentRoot == null || rowPrefab == null) return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        if (items == null || items.Length == 0) return;

        for (int i = 0; i < items.Length; i++)
        {
            ShopItemSO item = items[i];
            if (item == null) continue;

            ShopRowUI row = Instantiate(rowPrefab, contentRoot);
            row.Bind(this, item, IsOwned(item));
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void SetOwned(ShopItemSO item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        PlayerPrefs.SetInt(PrefOwnedPrefix + item.id, 1);
        PlayerPrefs.Save();
    }

    private void SetFeedback(string message)
    {
        if (feedbackLabel != null)
            feedbackLabel.text = message;
    }

    private string GetDisplayName(ShopItemSO item)
    {
        if (item == null) return "Item";
        return string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;
    }

    private GameObject GetRoot()
    {
        return rootPanel != null ? rootPanel : gameObject;
    }

    private void AutoWire()
    {
        if (storage == null)
            storage = FindFirstObjectByType<StorageManager>();

        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();

        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (contentRoot == null && scrollRect != null && scrollRect.content != null)
            contentRoot = scrollRect.content;

        if (closeButton == null)
            closeButton = FindCloseButton();

        GameObject root = GetRoot();
        if (root != null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
        }
    }

    private void WireCloseButton()
    {
        if (closeButton == null) return;
        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
        closeButton.interactable = true;
    }

    private Button FindCloseButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            string n = buttons[i].name.ToLowerInvariant();
            if (n == "btn_close" || n == "button_close" || n == "close_btn" || n == "buttonclose" || n == "closebutton" || n == "close")
                return buttons[i];
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            if (buttons[i].name.ToLowerInvariant().Contains("close"))
                return buttons[i];
        }

        return null;
    }

    private void DebugRaycastAt(Vector2 screenPos)
    {
        if (EventSystem.current != null)
        {
            PointerEventData ped = new PointerEventData(EventSystem.current);
            ped.position = screenPos;
            System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult r = results[i];
                Debug.Log("UI hit " + i.ToString() + " " + r.gameObject.name);
            }
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            Debug.Log("World hit " + hit.collider.gameObject.name);
    }
}
