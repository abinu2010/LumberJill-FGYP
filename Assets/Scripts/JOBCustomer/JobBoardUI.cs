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
        gameObject.SetActive(true);
        Refresh();
        PlayerController.IsInputLocked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PlayerController.IsInputLocked = false;
    }

    public void Refresh()
    {
        if (!jobManager || !listRoot || !rowPrefab) return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(listRoot.GetChild(i).gameObject);
        }

        var jobs = jobManager.ActiveJobs;
        for (int i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            var go = Instantiate(rowPrefab, listRoot);
            var row = go.GetComponent<JobRowUI>();
            if (!row) row = go.AddComponent<JobRowUI>();
            row.Bind(job);
        }
    }
}
