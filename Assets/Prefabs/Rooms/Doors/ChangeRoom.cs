using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.AI.Navigation;

public class ChangeRoom : MonoBehaviour
{
    private GameObject isoCamera;
    private GameObject changeRoomButton;
    private GameObject currentRoom;
    private GameObject player;
    [SerializeField] private GameObject nextRoom;
    [SerializeField] private string roomName;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private Vector3 cameraSpawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isoCamera = GameObject.Find("IsometricCamera");
        currentRoom = this.transform.parent.gameObject;
        changeRoomButton = GetComponentInChildren<RectTransform>().Find("changeRoomButton").gameObject;
        changeRoomButton.GetComponentInChildren<TMP_Text>().text = "go to " + roomName;
        changeRoomButton.SetActive(false);
        Button button = changeRoomButton.GetComponent<Button>();
        button.onClick.AddListener(OnChangeRoomButtonPressed);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnChangeRoomButtonPressed()
    {
        //currentRoom.SetActive(false);
        //nextRoom.SetActive(true);
        UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.ResetPath();
        agent.Warp(spawnPoint.transform.position);
        isoCamera.transform.position = cameraSpawnPoint;
    }
}