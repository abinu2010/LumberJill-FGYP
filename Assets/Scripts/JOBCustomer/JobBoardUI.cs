using UnityEngine;
using UnityEngine.Events;

public class JobBoardUI : MonoBehaviour
{
    public JobManager jobManager;
    public RectTransform listRoot;
    public GameObject rowPrefab;
    public UnityEvent Opened;

    public void Open()
    {
        Opened?.Invoke();

        if (UIManager.Instance != null)
            UIManager.Instance.Open(gameObject);
        else
        {
            gameObject.SetActive(true);
            PlayerController.IsInputLocked = true;
        }

        Refresh();
    }

    public void Close()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(gameObject);
        else
        {
            gameObject.SetActive(false);
            PlayerController.IsInputLocked = false;
        }
    }

    public void Refresh()
    {
        if (!jobManager || !listRoot || !rowPrefab) return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        var jobs = jobManager.ActiveJobs;
        for (int i = 0; i < jobs.Count; i++)
        {
            JobOrder job = jobs[i];
            if (job == null) continue;

            GameObject go = Instantiate(rowPrefab, listRoot);
            JobRowUI row = go.GetComponent<JobRowUI>();
            if (!row) row = go.AddComponent<JobRowUI>();
            row.Bind(job);
        }
    }
}
