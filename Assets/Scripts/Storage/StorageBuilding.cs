using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class StorageBuilding : MonoBehaviour
{
    [Header("Assign In Inspector")]
    [SerializeField] private GameObject windowPrefab;
    [SerializeField] private GameObject storageButtonPrefab;
    [SerializeField] private Vector3 buttonOffset = new Vector3(0, 2f, 0);

    private Canvas canvas;
    private Camera cam;

    private GameObject window;
    private GameObject button;
    private RectTransform buttonRect;

    void Awake()
    {
        EnsureEventSystem();
    }

    void Start()
    {
        cam = Camera.main;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null && canvases.Length > 0)
            canvas = canvases[0];

        // Create storage window
        if (windowPrefab)
        {
            window = Instantiate(windowPrefab, canvas.transform);
            window.SetActive(false);
        }
    }

    void Update()
    {
        if (!cam) cam = Camera.main;

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleTapOrClick(touch.position);
            }
        }
        // Handle mouse input (PC fallback)
        else if (Input.GetMouseButtonDown(0))
        {
            HandleTapOrClick(Input.mousePosition);
        }

        UpdateButtonPosition();
    }

    private void HandleTapOrClick(Vector2 screenPosition)
    {
        if (PointerOverUI(screenPosition)) return;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (hit.collider.GetComponentInParent<StorageBuilding>() == this)
            {
                ShowButton();
            }
        }
    }
    private void ShowButton()
    {
        if (button != null)
        {
            Destroy(button);
            button = null;
            buttonRect = null;
        }

        button = Instantiate(storageButtonPrefab, canvas.transform);
        buttonRect = button.GetComponent<RectTransform>();

        ForceText(button, "Open Storage");

        button.transform.SetAsLastSibling();

        button.GetComponent<Button>().onClick.AddListener(OpenWindow);
    }

    private void UpdateButtonPosition()
    {
        if (buttonRect == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + buttonOffset);

        if (screenPos.z < 0)
        {
            buttonRect.gameObject.SetActive(false);
            return;
        }

        if (!buttonRect.gameObject.activeSelf)
            buttonRect.gameObject.SetActive(true);

        buttonRect.position = screenPos;
    }


    private void OpenWindow()
    {
        if (window != null)
            window.SetActive(true);

        if (button != null)
            Destroy(button);

        button = null;
        buttonRect = null;

        PlayerController.IsInputLocked = true;
    }

    public void CloseWindow()
    {
        if (window != null)
            window.SetActive(false);

        PlayerController.IsInputLocked = false;
    }

    void OnDisable()
    {
        if (button != null)
            Destroy(button);
    }

    private bool PointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        // Check for touch
        if (Input.touchCount > 0)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            return results.Count > 0;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ForceText(GameObject root, string fallback)
    {
        var tmp = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp)
        {
            if (string.IsNullOrWhiteSpace(tmp.text)) tmp.text = fallback;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 36;
            tmp.color = Color.black;
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));

        DontDestroyOnLoad(es);
    }
}