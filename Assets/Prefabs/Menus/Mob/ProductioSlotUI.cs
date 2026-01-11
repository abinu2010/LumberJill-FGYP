using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProductioSlotUI : MonoBehaviour, IDropHandler
{
    public string slotId;
    public TextMeshProUGUI labelText;
    public Image outlineImage;
    public Image pieceIcon;

    public ItemSO CurrentItem { get; private set; }

    StorageManager storage;
    Color baseOutlineColor;
    Coroutine flashRoutine;

    void Awake()
    {
        storage = Object.FindAnyObjectByType<StorageManager>();

        if (outlineImage != null)
            baseOutlineColor = outlineImage.color;

        ClearPiece();
        ResetVisual();
    }

    public void Configure(string id, string label)
    {
        slotId = id;
        if (labelText != null) labelText.text = label;
        ClearPiece();
        ResetVisual();
    }

    public void ClearPiece()
    {
        CurrentItem = null;
        if (pieceIcon != null)
        {
            pieceIcon.enabled = false;
            pieceIcon.sprite = null;
        }
    }

    public void ResetVisual()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (outlineImage != null)
            outlineImage.color = baseOutlineColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemUI>() : null;
        if (drag == null) return;

        NoOfItems payload = drag.TakePayload();
        if (payload.IsEmpty)
        {
            drag.ReturnRemainder(payload);
            return;
        }

        ItemSO item = payload.item;

        if (!IsAllowedItem(item))
        {
            drag.ReturnRemainder(payload);   // Goes back to where it came from.
            ShowResult(false);               // Flash red.
            return;
        }

        // Take exactly one piece into this slot, rest goes back.
        if (payload.count > 1)
        {
            payload.count -= 1;
            drag.ReturnRemainder(payload);
        }

        // If slot already had something, put it back to storage.
        if (CurrentItem != null && storage != null)
        {
            storage.Put(CurrentItem, 1);
        }

        CurrentItem = item;

        if (pieceIcon != null)
        {
            pieceIcon.enabled = true;
            pieceIcon.sprite = item.icon;
        }

        ResetVisual();
    }

    bool IsAllowedItem(ItemSO item)
    {
        if (!item) return false;
        if (item.category != ItemCategory.Utility) return false;
        if (!item.isProductionSquarePiece) return false;
        return true;
    }

    public void ShowResult(bool isCorrect)
    {
        if (outlineImage == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        Color target = isCorrect ? Color.green : Color.red;
        flashRoutine = StartCoroutine(FlashOutline(target));
    }

    IEnumerator FlashOutline(Color target)
    {
        outlineImage.color = target;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            outlineImage.color = Color.Lerp(target, baseOutlineColor, t);
            yield return null;
        }

        outlineImage.color = baseOutlineColor;
        flashRoutine = null;
    }
}
