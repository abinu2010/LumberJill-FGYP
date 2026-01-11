using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager current;
    public Canvas canvas;
    public bool usingSimulatedData = true;

    private void Awake()
    {
        current = this;
        PlayerController.IsInputLocked = false; // extra safety
    }
}
