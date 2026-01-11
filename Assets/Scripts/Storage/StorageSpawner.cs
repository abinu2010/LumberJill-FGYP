using UnityEngine;

public class StorageSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cylinderPrefab;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 0.5f, 0);

    private void Start()
    {
        if (cylinderPrefab != null)
            Instantiate(cylinderPrefab, spawnPosition, Quaternion.identity);
        else
            Debug.LogError("StorageSpawner: No cylinder prefab assigned.");
    }
}
