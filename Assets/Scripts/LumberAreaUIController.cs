using UnityEngine;

public class LumberAreaUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject yardHUD;

    private int playersInside = 0;

    void Awake()
    {
        if (yardHUD)
            yardHUD.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInside++;

        if (playersInside == 1)
            ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInside--;

        if (playersInside <= 0)
            HideUI();
    }

    private void ShowUI()
    {
        if (yardHUD)
            yardHUD.SetActive(true);
    }

    private void HideUI()
    {
        if (yardHUD)
            yardHUD.SetActive(false);
    }
}
