using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public JobManager jobManager;
    public Transform[] deskPoints;

    public Transform customersRoot;

    public GameObject charliePrefab;
    public GameObject gabbyPrefab;
    public GameObject spongePrefab;
    public GameObject brandonPrefab;

    Dictionary<int, CustomerFBX> avatarsBySlot = new Dictionary<int, CustomerFBX>();

    void Awake()
    {
        if (jobManager != null)
        {
            jobManager.worldSpawner = this;
        }

        EnsureCustomersRoot();
    }

    void EnsureCustomersRoot()
    {
        if (customersRoot != null) return;

        GameObject existing = GameObject.Find("CustomersRoot");
        if (existing == null) existing = new GameObject("CustomersRoot");
        customersRoot = existing.transform;
    }

    public void SyncCustomers(IReadOnlyList<JobOrder> availableJobs)
    {
        EnsureCustomersRoot();

        HashSet<int> usedSlots = new HashSet<int>();

        for (int i = 0; i < availableJobs.Count; i++)
        {
            var job = availableJobs[i];
            if (job == null) continue;

            int slot = job.slotIndex;
            usedSlots.Add(slot);
            EnsureAvatarForJob(slot, job);
        }

        List<int> toRemove = new List<int>();

        foreach (var kvp in avatarsBySlot)
        {
            if (!usedSlots.Contains(kvp.Key))
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            avatarsBySlot.Remove(toRemove[i]);
        }
    }

    void EnsureAvatarForJob(int slotIndex, JobOrder job)
    {
        if (deskPoints == null || slotIndex < 0 || slotIndex >= deskPoints.Length) return;

        CustomerFBX avatar;
        if (avatarsBySlot.TryGetValue(slotIndex, out avatar))
        {
            if (avatar != null)
            {
                avatar.Setup(jobManager, job);
                return;
            }
        }

        Transform spawnPoint = deskPoints[slotIndex];
        GameObject prefab = GetPrefab(job.customer);
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, customersRoot);

        avatar = go.GetComponent<CustomerFBX>();
        if (avatar == null) avatar = go.AddComponent<CustomerFBX>();

        avatar.Setup(jobManager, job);
        avatarsBySlot[slotIndex] = avatar;
    }

    GameObject GetPrefab(CustomerKind kind)
    {
        switch (kind)
        {
            case CustomerKind.Charlie: return charliePrefab;
            case CustomerKind.Gabby: return gabbyPrefab;
            case CustomerKind.Sponge: return spongePrefab;
            case CustomerKind.Brandon: return brandonPrefab;
            default: return null;
        }
    }
}
