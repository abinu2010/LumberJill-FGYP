using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryJobRowUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public RectTransform slotsContainer;
    public Button deliverButton;
    public TextMeshProUGUI statusText;

    readonly List<DeliverySlotUI> slots = new List<DeliverySlotUI>();

    DeliveryPanelUI panel;
    JobOrder job;

    public void Bind(DeliveryPanelUI owner, JobOrder order, DeliverySlotUI slotPrefab)
    {
        panel = owner;
        job = order;

        if (titleText != null)
            titleText.text = BuildTitle(order);

        if (slotsContainer != null)
        {
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);
        }

        slots.Clear();

        if (slotPrefab != null && job != null && job.lines != null)
        {
            for (int i = 0; i < job.lines.Count; i++)
            {
                JobLine line = job.lines[i];
                if (line == null || line.product == null) continue;

                GameObject slotGO = Instantiate(slotPrefab.gameObject, slotsContainer);
                DeliverySlotUI slot = slotGO.GetComponent<DeliverySlotUI>();
                slot.Configure(line.product, line.quantity);
                slots.Add(slot);
            }
        }

        if (deliverButton != null)
        {
            deliverButton.onClick.RemoveAllListeners();
            deliverButton.onClick.AddListener(OnDeliverClicked);
        }

        if (statusText != null)
            statusText.text = "";
    }

    public DeliverySlotUI[] GetSlots()
    {
        return slots.ToArray();
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void OnDeliverClicked()
    {
        if (panel != null && job != null)
            panel.TryDeliver(job, this);
    }

    string BuildTitle(JobOrder order)
    {
        if (order == null) return "";

        string customer = order.customer.ToString();
        StringBuilder sb = new StringBuilder();

        if (order.lines != null)
        {
            for (int i = 0; i < order.lines.Count; i++)
            {
                JobLine line = order.lines[i];
                if (line == null) continue;

                if (sb.Length > 0) sb.Append(" + ");

                string name = line.product ? line.product.displayName : "Item";
                sb.Append(name);
                sb.Append(" x");
                sb.Append(line.quantity);
            }
        }

        int total = order.TotalQuantity;
        return customer + " delivery - " + sb.ToString() + " (" + total.ToString() + " items)";
    }
}
