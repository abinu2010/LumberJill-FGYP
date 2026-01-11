using System;
using System.Collections.Generic;
using UnityEngine;

public enum CustomerKind
{
    Charlie,
    Gabby,
    Sponge,
    Brandon
}

[Serializable]
public class JobLine
{
    public ItemSO product;
    public int quantity;
    [NonSerialized] public int producedCount;
}

[Serializable]
public class JobOrder
{
    public string id;
    public CustomerKind customer;
    public List<JobLine> lines = new List<JobLine>();
    public float deadlineSeconds;
    public int slotIndex = -1;

    [NonSerialized] public float acceptedAt;

    public bool isAccepted;
    public bool isCompleted;
    public bool isFailed;
    public bool isReadyForDelivery;
    public int misfitCount;
    public int xpReward;
    public int goldReward;

    public int TotalQuantity
    {
        get
        {
            int total = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line != null)
                {
                    total += Mathf.Max(0, line.quantity);
                }
            }
            return total;
        }
    }

    public int TotalProduced
    {
        get
        {
            int total = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line != null)
                {
                    total += Mathf.Max(0, line.producedCount);
                }
            }
            return total;
        }
    }

    public float StarValue
    {
        get
        {
            float stars = 3f - 0.5f * misfitCount;
            return Mathf.Clamp(stars, 0f, 3f);
        }
    }

    public float RemainingSeconds
    {
        get
        {
            if (!isAccepted || isCompleted || isFailed) return 0f;
            float elapsed = Time.time - acceptedAt;
            return Mathf.Max(0f, deadlineSeconds - elapsed);
        }
    }
}

public class JobManager : MonoBehaviour
{
    [Header("Products")]
    public List<ItemSO> productItems = new List<ItemSO>();

    [Header("Customer Slots")]
    public int customerSlots = 3;

    [Header("Combo Settings")]
    public int minLinesPerJob = 1;
    public int maxLinesPerJob = 3;
    public int minQuantityPerLine = 1;
    public int maxQuantityPerLine = 4;

    [Header("Time Settings")]
    public float minJobSeconds = 60f;
    public float maxJobSeconds = 600f;
    public int minComplexity = 1;
    public int maxComplexity = 20;

    [Header("Reward Settings")]
    public float basePayPerItem = 20f;
    public int baseXpPerJob = 50;

    [Header("UI")]
    public JobBoardUI jobBoardUI;

    [Header("Customers")]
    public CustomerSpawner worldSpawner;
    [SerializeField] private ItemSO chairItemSO;
    [SerializeField] private bool isTutorialMode = false;

    readonly List<JobOrder> availableJobs = new List<JobOrder>();
    readonly List<JobOrder> activeJobs = new List<JobOrder>();

    public IReadOnlyList<JobOrder> AvailableJobs => availableJobs;
    public IReadOnlyList<JobOrder> ActiveJobs => activeJobs;

    void Start()
    {
        GenerateInitialJobs();
        NotifyChanged();
    }

    void Update()
    {
        bool changed = false;

        for (int i = 0; i < activeJobs.Count; i++)
        {
            var job = activeJobs[i];
            if (job.isAccepted && !job.isCompleted && !job.isFailed)
            {
                if (job.RemainingSeconds <= 0f)
                {
                    job.isFailed = true;
                    changed = true;
                    HandleJobResolved(job, false);
                }
            }
        }

        if (changed && jobBoardUI && jobBoardUI.gameObject.activeSelf)
        {
            jobBoardUI.Refresh();
        }
    }

    public void GenerateInitialJobs()
    {
        availableJobs.Clear();
        for (int slot = 0; slot < customerSlots; slot++)
        {
            var kind = GetRandomCustomerKind();
            var job = CreateJob(kind);
            job.slotIndex = slot;
            availableJobs.Add(job);
        }
    }

    CustomerKind GetRandomCustomerKind()
    {
        Array values = Enum.GetValues(typeof(CustomerKind));
        int index = UnityEngine.Random.Range(0, values.Length);
        return (CustomerKind)values.GetValue(index);
    }

