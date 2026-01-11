using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CustomerFBX : MonoBehaviour
{
    public SpriteRenderer alertIcon;
    public CustomerCardUI orderPopup;

    JobManager jobManager;
    JobOrder job;

    void LateUpdate()
    {
        if (alertIcon != null)
        {
            alertIcon.transform.forward = Camera.main.transform.forward;
        }
    }
    public void Setup(JobManager manager, JobOrder order)
    {
        jobManager = manager;
        job = order;
        if (alertIcon != null)
            alertIcon.enabled = (order != null);

        if (orderPopup == null)
        {
            orderPopup = FindFirstObjectByType<CustomerCardUI>();
        }
    }
    void OnMouseDown()
    {
        if (PlayerController.IsInputLocked)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (jobManager == null || job == null) return;

        if (orderPopup == null)
        {
            orderPopup = FindFirstObjectByType<CustomerCardUI>();
        }

        if (orderPopup != null)
        {
            orderPopup.Show(jobManager, job, this);
        }
    }

}
