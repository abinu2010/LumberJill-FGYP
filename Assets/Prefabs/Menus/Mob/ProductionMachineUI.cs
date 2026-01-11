using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductionMachineUI : MonoBehaviour
{
    [Header("Slots Pool")]
    public ProductioSlotUI[] slots;

    [Header("Product Picker")]
    public Transform productButtonContainer;
    public Button productButtonPrefab;

    [Header("UI Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI errorText;

    [Header("Buttons")]
    public Button assembleButton;
    public Button closeButton;

    [Header("Auto Close")]
    public bool autoCloseOnSuccess = true;
    [Min(0.1f)] public float autoCloseDelaySeconds = 1.5f;

    [Header("Recipe Hint")]
    public Image hintImage;

    ProductionMachine owner;
    readonly List<ProductionRecipeSO> recipes = new List<ProductionRecipeSO>();
    ProductionRecipeSO currentRecipe;

    Coroutine autoCloseRoutine;

    public void Init(ProductionMachine machine, IList<ProductionRecipeSO> recipeList)
    {
        owner = machine;
        recipes.Clear();

        if (recipeList != null)
        {
            for (int i = 0; i < recipeList.Count; i++)
            {
                if (recipeList[i] != null)
                    recipes.Add(recipeList[i]);
            }
        }

        WireButtons();
        BuildProductButtons();

        if (recipes.Count > 0)
        {
            SelectRecipe(recipes[0]);
        }
        else
        {
            currentRecipe = null;
            ConfigureSlots(null);
            UpdateHintImage(null);
            ShowNoRecipesMessage();
        }
    }

    void WireButtons()
    {
        if (assembleButton != null)
        {
            assembleButton.onClick.RemoveAllListeners();
            assembleButton.onClick.AddListener(OnAssembleClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    void BuildProductButtons()
    {
        if (!productButtonContainer || !productButtonPrefab) return;

        for (int i = productButtonContainer.childCount - 1; i >= 0; i--)
            Destroy(productButtonContainer.GetChild(i).gameObject);

        RecipeUnlockManager unlockMgr = RecipeUnlockManager.Instance;

        for (int i = 0; i < recipes.Count; i++)
        {
            ProductionRecipeSO recipe = recipes[i];
            if (recipe == null) continue;

            Button btn = Object.Instantiate(productButtonPrefab, productButtonContainer);
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();

            bool unlocked = unlockMgr == null || unlockMgr.IsUnlocked(recipe);

            if (label != null)
            {
                label.text = unlocked
                    ? recipe.displayName
                    : recipe.displayName + " (Locked)";
            }

            btn.interactable = unlocked;

            ProductionRecipeSO captured = recipe;
            btn.onClick.AddListener(() => SelectRecipe(captured));
        }
    }

    void SelectRecipe(ProductionRecipeSO recipe)
    {
        currentRecipe = recipe;

        if (titleText != null)
            titleText.text = recipe != null ? recipe.displayName : string.Empty;

        if (assembleButton != null)
            assembleButton.interactable = (recipe != null);

        ConfigureSlots(recipe);
        UpdateHintImage(recipe);
        ShowNeedPiecesMessage();

    }

    void UpdateHintImage(ProductionRecipeSO recipe)
    {
        if (hintImage == null) return;

        if (recipe != null && recipe.hintSprite != null)
        {
            hintImage.enabled = true;
            hintImage.sprite = recipe.hintSprite;
        }
        else
        {
            hintImage.enabled = false;
            hintImage.sprite = null;
        }
    }

    void ConfigureSlots(ProductionRecipeSO recipe)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            ProductioSlotUI slot = slots[i];
            if (slot == null) continue;

            if (recipe != null && i < recipe.slots.Count)
            {
                var req = recipe.slots[i];
                slot.gameObject.SetActive(true);
                slot.Configure(req.slotId, req.label);
            }
            else
            {
                slot.gameObject.SetActive(false);
                slot.ClearPiece();
            }
        }
    }

    void OnAssembleClicked()
    {
        if (owner == null || currentRecipe == null) return;

        RecipeUnlockManager unlockMgr = RecipeUnlockManager.Instance;
        if (unlockMgr != null && !unlockMgr.IsUnlocked(currentRecipe))
        {
            ShowLockedMessage();
            return;
        }

        if (AnyRequiredSlotEmpty())
        {
            ShowNeedPiecesMessage();
            return;
        }

        int errors = CalculateErrors();

        int scrapThreshold = owner != null ? Mathf.Max(0, owner.misfitScrapThreshold) : 0;
        if (scrapThreshold > 0 && errors >= scrapThreshold)
        {
            ScrapAllSlotsWithMessage(errors);
            return;
        }

        UpdateErrorLabel(errors);
        owner.OnAssemble(currentRecipe, errors);


        if (autoCloseOnSuccess)
            BeginAutoClose();
    }

    void OnCloseClicked()
    {
        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        gameObject.SetActive(false);
        PlayerController.IsInputLocked = false;

    }

    void BeginAutoClose()
    {
        if (autoCloseRoutine != null)
            StopCoroutine(autoCloseRoutine);

        autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
    }

    IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelaySeconds);

        if (gameObject.activeInHierarchy)
        {
            OnCloseClicked();
        }

        autoCloseRoutine = null;
    }

    bool AnyRequiredSlotEmpty()
    {
        if (currentRecipe == null) return false;

        bool anyEmpty = false;

        for (int i = 0; i < currentRecipe.slots.Count; i++)
        {
            var req = currentRecipe.slots[i];
            ProductioSlotUI slot = GetSlotById(req.slotId);
            if (slot == null || slot.CurrentItem == null)
            {
                anyEmpty = true;
                if (slot != null)
                    slot.ShowResult(false);
            }
        }

        return anyEmpty;
    }

    int CalculateErrors()
    {
        if (currentRecipe == null) return 0;

        int errors = 0;

        for (int i = 0; i < currentRecipe.slots.Count; i++)
        {
            var req = currentRecipe.slots[i];
            ProductioSlotUI slot = GetSlotById(req.slotId);
            errors += SlotError(slot, req.requiredWidth, req.requiredHeight);
        }

        return errors;
    }

    ProductioSlotUI GetSlotById(string slotId)
    {
        if (slots == null) return null;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot != null && slot.slotId == slotId)
                return slot;
        }

        return null;
    }

    int SlotError(ProductioSlotUI slot, int reqW, int reqH)
    {
        if (slot == null)
            return 1;

        ItemSO item = slot.CurrentItem;
        if (item == null)
        {
            slot.ShowResult(false);
            return 1;
        }

        int w = Mathf.Max(1, item.gridWidth);
        int h = Mathf.Max(1, item.gridHeight);

        bool match = (w == reqW && h == reqH) || (w == reqH && h == reqW);
        slot.ShowResult(match);
        return match ? 0 : 1;
    }

    void UpdateErrorLabel(int errors)
    {
        if (errorText == null) return;

        if (errors <= 0)
            errorText.text = "Perfect fit. 0 wrong fits.";
        else if (errors == 1)
            errorText.text = "1 wrong fit.";
        else
            errorText.text = errors + " wrong fits.";
    }

    void ShowNeedPiecesMessage()
    {
        if (errorText == null) return;
        errorText.text = "Add your square pieces into every slot.";
    }

    void ShowLockedMessage()
    {
        if (errorText == null) return;
        errorText.text = "Buy a recipe in the shop to use this machine.";
    }

    void ShowNoRecipesMessage()
    {
        if (errorText != null)
            errorText.text = "Go to the shop and buy a recipe to start assembling.";

        if (assembleButton != null)
            assembleButton.interactable = false;
    }

    void ScrapAllSlotsWithMessage(int errors)
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].ClearPiece();
                slots[i].ResetVisual();
            }
        }

        if (errorText != null)
        {
            errorText.text = "Too many misfits (" + errors + "). Pieces scrapped. Try again.";
        }
    }
}