    JobOrder CreateJob(CustomerKind kind)
    {
        var job = new JobOrder();
        job.id = "JOB_" + Guid.NewGuid().ToString("N");
        job.customer = kind;

        int lineCount = GetLineCountFor(kind);
        lineCount = Mathf.Clamp(lineCount, minLinesPerJob, maxLinesPerJob);

        var used = new HashSet<ItemSO>();

        for (int i = 0; i < lineCount; i++)
        {
            var line = new JobLine();

            if (isTutorialMode)
            {
                line.product = chairItemSO;
            }
            else
            {
                line.product = GetRandomProduct(used);
            }

            if (line.product != null)
            {
                used.Add(line.product);
            }

            int qty = GetQuantityFor(kind);
            qty = Mathf.Clamp(qty, minQuantityPerLine, maxQuantityPerLine);
            line.quantity = Mathf.Max(1, qty);

            job.lines.Add(line);
        }

        if (job.TotalQuantity <= 0 && productItems.Count > 0)
        {
            var fallback = new JobLine();
            fallback.product = productItems[0];
            fallback.quantity = 1;
            job.lines.Add(fallback);
        }

        SetupJobTime(job);
        return job;
    }

    void SetupJobTime(JobOrder job)
    {
        int total = job.TotalQuantity;
        if (total <= 0) total = 1;

        int complexity = Mathf.Clamp(total, minComplexity, maxComplexity);

        float t01 = 0f;
        if (maxComplexity > minComplexity)
        {
            t01 = (complexity - minComplexity) /
                  (float)(maxComplexity - minComplexity);
        }

        float seconds = Mathf.Lerp(minJobSeconds, maxJobSeconds, t01);
        float multiplier = GetTimeMultiplierFor(job.customer);

        seconds *= multiplier;
        seconds = Mathf.Clamp(seconds, minJobSeconds, maxJobSeconds);
        job.deadlineSeconds = seconds;
    }

