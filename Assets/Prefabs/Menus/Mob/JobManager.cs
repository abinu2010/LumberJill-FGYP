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
                JobLine line = lines[i];
                if (line != null)
                    total += Mathf.Max(0, line.quantity);
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
                JobLine line = lines[i];
                if (line != null)
                    total += Mathf.Max(0, line.producedCount);
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
    public const string TutorialJobId = "JOB_TUTORIAL";

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

    private readonly List<JobOrder> availableJobs = new List<JobOrder>();
    private readonly List<JobOrder> activeJobs = new List<JobOrder>();
    private bool tutorialJobCompleted;

    public IReadOnlyList<JobOrder> AvailableJobs => availableJobs;
    public IReadOnlyList<JobOrder> ActiveJobs => activeJobs;
    public bool IsTutorialMode => isTutorialMode;
    public bool TutorialJobCompleted => tutorialJobCompleted;

    private void Start()
    {
        if (!isTutorialMode)
            GenerateInitialJobs();
        else
            NotifyChanged();
    }

    private void Update()
    {
        bool changed = false;

        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            JobOrder job = activeJobs[i];
            if (job == null) continue;

            if (job.isAccepted && !job.isCompleted && !job.isFailed && job.RemainingSeconds <= 0f)
            {
                job.isFailed = true;
                changed = true;
                HandleJobResolved(job, false);
            }
        }

        if (changed && jobBoardUI && jobBoardUI.gameObject.activeSelf)
            jobBoardUI.Refresh();
    }

    public void BeginTutorialMode()
    {
        isTutorialMode = true;
        tutorialJobCompleted = false;
        availableJobs.Clear();
        activeJobs.Clear();
        customerSlots = 0;
        NotifyChanged();
    }

    public void StartTutorialJob(ItemSO tutorialProduct)
    {
        isTutorialMode = true;
        tutorialJobCompleted = false;
        availableJobs.Clear();
        activeJobs.Clear();
        customerSlots = 1;

        ItemSO product = tutorialProduct != null ? tutorialProduct : chairItemSO;
        if (product == null && productItems != null && productItems.Count > 0)
            product = productItems[0];

        JobOrder job = new JobOrder();
        job.id = TutorialJobId;
        job.customer = CustomerKind.Charlie;
        job.deadlineSeconds = Mathf.Max(60f, maxJobSeconds > 0f ? maxJobSeconds : 360f);
        job.slotIndex = 0;
        job.goldReward = 50;
        job.xpReward = 0;

        if (product != null)
        {
            JobLine line = new JobLine();
            line.product = product;
            line.quantity = 1;
            job.lines.Add(line);
        }

        availableJobs.Add(job);
        NotifyChanged();
    }

    public void EndTutorialAndStartRealGame()
    {
        isTutorialMode = false;
        availableJobs.Clear();
        activeJobs.Clear();
        customerSlots = Mathf.Max(3, customerSlots);
        GenerateInitialJobs();
        NotifyChanged();
    }

    public void GenerateInitialJobs()
    {
        availableJobs.Clear();

        if (isTutorialMode)
        {
            NotifyChanged();
            return;
        }

        int slots = Mathf.Max(0, customerSlots);
        for (int slot = 0; slot < slots; slot++)
        {
            CustomerKind kind = GetRandomCustomerKind();
            JobOrder job = CreateJob(kind);
            job.slotIndex = slot;
            availableJobs.Add(job);
        }

        NotifyChanged();
    }

    private CustomerKind GetRandomCustomerKind()
    {
        Array values = Enum.GetValues(typeof(CustomerKind));
        int index = UnityEngine.Random.Range(0, values.Length);
        return (CustomerKind)values.GetValue(index);
    }

    private JobOrder CreateJob(CustomerKind kind)
    {
        JobOrder job = new JobOrder();
        job.id = "JOB_" + Guid.NewGuid().ToString("N");
        job.customer = kind;

        int safeMinLines = Mathf.Max(1, minLinesPerJob);
        int safeMaxLines = Mathf.Max(safeMinLines, maxLinesPerJob);
        int lineCount = Mathf.Clamp(GetLineCountFor(kind), safeMinLines, safeMaxLines);

        HashSet<ItemSO> used = new HashSet<ItemSO>();

        for (int i = 0; i < lineCount; i++)
        {
            JobLine line = new JobLine();
            line.product = GetRandomProduct(used);

            if (line.product != null)
                used.Add(line.product);

            int safeMinQuantity = Mathf.Max(1, minQuantityPerLine);
            int safeMaxQuantity = Mathf.Max(safeMinQuantity, maxQuantityPerLine);
            line.quantity = Mathf.Clamp(GetQuantityFor(kind), safeMinQuantity, safeMaxQuantity);
            job.lines.Add(line);
        }

        if (job.TotalQuantity <= 0 && productItems != null && productItems.Count > 0)
        {
            JobLine fallback = new JobLine();
            fallback.product = productItems[0];
            fallback.quantity = 1;
            job.lines.Add(fallback);
        }

        SetupJobTime(job);
        return job;
    }

    private void SetupJobTime(JobOrder job)
    {
        int total = Mathf.Max(1, job.TotalQuantity);
        int complexity = Mathf.Clamp(total, minComplexity, maxComplexity);

        float t01 = 0f;
        if (maxComplexity > minComplexity)
            t01 = (complexity - minComplexity) / (float)(maxComplexity - minComplexity);

        float seconds = Mathf.Lerp(minJobSeconds, maxJobSeconds, t01);
        seconds *= GetTimeMultiplierFor(job.customer);
        job.deadlineSeconds = Mathf.Clamp(seconds, minJobSeconds, maxJobSeconds);
    }

    private float GetTimeMultiplierFor(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Charlie: return 0.7f;
            case CustomerKind.Sponge: return 0.9f;
            case CustomerKind.Brandon: return 1.2f;
            default: return 1f;
        }
    }

    private ItemSO GetRandomProduct(HashSet<ItemSO> used)
    {
        if (productItems == null || productItems.Count == 0) return null;

        if (used != null && used.Count < productItems.Count)
        {
            for (int i = 0; i < 12; i++)
            {
                int index = UnityEngine.Random.Range(0, productItems.Count);
                ItemSO candidate = productItems[index];
                if (candidate != null && !used.Contains(candidate)) return candidate;
            }
        }

        int fallbackIndex = UnityEngine.Random.Range(0, productItems.Count);
        return productItems[fallbackIndex];
    }

    private int GetLineCountFor(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Sponge: return UnityEngine.Random.Range(1, 3);
            case CustomerKind.Brandon: return UnityEngine.Random.Range(2, 4);
            default: return UnityEngine.Random.Range(1, 4);
        }
    }

    private int GetQuantityFor(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Sponge: return UnityEngine.Random.Range(1, 3);
            case CustomerKind.Brandon: return UnityEngine.Random.Range(2, 6);
            default: return UnityEngine.Random.Range(1, 5);
        }
    }

    public int EstimateGold(JobOrder job)
    {
        if (job == null) return 0;
        int total = Mathf.Max(1, job.TotalQuantity);
        return Mathf.RoundToInt(basePayPerItem * total);
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

        if (isTutorialMode)
        {
            StartTutorialJob(GetFirstProductFrom(job));
            return;
        }

        if (slot >= 0)
            SpawnNewJobForSlot(slot);

        NotifyChanged();
    }

    public void ReportProductBuilt(ItemSO product, bool misfit)
    {
        if (product == null) return;

        bool changed = false;

        for (int i = 0; i < activeJobs.Count; i++)
        {
            JobOrder job = activeJobs[i];
            if (job == null) continue;
            if (!job.isAccepted || job.isCompleted || job.isFailed) continue;

            bool matched = false;

            for (int j = 0; j < job.lines.Count; j++)
            {
                JobLine line = job.lines[j];
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
                    job.isReadyForDelivery = true;

                changed = true;
                break;
            }
        }

        if (changed && jobBoardUI && jobBoardUI.gameObject.activeSelf)
            jobBoardUI.Refresh();
    }

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

    private void CompleteJob(JobOrder job)
    {
        if (job == null) return;
        if (job.isCompleted) return;

        job.isCompleted = true;

        int totalQuantity = Mathf.Max(1, job.TotalQuantity);
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
                if (stars >= 3f) pay *= 1.4f;
                break;
            case CustomerKind.Brandon:
                if (totalQuantity >= 5 && stars >= 3f)
                {
                    pay *= 1.5f;
                    xp += 25f;
                }
                break;
        }

        if (job.id == TutorialJobId)
        {
            pay = Mathf.Max(pay, 50f);
            xp = Mathf.Max(0f, xp);
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
    }

    private void HandleJobResolved(JobOrder job, bool succeeded)
    {
        if (job == null) return;

        if (!succeeded)
        {
            Inventory inv = FindFirstObjectByType<Inventory>();
            if (inv != null)
            {
                inv.AddMoney(-50f);
                inv.AddXp(-10);
            }
        }

        if (activeJobs.Contains(job))
            activeJobs.Remove(job);

        if (job.id == TutorialJobId)
        {
            tutorialJobCompleted = succeeded;
            NotifyChanged();
            return;
        }

        if (!isTutorialMode && job.slotIndex >= 0)
            SpawnNewJobForSlot(job.slotIndex);

        NotifyChanged();
    }

    private void SpawnNewJobForSlot(int slotIndex)
    {
        if (isTutorialMode) return;

        CustomerKind kind = GetRandomCustomerKind();
        JobOrder newJob = CreateJob(kind);
        newJob.slotIndex = slotIndex;
        availableJobs.Add(newJob);
    }

    public void NotifyChanged()
    {
        if (worldSpawner)
            worldSpawner.SyncCustomers(availableJobs);

        if (jobBoardUI && jobBoardUI.gameObject.activeSelf)
            jobBoardUI.Refresh();
    }

    public void AddJob(JobOrder job)
    {
        if (job == null) return;

        if (job.slotIndex < 0)
            job.slotIndex = FindFreeSlotIndex();

        availableJobs.Add(job);
        NotifyChanged();
    }

    private int FindFreeSlotIndex()
    {
        int slots = Mathf.Max(1, customerSlots);

        for (int i = 0; i < slots; i++)
        {
            bool used = false;

            for (int j = 0; j < availableJobs.Count; j++)
            {
                if (availableJobs[j] != null && availableJobs[j].slotIndex == i)
                {
                    used = true;
                    break;
                }
            }

            if (!used) return i;
        }

        return 0;
    }

    private ItemSO GetFirstProductFrom(JobOrder job)
    {
        if (job != null && job.lines != null && job.lines.Count > 0 && job.lines[0] != null)
            return job.lines[0].product;

        if (chairItemSO != null) return chairItemSO;
        if (productItems != null && productItems.Count > 0) return productItems[0];
        return null;
    }
}
