using UnityEngine;

public class StorageTabSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject utilitiesPage;
    [SerializeField] private GameObject productsPage;

    private void Start()
    {
        ShowUtilities();
    }

    public void ShowUtilities()
    {
        if (utilitiesPage == null || productsPage == null) return;

        utilitiesPage.SetActive(true);
        productsPage.SetActive(false);
    }

    public void ShowProducts()
    {
        if (utilitiesPage == null || productsPage == null) return;

        utilitiesPage.SetActive(false);
        productsPage.SetActive(true);
    }
}
