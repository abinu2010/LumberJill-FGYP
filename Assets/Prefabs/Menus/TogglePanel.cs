using UnityEngine;
using UnityEngine.Events;

public class TogglePanel : MonoBehaviour
{
    [SerializeField] private GameObject panel, UIBlocker;
    private bool panelOpen = false;
    
    public UnityEvent Activated;

    public void OpenPanel()
    {
        if(!panelOpen) 
        {
            if (panel != null)
                panel.SetActive(true);

            if (UIBlocker != null)
            {
                UIBlocker.SetActive(true);

                // Try to ensure blocker is behind the panel so the panel's controls remain interactive
                var parent = panel != null ? panel.transform.parent : null;
                if (parent != null)
                {
                    UIBlocker.transform.SetParent(parent, false);
                    int panelIndex = panel.transform.GetSiblingIndex();
                    // place blocker at same index as panel (behind it) and move panel to top
                    UIBlocker.transform.SetSiblingIndex(panelIndex);
                    panel.transform.SetAsLastSibling();
                }
            }

            Activated.Invoke();
            panelOpen = true;
        }
    }

    public void ClosePanel()
    {
        if(panelOpen) 
        {
            if (panel != null)
                panel.SetActive(false);
            if (UIBlocker != null)
                UIBlocker.SetActive(false);
            panelOpen = false;
        }
    }
}
