using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageUI : MonoBehaviour
{
   // [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);

        PlayerController.IsInputLocked = false;
    }
}
