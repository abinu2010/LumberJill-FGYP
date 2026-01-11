using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TreePlot : MonoBehaviour
{
    [Header("Identity")]
    public string plotId = "Plot_01";

    [Header("Items")]
    public ItemSO seedItem;
    public ItemSO logItem;

    [Header("Economy")]
    [Min(1)] public int seedsPerTree = 3;
    [Min(1)] public int logsPerHarvest = 10;

    [Header("Growth Minutes")]
    [Min(0.1f)] public float growMinutes = 3f;

    [Header("Visuals")]
    public GameObject emptyVisual;
    public GameObject growingVisual;
    public GameObject grownVisual;

    [Header("UI References")]
    public Slider progressSlider;
    public TextMeshProUGUI timeLabel;
    public TextMeshProUGUI statusText;
    public Button harvestButton;

    [Header("Storage FX")]
    public Canvas overlayCanvas;
    public RectTransform storageAnchor;
    public Sprite logIcon;
    public Vector2 flyIconSize = new Vector2(36, 36);
    public float flyDuration = 0.7f;
    public int flyBurst = 6;
    public AnimationCurve flyCurve;
    public string overlayCanvasTag = "OverlayCanvas";
    public string storageAnchorName = "StorageAnchor";

    private StorageManager storage;
    private float growTimer;
    private float growSeconds;

    private enum State { Empty, Growing, Ready }
    private State state = State.Empty;

    void Awake()
    {
        storage = FindFirstObjectByType<StorageManager>();
        growSeconds = growMinutes * 60f;
        if (flyCurve == null || flyCurve.keys.Length == 0)
            flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (harvestButton)
        {
            harvestButton.onClick.RemoveAllListeners();
            harvestButton.onClick.AddListener(Harvest);
        }

        if (progressSlider)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        AutoWireStorage();
        SetState(State.Empty);
    }

    void Update()
    {
        if (state == State.Growing)
            TickGrowth();
    }

    void OnMouseDown()
    {
        // Only plant by tap
        if (state == State.Empty)
            Plant();
        // No harvest here now
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleTapOrClick(touch.position);
            }
        }
    }
    void HandleTapOrClick(Vector2 screenPosition)
    {
        // Check if tapping on UI
        if (EventSystem.current != null && IsPointerOverUI(screenPosition)) return;

        // Raycast to check if this object was tapped
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                // Only plant by tap when empty
                if (state == State.Empty)
                    Plant();
            }
        }
    }

    bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    void Plant()
    {
        if (!storage || seedItem == null) return;

        int have = storage.GetCount(seedItem);
        if (have < seedsPerTree)
        {
            if (statusText) statusText.text = "Need more seeds";
            return;
        }

        storage.Take(seedItem, seedsPerTree);
        growSeconds = growMinutes * 60f;
        growTimer = 0f;
        SetState(State.Growing);
    }

    void TickGrowth()
    {
        growTimer += Time.deltaTime;
        float t = Mathf.Clamp01(growTimer / Mathf.Max(0.01f, growSeconds));

        if (progressSlider) progressSlider.value = t;

        if (timeLabel)
        {
            float remain = Mathf.Max(0f, growSeconds - growTimer);
            int mins = (int)(remain / 60f);
            int secs = (int)(remain % 60f);
            timeLabel.text = $"{mins:00}:{secs:00}";
        }

        if (growTimer >= growSeconds)
            SetState(State.Ready);
    }

    void Harvest()
    {
        if (!storage || logItem == null)
        {
            SetState(State.Empty);
            return;
        }

        storage.Put(logItem, logsPerHarvest);
        Vector3 startWorld = transform.position + Vector3.up * 1.6f;

        if (overlayCanvas && storageAnchor && logIcon)
            StartCoroutine(FlyToStorageRoutine(startWorld, logsPerHarvest));

        SetState(State.Empty);
    }

    void SetState(State newState)
    {
        state = newState;

        if (emptyVisual) emptyVisual.SetActive(state == State.Empty);
        if (growingVisual) growingVisual.SetActive(state == State.Growing);
        if (grownVisual) grownVisual.SetActive(state == State.Ready);

        if (progressSlider) progressSlider.gameObject.SetActive(state == State.Growing);
        if (timeLabel) timeLabel.gameObject.SetActive(state == State.Growing);
        if (harvestButton) harvestButton.gameObject.SetActive(state == State.Ready);

        if (statusText)
        {
            if (state == State.Empty) statusText.text = "Tap tree stump to plant";
            else if (state == State.Growing) statusText.text = "Tree growing";
            else if (state == State.Ready) statusText.text = "Tree ready";
        }
    }

    void AutoWireStorage()
    {
        if (overlayCanvas == null)
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!c || !c.gameObject.scene.IsValid()) continue;
                if (!string.IsNullOrEmpty(overlayCanvasTag) &&
                    c.CompareTag(overlayCanvasTag))
                {
                    overlayCanvas = c;
                    break;
                }

                if (c.renderMode == RenderMode.ScreenSpaceOverlay ||
                    c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    overlayCanvas = c;
                    break;
                }
            }
        }

        if (overlayCanvas && storageAnchor == null)
        {
            foreach (var rt in overlayCanvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == storageAnchorName)
                {
                    storageAnchor = rt;
                    break;
                }
            }
        }
    }

    IEnumerator FlyToStorageRoutine(Vector3 startWorld, int count)
    {
        if (!overlayCanvas || !storageAnchor || logIcon == null) yield break;

        int icons = Mathf.Clamp(count, 1, flyBurst);
        RectTransform overlayRect = overlayCanvas.transform as RectTransform;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(Camera.main, startWorld);
        Vector2 startLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRect, startScreen, overlayCanvas.worldCamera, out startLocal);

        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(
            overlayCanvas ? overlayCanvas.worldCamera : null, storageAnchor.position);
        Vector2 endLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRect, endScreen, overlayCanvas ? overlayCanvas.worldCamera : null, out endLocal);

        Vector2 control = Vector2.Lerp(startLocal, endLocal, 0.5f) + Vector2.up * 80f;

        for (int i = 0; i < icons; i++)
            StartCoroutine(SingleFlyIcon(startLocal, control, endLocal, i * 0.03f));

        yield return new WaitForSeconds(flyDuration + 0.15f);
    }

    IEnumerator SingleFlyIcon(Vector2 start, Vector2 control, Vector2 end, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        RectTransform overlayRect = overlayCanvas.transform as RectTransform;
        var iconObj = new GameObject("TreeFlyIcon", typeof(RectTransform),
            typeof(CanvasGroup), typeof(Image));
        iconObj.transform.SetParent(overlayRect, false);

        var iconRc = iconObj.GetComponent<RectTransform>();
        iconRc.sizeDelta = flyIconSize;
        iconRc.anchoredPosition = start;

        var img = iconObj.GetComponent<Image>();
        img.sprite = logIcon;
        img.raycastTarget = false;

        var grp = iconObj.GetComponent<CanvasGroup>();
        grp.alpha = 1f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, flyDuration);
            float k = Mathf.Clamp01(t);
            float e = flyCurve != null ? flyCurve.Evaluate(k) : k;

            Vector2 p = QuadraticBezier(start, control, end, e);
            iconRc.anchoredPosition = p;

            float s = Mathf.Lerp(1.0f, 0.65f, e);
            iconRc.localScale = new Vector3(s, s, 1f);
            grp.alpha = 1f - e * 0.2f;

            yield return null;
        }

        Destroy(iconObj);
    }

    static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }
}
