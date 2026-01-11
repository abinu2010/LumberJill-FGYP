using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JobBoard : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public JobBoardUI jobBoard;

    void OnMouseDown()
    {
        if (!jobBoard) return;
        if (jobBoard.gameObject.activeSelf) jobBoard.Close();
        else jobBoard.Open();
    }
}
