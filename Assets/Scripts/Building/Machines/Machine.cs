using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class Machine : MonoBehaviour, IDropHandler
{
    [Header("Items")]
    public ItemSO logItem;
    public ItemSO plankItem;
    [Min(1)] public int planksPerLog = 2;

    [Header("Processing")]
    [Min(0.1f)] public float secondsPerLog = 0.75f;
    public Transform inputPoint;
    public Transform outputPoint;
    [FormerlySerializedAs("inputVfxPrefab")] public GameObject inputEffectPrefab;
    [FormerlySerializedAs("outputVfxPrefab")] public GameObject outputEffectPrefab;
    [FormerlySerializedAs("vfxLifetime")] public float effectLifetime = 1.5f;

    [Header("Drop Zone (World-Space)")]
    [Min(0.2f)] public float dropZoneWorldSize = 1.2f;
    public bool showDropZone = false;

    [Header("Quantity UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptTitle;
    public Slider promptSlider;
    public TextMeshProUGUI promptValue;
    public Button okButton;
    public Button cancelButton;

    [Header("Pickup Blimp")]
    public bool enableBlimp = true;
    public float blimpHeight = 1.6f;
    public Vector2 blimpSize = new Vector2(140, 90);
    [FormerlySerializedAs("blimpSprite")] public Sprite blimpBackgroundSprite;

    [Header("Work Timer")]
    public bool showTimer = true;
    public Sprite hourglassSprite;
    public float timerHeight = 1.4f;
    public Vector2 timerSize = new Vector2(64, 64);
    public float timerSpinSpeed = 180f;

    [Header("Timer Placement")]
    public bool timerDockToBlimp = true;
    public Vector3 timerLocalOffset = new Vector3(0.28f, 0.00f, 0f);

    [Header("Storage Fly FX")]
    public Canvas overlayCanvas;
    public RectTransform storageAnchor;
    public Sprite plankIcon;
    public Vector2 flyIconSize = new Vector2(36, 36);
    public float flyDuration = 0.7f;
    public int flyBurst = 6;
    public AnimationCurve flyCurve;

    [Header("Auto Wiring")]
    public bool autoFindStorageUI = true;
    public string overlayCanvasTag = "OverlayCanvas";
    public string storageAnchorName = "StorageAnchor";
    public float uiProbeInterval = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator tableSawAnimator;
    [SerializeField] private string animBoolParameter = "IsCutting";

    private StorageManager storage;
    private Placeble placeble;
    private Canvas dropCanvas;
    private bool busy;
    private int pendingPlanks;
    private MachineBlimp blimp;

    private Canvas timerCanvas;
    private RectTransform timerRect;
    private Image timerImage;
    private Transform blimpRoot;

    private float _nextProbeTime;

    private static bool TagDefined(string tagName)
    {
        try { GameObject.FindGameObjectsWithTag(tagName); return true; }
        catch { return false; }
    }

    void Awake()
    {
        storage = FindFirstObjectByType<StorageManager>();
        placeble = GetComponent<Placeble>();
        EnsureDropZone();
        SetupPrompt();
        EnsureBlimp();
        EnsureTimer();

        if (flyCurve == null || flyCurve.keys.Length == 0)
            flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        TryAutoWireStorage();

        if (!tableSawAnimator)
            tableSawAnimator = GetComponentInChildren<Animator>(true);
    }

    void Update()
    {
        if (dropCanvas)
        {
            dropCanvas.enabled = placeble == null || placeble.placed;
            if (dropCanvas.worldCamera == null) dropCanvas.worldCamera = Camera.main;
        }

        if (autoFindStorageUI && Time.unscaledTime >= _nextProbeTime)
        {
            _nextProbeTime = Time.unscaledTime + uiProbeInterval;
            TryAutoWireStorage();
        }

        if (timerCanvas && timerDockToBlimp)
        {
            Vector3 anchor = blimpRoot ? blimpRoot.localPosition : new Vector3(0, blimpHeight, 0);
            timerCanvas.transform.localPosition = anchor + timerLocalOffset;
        }

        if (blimp) blimp.FaceCamera(Camera.main);

        if (timerCanvas && timerCanvas.gameObject.activeSelf)
        {
            FaceCanvas(timerCanvas.transform, Camera.main);
            if (timerRect) timerRect.Rotate(0f, 0f, -timerSpinSpeed * Time.deltaTime);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (busy || (placeble && !placeble.placed)) return;

        var draggable = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemUI>() : null;
        if (!draggable) return;

        var stack = draggable.TakePayload();
        if (stack.IsEmpty || stack.item != logItem)
        {
            draggable.ReturnRemainder(stack);
            return;
        }

        ShowPrompt("Logs  Planks", stack.count,
            useCount =>
            {
                stack.count -= useCount;
                draggable.ReturnRemainder(stack);
                StartCoroutine(ProcessLogs(useCount));
            },
            () => draggable.ReturnRemainder(stack)
        );
    }

    private IEnumerator ProcessLogs(int logs)
    {
        if (logs <= 0 || busy) yield break;
        busy = true;
        SetTimer(true);

        if (tableSawAnimator)
            tableSawAnimator.SetBool(animBoolParameter, true);

        for (int i = 0; i < logs; i++)
        {
            if (inputEffectPrefab && inputPoint)
            {
                var fxIn = Instantiate(inputEffectPrefab, inputPoint.position, Quaternion.identity);
                if (effectLifetime > 0) Destroy(fxIn, effectLifetime);
            }

            yield return new WaitForSeconds(secondsPerLog);

            pendingPlanks += planksPerLog;
            UpdateBlimpUI();

            if (outputEffectPrefab && outputPoint)
            {
                var fxOut = Instantiate(outputEffectPrefab, outputPoint.position, Quaternion.identity);
                if (effectLifetime > 0) Destroy(fxOut, effectLifetime);
            }
        }

        if (tableSawAnimator)
            tableSawAnimator.SetBool(animBoolParameter, false);

        busy = false;
        SetTimer(false);
    }

    private void EnsureBlimp()
    {
        if (!enableBlimp) return;

        var canvasObj = new GameObject("BlimpCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, blimpHeight, 0);

        var c = canvasObj.GetComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.worldCamera = Camera.main;
        c.sortingOrder = 2100;

        var rc = c.GetComponent<RectTransform>();
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = blimpSize;
        float scale = 0.6f / Mathf.Max(1f, blimpSize.x);
        c.transform.localScale = new Vector3(scale, scale, scale);

        var btnObj = new GameObject("Blimp", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvasObj.transform, false);
        var btnRc = btnObj.GetComponent<RectTransform>();
        btnRc.anchorMin = btnRc.anchorMax = btnRc.pivot = new Vector2(0.5f, 0.5f);
        btnRc.sizeDelta = blimpSize;

        var bg = btnObj.GetComponent<Image>();
        if (blimpBackgroundSprite) { bg.sprite = blimpBackgroundSprite; bg.type = Image.Type.Sliced; }
        else bg.color = new Color(0f, 0f, 0f, 0.55f);

        var textObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var textRc = textObj.GetComponent<RectTransform>();
        textRc.anchorMin = textRc.anchorMax = new Vector2(0.5f, 0f);
        textRc.pivot = new Vector2(0.5f, 0f);
        textRc.anchoredPosition = new Vector2(0f, 6f);
        textRc.sizeDelta = new Vector2(blimpSize.x, 28f);

        var countText = textObj.GetComponent<TextMeshProUGUI>();
        countText.text = "x0";
        countText.alignment = TextAlignmentOptions.Center;
        countText.enableAutoSizing = true;
        countText.fontSizeMin = 26; countText.fontSizeMax = 36;
        countText.color = Color.black;

        blimp = canvasObj.AddComponent<MachineBlimp>();
        blimp.Init(this, countText, btnObj.GetComponent<Button>());

        blimpRoot = canvasObj.transform;
        canvasObj.SetActive(false);
    }

    private void EnsureTimer()
    {
        if (!showTimer) return;

        var timerCanvasObj = new GameObject("TimerCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        timerCanvasObj.transform.SetParent(transform, false);

        Vector3 anchor = blimpRoot ? blimpRoot.localPosition : new Vector3(0, blimpHeight, 0);
        timerCanvasObj.transform.localPosition = anchor + timerLocalOffset;

        var c = timerCanvasObj.GetComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.worldCamera = Camera.main;
        c.sortingOrder = 2110;

        var rc = c.GetComponent<RectTransform>();
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = timerSize;
        float scale = 0.4f / Mathf.Max(1f, timerSize.x);
        c.transform.localScale = new Vector3(scale, scale, scale);

        var imgObj = new GameObject("Hourglass", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(timerCanvasObj.transform, false);
        timerRect = imgObj.GetComponent<RectTransform>();
        timerRect.anchorMin = timerRect.anchorMax = timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.sizeDelta = timerSize;

        timerImage = imgObj.GetComponent<Image>();
        timerImage.sprite = hourglassSprite;
        timerImage.color = new Color(1f, 1f, 1f, 0.9f);

        timerCanvas = c;
        timerCanvasObj.SetActive(false);
    }

    private void SetTimer(bool on)
    {
        if (!showTimer || !timerCanvas) return;
        timerCanvas.gameObject.SetActive(on);
        if (timerRect) timerRect.localRotation = Quaternion.identity;
    }

    private void UpdateBlimpUI()
    {
        if (!enableBlimp || !blimp) return;
        blimp.gameObject.SetActive(pendingPlanks > 0);
        blimp.SetCount(pendingPlanks);
    }

    internal void CollectBlimp()
    {
        if (pendingPlanks <= 0) return;
        if (!storage) return;

        Vector3 startWorld = transform.position + new Vector3(0f, blimpHeight, 0f);
        if (blimp) startWorld = blimp.transform.position;

        int collected = pendingPlanks;
        storage.Put(plankItem, pendingPlanks);
        pendingPlanks = 0;
        UpdateBlimpUI();

        if (!overlayCanvas || !storageAnchor) TryAutoWireStorage();
        if (overlayCanvas && storageAnchor && plankIcon)
            StartCoroutine(FlyToStorageRoutine(startWorld, collected));
    }

    private void EnsureDropZone()
    {
        var forwarderExisting = GetComponentInChildren<MachineDropForwarder>(true);
        if (forwarderExisting)
        {
            forwarderExisting.target = this;
            dropCanvas = forwarderExisting.GetComponentInParent<Canvas>();
            return;
        }

        var canvasObj = new GameObject("DropCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        dropCanvas = canvasObj.GetComponent<Canvas>();
        dropCanvas.renderMode = RenderMode.WorldSpace;
        dropCanvas.worldCamera = Camera.main;
        dropCanvas.sortingOrder = 2000;
        dropCanvas.enabled = false;

        var rc = dropCanvas.GetComponent<RectTransform>();
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = new Vector2(300f, 300f);
        float scale = Mathf.Max(0.001f, dropZoneWorldSize / rc.sizeDelta.x);
        dropCanvas.transform.localScale = new Vector3(scale, scale, scale);

        var zoneObj = new GameObject("DropZone", typeof(RectTransform), typeof(Image), typeof(MachineDropForwarder));
        zoneObj.transform.SetParent(canvasObj.transform, false);
        var zoneRc = zoneObj.GetComponent<RectTransform>();
        zoneRc.anchorMin = zoneRc.anchorMax = zoneRc.pivot = new Vector2(0.5f, 0.5f);
        zoneRc.sizeDelta = rc.sizeDelta;
        zoneRc.localPosition = Vector3.zero;

        var img = zoneObj.GetComponent<Image>();
        img.color = showDropZone ? new Color(0f, 1f, 0f, 0.18f) : new Color(1f, 1f, 1f, 0.001f);
        zoneObj.GetComponent<MachineDropForwarder>().target = this;
    }

    private void SetupPrompt()
    {
        if (!promptPanel)
        {
            Transform quantityTransform = null;

            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (!canvas) continue;
                foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (!t) continue;
                    string n = t.name;
                    if (n == "PlankQuantityUI" || n == "QuantityUI" || n.Contains("QuantityUI"))
                    {
                        quantityTransform = t;
                        break;
                    }
                }
                if (quantityTransform) break;
            }

            if (!quantityTransform && TagDefined("QuantityUI"))
            {
                var tagged = GameObject.FindGameObjectWithTag("QuantityUI");
                if (tagged) quantityTransform = tagged.transform;
            }

            if (quantityTransform)
            {
                var panel = quantityTransform.gameObject;
                promptPanel = panel;
                promptTitle = panel.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                promptSlider = panel.transform.Find("Slider")?.GetComponent<Slider>();
                promptValue = panel.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
                okButton = panel.transform.Find("OK")?.GetComponent<Button>();
                cancelButton = panel.transform.Find("Cancel")?.GetComponent<Button>();
            }
        }

        if (promptPanel) promptPanel.SetActive(false);

        if (promptSlider)
        {
            promptSlider.wholeNumbers = true;
            promptSlider.onValueChanged.RemoveAllListeners();
            promptSlider.onValueChanged.AddListener(v =>
            {
                if (promptValue) promptValue.text = "x" + ((int)v).ToString();
            });
        }

        if (okButton)
        {
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(() => ClosePrompt(true));
        }

        if (cancelButton)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => ClosePrompt(false));
        }
    }


    private System.Action<int> _onConfirm;
    private System.Action _onCancel;

    private void ShowPrompt(string title, int max, System.Action<int> onConfirm, System.Action onCancel)
    {
        if (!promptPanel || !promptSlider) { onCancel?.Invoke(); return; }
        _onConfirm = onConfirm; _onCancel = onCancel;

        if (promptTitle) promptTitle.text = title;
        promptSlider.minValue = 1;
        promptSlider.maxValue = Mathf.Max(1, max);
        promptSlider.value = promptSlider.maxValue;
        if (promptValue) promptValue.text = "x" + ((int)promptSlider.value).ToString();

        promptPanel.SetActive(true);
    }


    private void ClosePrompt(bool confirmed)
    {
        if (!promptPanel || !promptSlider) return;
        int value = (int)promptSlider.value;
        promptPanel.SetActive(false);

        var c = _onConfirm; var x = _onCancel; _onConfirm = null; _onCancel = null;
        if (confirmed) c?.Invoke(value); else x?.Invoke();
    }

    private void FaceCanvas(Transform t, Camera cam)
    {
        if (!cam) return;
        var dir = t.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(dir);
    }

    private IEnumerator FlyToStorageRoutine(Vector3 startWorld, int count)
    {
        int icons = Mathf.Clamp(count, 1, flyBurst);
        RectTransform overlayCanvasRect = overlayCanvas.transform as RectTransform;

        Vector2 startLocal;
        Vector2 endLocal;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(Camera.main, startWorld);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayCanvasRect, startScreen, overlayCanvas.worldCamera, out startLocal);

        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(overlayCanvas ? overlayCanvas.worldCamera : null, storageAnchor.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayCanvasRect, endScreen, overlayCanvas ? overlayCanvas.worldCamera : null, out endLocal);

        Vector2 control = Vector2.Lerp(startLocal, endLocal, 0.5f) + Vector2.up * 80f;

        for (int i = 0; i < icons; i++)
            StartCoroutine(SingleFlyIcon(startLocal, control, endLocal, i * 0.03f));

        yield return new WaitForSeconds(flyDuration + 0.15f);
        StartCoroutine(PulseStorageAnchor());
    }

    private IEnumerator SingleFlyIcon(Vector2 start, Vector2 control, Vector2 end, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        RectTransform overlayCanvasRect = overlayCanvas.transform as RectTransform;
        var iconObj = new GameObject("FlyIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        iconObj.transform.SetParent(overlayCanvasRect, false);

        var iconRc = iconObj.GetComponent<RectTransform>();
        iconRc.sizeDelta = flyIconSize;
        iconRc.anchoredPosition = start;

        var img = iconObj.GetComponent<Image>();
        img.sprite = plankIcon;
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

    private static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private IEnumerator PulseStorageAnchor()
    {
        float up = 0.1f;
        float down = 0.1f;
        Vector3 baseScale = storageAnchor.localScale;
        Vector3 bigScale = baseScale * 1.12f;

        float t = 0f;
        while (t < up)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / up);
            storageAnchor.localScale = Vector3.Lerp(baseScale, bigScale, k);
            yield return null;
        }

        t = 0f;
        while (t < down)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / down);
            storageAnchor.localScale = Vector3.Lerp(bigScale, baseScale, k);
            yield return null;
        }

        storageAnchor.localScale = baseScale;
    }

    private void TryAutoWireStorage()
    {
        if (overlayCanvas == null && autoFindStorageUI)
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!c || !c.gameObject.scene.IsValid()) continue;

                bool tagMatch = false;
                if (!string.IsNullOrEmpty(overlayCanvasTag) && TagDefined(overlayCanvasTag))
                    tagMatch = (c.gameObject.tag == overlayCanvasTag);

                if (tagMatch || c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                { overlayCanvas = c; break; }
            }
        }

        if (overlayCanvas && storageAnchor == null)
        {
            foreach (var rt in overlayCanvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == storageAnchorName) { storageAnchor = rt; break; }
            }
        }

        if (overlayCanvas && storageAnchor == null)
        {
            var go = new GameObject(storageAnchorName, typeof(RectTransform));
            go.transform.SetParent(overlayCanvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-32f, -32f);
            rt.sizeDelta = new Vector2(8f, 8f);
            storageAnchor = rt;
        }
    }

    public void WireStorageUI(Canvas canvas, RectTransform anchor)
    {
        overlayCanvas = canvas;
        storageAnchor = anchor;
    }
}

