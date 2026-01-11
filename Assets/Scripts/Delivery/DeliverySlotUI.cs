using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeliverySlotUI : MonoBehaviour, IDropHandler
{
    [Header("UI")]
    public TextMeshProUGUI labelText;
    public Image outlineImage;
    public Image itemIcon;

    [Header("Debug")]
    public bool debugLog = true;

    public ItemSO TargetItem { get; private set; }
    public int RequiredQuantity { get; private set; }
    public int DeliveredCount { get; private set; }

    Color baseOutlineColor;
    Coroutine flashRoutine;

    void Awake()
    {
        if (outlineImage != null)
            baseOutlineColor = outlineImage.color;

        if (debugLog)
        {
            Debug.Log("DeliverySlotUI Awake on " + GetPath(this.transform) +
                      " TargetItem=" + (TargetItem ? TargetItem.displayName : "null"));
        }

        RefreshLabel();
        UpdateIcon();
    }

    public void Configure(ItemSO item, int quantity)
    {
        TargetItem = item;
        RequiredQuantity = Mathf.Max(1, quantity);
        DeliveredCount = 0;

        if (debugLog)
        {
            Debug.Log("DeliverySlotUI Configure on " + GetPath(this.transform) +
                      " target=" + (TargetItem ? TargetItem.displayName : "null") +
                      " qty=" + RequiredQuantity);
        }

        RefreshLabel();
        ResetVisual();
        UpdateIcon();
    }

    public void RefreshLabel()
    {
        if (!labelText) return;

        string nameText = TargetItem ? TargetItem.displayName : "Item";
        labelText.text = nameText + " " + DeliveredCount + "/" + RequiredQuantity;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemUI>() : null;
        if (drag == null) return;

        NoOfItems payload = drag.TakePayload();
        if (payload.IsEmpty)
        {
            if (debugLog)
            {
                Debug.Log(" OnDrop empty payload from " + drag.name +
                          " on slot " + GetPath(this.transform));
            }
            drag.ReturnRemainder(payload);
            return;
        }

        ItemSO droppedItem = payload.item;

        if (debugLog)
        {
            Debug.Log(" OnDrop slot='" + GetPath(this.transform) +
                      "' expected=" + (TargetItem ? TargetItem.displayName : "null") +
                      " dropped=" + (droppedItem ? droppedItem.displayName : "null") +
                      " count=" + payload.count);
        }

        // If this slot was never configured properly
        if (TargetItem == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(" TargetItem is null on slot " +
                                 GetPath(this.transform) +
                                 ". You are probably dropping onto a template slot.");
            }
            drag.ReturnRemainder(payload);
            Flash(false);
            return;
        }

        if (droppedItem != TargetItem)
        {
            if (debugLog)
            {
                Debug.Log(" Wrong item on slot " + GetPath(this.transform) +
                          ". Expected=" + TargetItem.name +
                          " got=" + (droppedItem ? droppedItem.name : "null"));
            }

            drag.ReturnRemainder(payload);
            Flash(false);
            return;
        }

        int remainingNeeded = Mathf.Max(0, RequiredQuantity - DeliveredCount);
        if (remainingNeeded <= 0)
        {
            if (debugLog)
            {
                Debug.Log("Slot " + GetPath(this.transform) +
                          " already full. Returning all.");
            }

            drag.ReturnRemainder(payload);
            Flash(true);
            return;
        }

    
        int taken = Mathf.Min(remainingNeeded, payload.count);
        DeliveredCount += taken;
        payload.count -= taken;

        if (debugLog)
        {
            Debug.Log("Slot " + GetPath(this.transform) +
                      " took=" + taken +
                      " now=" + DeliveredCount + "/" + RequiredQuantity +
                      " leftover=" + payload.count);
        }

        if (DeliveredCount > RequiredQuantity)
            DeliveredCount = RequiredQuantity;

        RefreshLabel();
        UpdateIcon();

        drag.ReturnRemainder(payload);

        Flash(true);
    }

    public bool IsSatisfied()
    {
        return DeliveredCount >= RequiredQuantity;
    }

    public void ClearCountsOnly()
    {
        DeliveredCount = 0;
        RefreshLabel();
        ResetVisual();
        UpdateIcon();
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

    void UpdateIcon()
    {
        if (!itemIcon) return;

        if (TargetItem == null || DeliveredCount <= 0)
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
        }
        else
        {
            itemIcon.sprite = TargetItem.icon;
            itemIcon.enabled = TargetItem.icon != null;
        }
    }

    void Flash(bool ok)
    {
        if (outlineImage == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        Color target = ok ? Color.green : Color.red;
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
    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
