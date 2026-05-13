using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private readonly HashSet<GameObject> openPanels = new HashSet<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshInputLock();
    }

    private void LateUpdate()
    {
        RemoveDeadPanels();
        RefreshInputLock();
    }

    public void Open(GameObject panel)
    {
        if (!panel) return;

        panel.SetActive(true);
        openPanels.Add(panel);
        RefreshInputLock();
    }

    public void Close(GameObject panel)
    {
        if (!panel) return;

        panel.SetActive(false);
        openPanels.Remove(panel);
        RemoveDeadPanels();
        RefreshInputLock();
    }

    public void Toggle(GameObject panel)
    {
        if (!panel) return;

        if (panel.activeSelf)
            Close(panel);
        else
            Open(panel);
    }

    public bool IsOpen(GameObject panel)
    {
        if (!panel) return false;
        return panel.activeInHierarchy || openPanels.Contains(panel);
    }

    public void ForceRefresh()
    {
        RemoveDeadPanels();
        RefreshInputLock();
    }

    private void RemoveDeadPanels()
    {
        openPanels.RemoveWhere(panel => panel == null || !panel.activeInHierarchy);
    }

    private void RefreshInputLock()
    {
        foreach (GameObject panel in openPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                PlayerController.IsInputLocked = true;
                return;
            }
        }

        PlayerController.IsInputLocked = false;
    }
}
