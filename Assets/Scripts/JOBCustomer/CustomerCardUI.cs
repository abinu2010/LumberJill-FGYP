using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class CustomerCardUI : MonoBehaviour
{
    public GameObject rootPanel;
    public TextMeshProUGUI customerNameText;
    public TextMeshProUGUI itemsText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI noteText;
    public Button acceptButton;
    public Button declineButton;
    public Button closeButton;

    public UnityEvent OnShown;
    public UnityEvent OnAccepted;
    public UnityEvent OnDeclined;
    public UnityEvent OnHidden;

    private JobManager jobManager;
    private JobOrder job;

    private void Awake()
    {
        if (!rootPanel) rootPanel = gameObject;
        rootPanel.SetActive(false);
    }

    public void Show(JobManager manager, JobOrder order, CustomerFBX sourceAvatar)
    {
        jobManager = manager;
        job = order;

        if (jobManager == null || job == null)
        {
            Hide();
            return;
        }

        if (!rootPanel) rootPanel = gameObject;
        rootPanel.SetActive(true);
        OnShown?.Invoke();

        if (customerNameText)
            customerNameText.text = GetCustomerName(job.customer);

        if (itemsText)
            itemsText.text = BuildItemsText(job);

        if (timeText)
        {
            int seconds = Mathf.CeilToInt(job.deadlineSeconds);
            int m = seconds / 60;
            int s = seconds % 60;
            timeText.text = "Time limit: " + m.ToString("00") + ":" + s.ToString("00");
        }

        if (rewardText)
        {
            int gold = jobManager.EstimateGold(job);
            rewardText.text = "Estimated pay: " + gold.ToString() + " gold";
        }

        if (noteText)
            noteText.text = BuildCustomerNote(job.customer);

        if (acceptButton)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }

        if (declineButton)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(OnDeclineClicked);
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
    }

    public void Hide()
    {
        if (!rootPanel) rootPanel = gameObject;
        rootPanel.SetActive(false);
        OnHidden?.Invoke();
    }

    private void OnAcceptClicked()
    {
        if (jobManager != null && job != null)
            jobManager.AcceptJob(job);

        OnAccepted?.Invoke();
        Hide();
    }

    private void OnDeclineClicked()
    {
        if (jobManager != null && job != null)
            jobManager.DeclineJob(job);

        OnDeclined?.Invoke();
        Hide();
    }

    private string GetCustomerName(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Charlie: return "Charlie";
            case CustomerKind.Gabby: return "Gabby";
            case CustomerKind.Sponge: return "Sponge";
            case CustomerKind.Brandon: return "Brandon";
            default: return kind.ToString();
        }
    }

    private string BuildItemsText(JobOrder order)
    {
        if (order.lines == null || order.lines.Count == 0)
            return "No items";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < order.lines.Count; i++)
        {
            JobLine line = order.lines[i];
            if (line == null) continue;

            string name = line.product ? line.product.displayName : "Item";
            sb.Append(name);
            sb.Append(" x");
            sb.Append(line.quantity);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string BuildCustomerNote(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Charlie: return "Short deadlines, higher pay.";
            case CustomerKind.Gabby: return "Wants perfect quality.";
            case CustomerKind.Sponge: return "Small, simple orders.";
            case CustomerKind.Brandon: return "Large combo orders.";
            default: return "";
        }
    }
}