    float GetTimeMultiplierFor(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Charlie:
                return 0.7f;
            case CustomerKind.Sponge:
                return 0.9f;
            case CustomerKind.Brandon:
                return 1.2f;
            case CustomerKind.Gabby:
            default:
                return 1f;
        }
    }

    ItemSO GetRandomProduct(HashSet<ItemSO> used)
    {
        if (productItems == null || productItems.Count == 0) return null;

        if (used != null && used.Count < productItems.Count)
        {
            for (int i = 0; i < 8; i++)
            {
                int index = UnityEngine.Random.Range(0, productItems.Count);
                var candidate = productItems[index];
                if (!used.Contains(candidate)) return candidate;
            }
        }

        int fallbackIndex = UnityEngine.Random.Range(0, productItems.Count);
        return productItems[fallbackIndex];
    }

    int GetLineCountFor(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Sponge:
                return UnityEngine.Random.Range(1, 3);
            case CustomerKind.Brandon:
                return UnityEngine.Random.Range(2, 4);
            default:
                return UnityEngine.Random.Range(1, 4);
        }
    }

    int GetQuantityFor(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Sponge:
                return UnityEngine.Random.Range(1, 3);
            case CustomerKind.Brandon:
                return UnityEngine.Random.Range(2, 6);
            case CustomerKind.Charlie:
            case CustomerKind.Gabby:
            default:
                return UnityEngine.Random.Range(1, 5);
        }
    }

    public int EstimateGold(JobOrder job)
    {
        if (job == null) return 0;
        int total = job.TotalQuantity;
        float baseTotal = basePayPerItem * total;
        return Mathf.RoundToInt(baseTotal);
    }

    public void AcceptJob(JobOrder job)
    {
        if (job == null) return;
        if (job.isAccepted) return;
        if (!availableJobs.Contains(job)) return;

        job.isAccepted = true;
        job.acceptedAt = Time.time;
        availableJobs.Remove(job);
        activeJobs.Add(job);

        NotifyChanged();
    }

    public void DeclineJob(JobOrder job)
    {
        if (job == null) return;
        if (!availableJobs.Contains(job)) return;

        int slot = job.slotIndex;

        availableJobs.Remove(job);

        if (slot >= 0)
        {
            SpawnNewJobForSlot(slot);
        }

        NotifyChanged();
    }

    // called by the production machine when it finishes building one item
    public void ReportProductBuilt(ItemSO product, bool misfit)
    {
        if (product == null) return;

        bool changed = false;

        for (int i = 0; i < activeJobs.Count; i++)
        {
            var job = activeJobs[i];
            if (!job.isAccepted || job.isCompleted || job.isFailed) continue;

            bool matched = false;

            for (int j = 0; j < job.lines.Count; j++)
            {
                var line = job.lines[j];
                if (line == null) continue;
                if (line.product != product) continue;
                if (line.producedCount >= line.quantity) continue;

                line.producedCount++;
                if (misfit) job.misfitCount++;
                matched = true;
                break;
            }

            if (matched)
            {
                if (job.TotalProduced >= job.TotalQuantity)
                {
                    job.isReadyForDelivery = true;
                }
                changed = true;
                break;
            }
        }

        if (changed && jobBoardUI && jobBoardUI.gameObject.activeSelf)
        {
            jobBoardUI.Refresh();
        }
    }

    // called by the truck UI when the player has dragged items and pressed Deliver
    public void DeliverJob(JobOrder job)
    {
        if (job == null) return;
        if (job.isCompleted || job.isFailed) return;

        if (!job.isReadyForDelivery)
        {
            Debug.Log("Tried to deliver job that is not ready for delivery.");
            return;
        }

        CompleteJob(job);
    }

    void CompleteJob(JobOrder job)
    {
        if (job == null) return;
        if (job.isCompleted) return;

        job.isCompleted = true;

        int totalQuantity = job.TotalQuantity;
        float baseTotal = basePayPerItem * totalQuantity;

        float stars = job.StarValue;
        float starFactor = stars / 3f;

        float pay = baseTotal * starFactor;

        float xp = baseXpPerJob;
        float xpMultiplier = Mathf.Max(0f, 1f - 0.1f * job.misfitCount);
        xp *= xpMultiplier;

        switch (job.customer)
        {
            case CustomerKind.Charlie:
                pay *= 1.2f;
                break;
            case CustomerKind.Gabby:
                if (stars >= 3f)
                {
                    pay *= 1.4f;
                }
                break;
            case CustomerKind.Sponge:
                break;
            case CustomerKind.Brandon:
                if (totalQuantity >= 5 && stars >= 3f)
                {
                    pay *= 1.5f;
                    xp += 25f;
                }
                break;
        }

        job.goldReward = Mathf.RoundToInt(pay);
        job.xpReward = Mathf.RoundToInt(xp);

        Inventory inv = FindFirstObjectByType<Inventory>();
        if (inv != null)
        {
            if (job.goldReward > 0) inv.AddMoney(job.goldReward);
            if (job.xpReward > 0) inv.AddXp(job.xpReward);
        }

        HandleJobResolved(job, true);

        Debug.Log("Job " + job.id +
                  " delivered. Customer=" + job.customer +
                  " Pay=" + job.goldReward +
                  " XP=" + job.xpReward +
                  " Stars=" + job.StarValue);
    }

    void HandleJobResolved(JobOrder job, bool succeeded)
    {
        if (!succeeded)
        {
            //for failure reduce
            Inventory inv = FindFirstObjectByType<Inventory>();
            if (inv != null)
            {
                // Reduce money by 50 
                inv.AddMoney(-50f);

                // Reduce XP by 10
                inv.AddXp(-10);
            }
            // Remove failed job from active list
            if (activeJobs.Contains(job))
                activeJobs.Remove(job);
        }
        if (job.slotIndex >= 0)
        {
            SpawnNewJobForSlot(job.slotIndex);
        }

        NotifyChanged();
    }
    void SpawnNewJobForSlot(int slotIndex)
    {
        CustomerKind kind = GetRandomCustomerKind();
        var newJob = CreateJob(kind);
        newJob.slotIndex = slotIndex;
        availableJobs.Add(newJob);
    }
   public void NotifyChanged()
    {
        if (worldSpawner)
        {
            worldSpawner.SyncCustomers(availableJobs);
        }

        if (jobBoardUI && jobBoardUI.gameObject.activeSelf)
        {
            jobBoardUI.Refresh();
        }
    }
    public void AddJob(JobOrder job)
    {
        availableJobs.Add(job);
        NotifyChanged(); 
    }

}
