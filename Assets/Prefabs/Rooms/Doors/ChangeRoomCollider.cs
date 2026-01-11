using UnityEngine;

public class ChangeRoomCollider : MonoBehaviour
{
    private GameObject changeRoomButton;
    
    void Start()
    {
        changeRoomButton = transform.parent.GetComponentInChildren<RectTransform>().Find("changeRoomButton").gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            changeRoomButton.SetActive(true);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            changeRoomButton.SetActive(false);
        }
    }
}
