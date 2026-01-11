using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SquareCutter : MonoBehaviour, IDropHandler
{
    [Header("Item Setup")]
    public ItemSO plankItem;                          
    public ItemSO defaultSquareItem;                  

    [Header("Rules")]
    [Min(1)] public int maxDimension = 8;             
    [Min(1)] public int planksPerWidthUnit = 1;       
    [Min(0.1f)] public float secondsForTwoByTwo = 3f; 

    [Header("Recipes")]
    public List<Recipe> recipes = new();
    [System.Serializable]
    public struct Recipe
    {
        [Min(1)] public int width;                    
        [Min(1)] public int height;                   
        public ItemSO outputItem;                     
        [Min(0)] public int planksCost;               
        [Min(0f)] public float seconds;               
    }

    [Header("World Effects")]
    public Transform inputPoint;                      
    public Transform outputPoint;                     
    public GameObject inputEffectPrefab;              
    public GameObject outputEffectPrefab;             
    public float effectLifetime = 1.5f;               

    [Header("Drop Zone")]
    [Min(0.2f)] public float dropZoneWorldSize = 1.2f;
    public bool showDropZone = true;                  
    public Vector3 dropZoneLocalOffset = new(0f, 1.6f, 0f);
    public Sprite dropZoneSprite;                     
    public Color dropZoneColor = new(0f, 1f, 0f, 0.25f);

    [Header("Drop Zone Layout")]
    public Vector2 dropZoneUISize = new(300f, 300f); 
    public float dropZoneScaleMultiplier = 1f;

    public Vector2 dropHintAnchorMin = new(0.5f, 0f);
    public Vector2 dropHintAnchorMax = new(0.5f, 0f);
    public Vector2 dropHintPivot = new(0.5f, 0f);

    public Vector2 dropHintOffset = new(0f, 10f);
    public Vector2 dropHintSize = new(280f, 60f);

    public TMP_FontAsset dropHintFont;
    public bool dropHintBold = false;

    public Color dropHintTextColor = Color.black;
    public bool dropHintAutoSize = true;
    public float dropHintFontSize = 28f;
    public int dropHintFontSizeMin = 20;
    public int dropHintFontSizeMax = 32;

    public bool enableBlimp = true;                   // enable blimp
    public float blimpHeight = 1.6f;                  // blimp height
    public Vector2 blimpSize = new(140, 90);          // blimp size
    public Sprite blimpBackgroundSprite;
    public float blimpScale = 1f; // blimp sprite
    public bool showTimer = true;                     
    public Sprite hourglassSprite;                    // timer sprite
    public Vector2 timerSize = new(64, 64);           // timer size
    public float timerSpinSpeed = 180f;               // timer spin
    public bool timerDockToBlimp = true;              // dock timer
    public Vector3 timerLocalOffset = new(0.28f, 0f, 0f); // timer offset

    [Header("Blimp Text")]
    public TMP_FontAsset blimpFont;
    public bool blimpBold = false;
    public Color blimpTextColor = Color.black;

    public bool blimpAutoSize = true;
    public int blimpFontSizeMin = 20;
    public int blimpFontSizeMax = 40;

    public float blimpFontSize = 28f;



    [Header("Fly To Storage")]
    public Canvas overlayCanvas;                 
    public RectTransform storageAnchor;          
    public Sprite outputIcon;                    
    public Vector2 flyIconSize = new(36, 36);    
    public float flyDuration = 0.7f;             
    public int flyBurst = 6;                     
    public AnimationCurve flyCurve;              

    public bool autoFindStorageUI = true;            
    public string overlayCanvasTag = "OverlayCanvas";
    public string storageAnchorName = "StorageAnchor";
    public float uiProbeInterval = 0.5f;              

    [Header("Dimension UI")]
    public GameObject dimensionPanel;               
    public TMP_InputField widthField;               
    public TMP_InputField heightField;               
    public TextMeshProUGUI dimSummary;                
    public Button dimOkButton;                       
    public Button dimCancelButton;                   
    public bool rememberLastDimension = true;        

    [Header("Debug")]
    public bool debugLogs = false;               

    private StorageManager storage;
    private Placeble placeble;
    private Canvas dropCanvas;
    private TextMeshProUGUI dropHint;
    private bool busy;
    private bool waitingForDrop;
    private int selW = 2;
    private int selH = 2;
    private ItemSO currentOutput;
    private int costPlanks;
    private float secondsPerPiece;
    private int pendingCount;
    private MachineBlimp blimp;
    private Transform blimpRoot;
    private TextMeshProUGUI blimpCountText;
    private Canvas timerCanvas;
    private RectTransform timerRect;
    private float _nextProbeTime;
    private ItemSO lastPendingItem;

    private void Awake()
    {
        storage = FindFirstObjectByType<StorageManager>();  
        placeble = GetComponent<Placeble>();                

        EnsureDropZone();                                   
        EnsureBlimp();                                      
        EnsureTimer();                                      
        SetupDimensionUI();                                 

        if (flyCurve == null || flyCurve.keys.Length == 0)  
            flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        AutoWireStorage();                               
        ComputeDimensionParams();                        

        if (dropCanvas) dropCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (dropCanvas)
        {
            bool canShow = waitingForDrop && !busy && showDropZone;
            dropCanvas.gameObject.SetActive(canShow);
            dropCanvas.worldCamera = Camera.main;
            FaceCanvas(dropCanvas.transform, Camera.main);
        }

        if (autoFindStorageUI && Time.unscaledTime >= _nextProbeTime)
        {
            _nextProbeTime = Time.unscaledTime + uiProbeInterval;
            AutoWireStorage();
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

        if (dropHint) dropHint.gameObject.SetActive(waitingForDrop && !busy);
    }

    private void OnMouseDown()
    {
        if (PlayerController.IsInputLocked)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (busy) return;
        if (placeble && !placeble.placed) return;
        OpenDimensionUI();
    }


    public void OnDrop(PointerEventData e)
    {
        if (!waitingForDrop || busy) return;
        if (placeble && !placeble.placed) return;

        var drag = e.pointerDrag ? e.pointerDrag.GetComponent<DraggableItemUI>() : null;
        if (!drag) return;

        var payload = drag.TakePayload();
        if (payload.IsEmpty || payload.item != plankItem)
        {
            drag.ReturnRemainder(payload);
            return;
        }

        int needed = costPlanks;
        if (payload.count < needed)
        {
            drag.ReturnRemainder(payload);
            return;
        }

        payload.count -= needed;
        drag.ReturnRemainder(payload);
        waitingForDrop = false;

        if (debugLogs) Debug.Log($"[SquareCutter] Consumed={needed} Dim={selW}x{selH}");
        StartCoroutine(ProcessPieces(1));
    }

    private IEnumerator ProcessPieces(int pieces)
    {
        busy = true;
        SetTimer(true);

        for (int i = 0; i < pieces; i++)
        {
            if (inputEffectPrefab && inputPoint)
            {
                var fxIn = Instantiate(inputEffectPrefab, inputPoint.position, Quaternion.identity);
                if (effectLifetime > 0) Destroy(fxIn, effectLifetime);
            }

            yield return new WaitForSeconds(secondsPerPiece);
            pendingCount++;
            UpdateBlimpUI();

            if (outputEffectPrefab && outputPoint)
            {
                var fxOut = Instantiate(outputEffectPrefab, outputPoint.position, Quaternion.identity);
                if (effectLifetime > 0) Destroy(fxOut, effectLifetime);
            }
        }

        SetTimer(false);
        busy = false;
        if (dropHint) dropHint.text = $"Drop {costPlanks} planks here";
        if (debugLogs) Debug.Log(" Piece completed.");
    }

    private void ComputeDimensionParams()
    {
        Recipe? match = null;
        foreach (var r in recipes)
        {
            bool same = (r.width == selW && r.height == selH) || (r.width == selH && r.height == selW);
            if (same)
            {
                match = r;
                break;
            }
        }

        currentOutput = match.HasValue && match.Value.outputItem
            ? match.Value.outputItem
            : (defaultSquareItem ? defaultSquareItem : plankItem);

        costPlanks = match.HasValue && match.Value.planksCost > 0
            ? match.Value.planksCost
            : ComputeCostFor(selW);

        secondsPerPiece = match.HasValue && match.Value.seconds > 0f
            ? match.Value.seconds
            : ComputeSecondsFor(selW, selH);

        if (dimSummary)
            dimSummary.text = $"W: {selW} x H: {selH} -> REQUIRES {costPlanks} PLANKS AND {secondsPerPiece:0.#} SECONDS";
        if (dropHint)
            dropHint.text = $"Drop {costPlanks} planks here";

        if (debugLogs)
        {
            string itemName = currentOutput ? currentOutput.displayName : "NULL";
            string iconState = (currentOutput && currentOutput.icon) ? "ICON_OK" : "ICON_MISSING";
        }
    }

    private int ComputeCostFor(int w)
    {
        return Mathf.Max(1, planksPerWidthUnit * Mathf.Max(1, w)); 
    }

    private float ComputeSecondsFor(int w, int h)
    {
        float area = Mathf.Max(1, w * h);                  
        return secondsForTwoByTwo * (area / 4f);           
    }

    private void OpenDimensionUI()
    {
        if (!dimensionPanel)
        {
            SetupDimensionUI();
        }

        if (!dimensionPanel)
        {
            waitingForDrop = true;
            return;
        }

        if (widthField) widthField.text = selW.ToString();
        if (heightField) heightField.text = selH.ToString();
        ComputeDimensionParams();
        dimensionPanel.SetActive(true);
    }


    private void CloseDimensionUI(bool confirmed)
    {
        if (!dimensionPanel) return;

        if (confirmed)
        {
            int w = ParseField(widthField, selW);
            int h = ParseField(heightField, selH);
            selW = Mathf.Clamp(w, 1, maxDimension);
            selH = Mathf.Clamp(h, 1, maxDimension);
            ComputeDimensionParams();

            if (pendingCount > 0 && currentOutput != lastPendingItem)
                CollectBlimp();

            lastPendingItem = currentOutput;
            waitingForDrop = true;
        }
        dimensionPanel.SetActive(false);
    }

    private static int ParseField(TMP_InputField f, int fallback)
    {
        if (!f || string.IsNullOrWhiteSpace(f.text)) return fallback;
        return int.TryParse(f.text, out int v) ? v : fallback;
    }

    private void OnDimChanged()
    {
        int w = ParseField(widthField, selW);
        int h = ParseField(heightField, selH);
        selW = Mathf.Clamp(w, 1, maxDimension);
        selH = Mathf.Clamp(h, 1, maxDimension);
        ComputeDimensionParams();
    }

    private void UpdateBlimpUI()
    {
        if (!enableBlimp) return;
        if (blimp) blimp.gameObject.SetActive(pendingCount > 0);
        if (blimpCountText) blimpCountText.text = $"COLLECT: x{pendingCount}";
    }

    internal void CollectBlimp()
    {
        if (pendingCount <= 0 || !storage)
        {
            pendingCount = 0;
            UpdateBlimpUI();
            return;
        }

        Vector3 startWorld = transform.position + new Vector3(0f, blimpHeight, 0f);
        if (blimp) startWorld = blimp.transform.position;

        int collected = pendingCount;
        storage.Put(currentOutput, collected);
        pendingCount = 0;
        UpdateBlimpUI();

        if (!overlayCanvas || !storageAnchor) AutoWireStorage();
        if (overlayCanvas && storageAnchor && outputIcon)
            StartCoroutine(FlyToStorageRoutine(startWorld, collected));
    }

    private void EnsureDropZone()
    {
        var canvasObj = new GameObject("SquareDropCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = dropZoneLocalOffset;

        dropCanvas = canvasObj.GetComponent<Canvas>();
        dropCanvas.renderMode = RenderMode.WorldSpace;
        dropCanvas.worldCamera = Camera.main;
        dropCanvas.sortingOrder = 2000;

        var rc = dropCanvas.GetComponent<RectTransform>();
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = dropZoneUISize;

        float baseScale = Mathf.Max(0.001f, dropZoneWorldSize / rc.sizeDelta.x);
        float finalScale = baseScale * Mathf.Max(0.001f, dropZoneScaleMultiplier);
        dropCanvas.transform.localScale = new Vector3(finalScale, finalScale, finalScale);

        var zoneObj = new GameObject("DropZone", typeof(RectTransform), typeof(Image));
        zoneObj.transform.SetParent(canvasObj.transform, false);
        var zoneRc = zoneObj.GetComponent<RectTransform>();
        zoneRc.anchorMin = zoneRc.anchorMax = zoneRc.pivot = new Vector2(0.5f, 0.5f);
        zoneRc.sizeDelta = dropZoneUISize;
        zoneRc.localPosition = Vector3.zero;

        var img = zoneObj.GetComponent<Image>();
        img.sprite = dropZoneSprite;
        img.type = Image.Type.Sliced;
        img.color = showDropZone ? dropZoneColor : new Color(1f, 1f, 1f, 0.001f);
        img.raycastTarget = true;

        var proxy = zoneObj.AddComponent<SquareDropProxy>();
        proxy.square = this;

        var hintObj = new GameObject("DropHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(zoneObj.transform, false);
        var hintRc = hintObj.GetComponent<RectTransform>();
        hintRc.anchorMin = dropHintAnchorMin;
        hintRc.anchorMax = dropHintAnchorMax;
        hintRc.pivot = dropHintPivot;
        hintRc.anchoredPosition = dropHintOffset;
        hintRc.sizeDelta = dropHintSize;

        dropHint = hintObj.GetComponent<TextMeshProUGUI>();
        dropHint.alignment = TextAlignmentOptions.Center;

        dropHint.fontStyle = dropHintBold ? FontStyles.Bold : FontStyles.Normal;


        if (dropHintFont != null)
            dropHint.font = dropHintFont;

        dropHint.color = dropHintTextColor;
        dropHint.enableAutoSizing = dropHintAutoSize;

        if (dropHintAutoSize)
        {
            dropHint.fontSizeMin = dropHintFontSizeMin;
            dropHint.fontSizeMax = dropHintFontSizeMax;
        }
        else
        {
            dropHint.fontSize = dropHintFontSize;
        }

        dropHint.text = "Drop planks here";
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
        c.transform.localScale = Vector3.one * blimpScale;

        var btnObj = new GameObject("Blimp", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvasObj.transform, false);
        var btnRc = btnObj.GetComponent<RectTransform>();
        btnRc.anchorMin = btnRc.anchorMax = btnRc.pivot = new Vector2(0.5f, 0.5f);
        btnRc.sizeDelta = blimpSize;

        var bg = btnObj.GetComponent<Image>();
        if (blimpBackgroundSprite)
        {
            bg.sprite = blimpBackgroundSprite;
            bg.type = Image.Type.Sliced;
        }
        else bg.color = new Color(0f, 0f, 0f, 0.55f);

        var textObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var textRc = textObj.GetComponent<RectTransform>();
        textRc.anchorMin = textRc.anchorMax = new Vector2(0.5f, 0f);
        textRc.pivot = new Vector2(0.5f, 0f);
        textRc.anchoredPosition = new Vector2(0f, 6f);
        textRc.sizeDelta = new Vector2(blimpSize.x, 28f);

        var countText = textObj.GetComponent<TextMeshProUGUI>();
        countText.text = "COLLECT: x0";
        countText.alignment = TextAlignmentOptions.Center;

        if (blimpFont != null)
            countText.font = blimpFont;

        countText.fontStyle = blimpBold ? FontStyles.Bold : FontStyles.Normal;

        countText.color = blimpTextColor;

        countText.enableAutoSizing = blimpAutoSize;

        if (blimpAutoSize)
        {
            countText.fontSizeMin = blimpFontSizeMin;
            countText.fontSizeMax = blimpFontSizeMax;
        }
        else
        {
            countText.fontSize = blimpFontSize;
        }

        blimp = canvasObj.AddComponent<MachineBlimp>();
        blimp.InitForSquare(this, btnObj.GetComponent<Button>());

        blimpCountText = countText;
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

        var timerImage = imgObj.GetComponent<Image>();
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

    private IEnumerator FlyToStorageRoutine(Vector3 startWorld, int count)
    {
        int icons = Mathf.Clamp(count, 1, flyBurst);
        RectTransform overlayCanvasRect = overlayCanvas.transform as RectTransform;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(Camera.main, startWorld);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvasRect,
            startScreen,
            overlayCanvas.worldCamera,
            out var startLocal
        );

        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(
            overlayCanvas ? overlayCanvas.worldCamera : null,
            storageAnchor.position
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvasRect,
            endScreen,
            overlayCanvas ? overlayCanvas.worldCamera : null,
            out var endLocal
        );

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
        img.sprite = outputIcon;
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

    private void AutoWireStorage()
    {
        if (overlayCanvas == null && autoFindStorageUI)
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!c || !c.gameObject.scene.IsValid()) continue;
                if (!string.IsNullOrEmpty(overlayCanvasTag) && c.CompareTag(overlayCanvasTag))
                {
                    overlayCanvas = c;
                    break;
                }
            }
            if (overlayCanvas == null)
            {
                foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
                {
                    if (!c || !c.gameObject.scene.IsValid()) continue;
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        overlayCanvas = c;
                        break;
                    }
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

    private void FaceCanvas(Transform t, Camera cam)
    {
        if (!cam) return;
        var dir = t.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(dir);
    }

    private void SetupDimensionUI()
    {
        if (!dimensionPanel)
        {
            RectTransform found = null;
            var rects = Resources.FindObjectsOfTypeAll<RectTransform>();
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                if (!r) continue;
                var go = r.gameObject;
                if (!go.scene.IsValid()) continue;
                if (go.name == "DimensionUI")
                {
                    found = r;
                    break;
                }
            }

            if (found) dimensionPanel = found.gameObject;
        }

        if (!dimensionPanel) return;

        if (!widthField)
        {
            var wTransform = dimensionPanel.transform.Find("Row_LeftCol/ROWWH/W");
            if (wTransform)
            {
                widthField = wTransform.GetComponentInChildren<TMP_InputField>(true);
            }
        }

        if (!heightField)
        {
            var hTransform = dimensionPanel.transform.Find("Row_LeftCol/ROWWH/H");
            if (hTransform)
            {
                heightField = hTransform.GetComponentInChildren<TMP_InputField>(true);
            }
        }

        if (!dimSummary)
        {
            var sTransform = dimensionPanel.transform.Find("Row_RightCol/Summary");
            if (sTransform)
            {
                dimSummary = sTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (!dimCancelButton)
        {
            var cTransform = dimensionPanel.transform.Find("Row_Buttons/Cancel");
            if (cTransform)
            {
                dimCancelButton = cTransform.GetComponent<Button>();
            }
        }

        if (!dimOkButton)
        {
            var oTransform = dimensionPanel.transform.Find("Row_Buttons/OK");
            if (oTransform)
            {
                dimOkButton = oTransform.GetComponent<Button>();
            }
        }

        dimensionPanel.SetActive(false);

        if (widthField)
        {
            widthField.contentType = TMP_InputField.ContentType.IntegerNumber;
            widthField.onValueChanged.RemoveAllListeners();
            widthField.onValueChanged.AddListener(_ => OnDimChanged());
        }

        if (heightField)
        {
            heightField.contentType = TMP_InputField.ContentType.IntegerNumber;
            heightField.onValueChanged.RemoveAllListeners();
            heightField.onValueChanged.AddListener(_ => OnDimChanged());
        }

        if (dimOkButton)
        {
            dimOkButton.onClick.RemoveAllListeners();
            dimOkButton.onClick.AddListener(() => CloseDimensionUI(true));
        }

        if (dimCancelButton)
        {
            dimCancelButton.onClick.RemoveAllListeners();
            dimCancelButton.onClick.AddListener(() => CloseDimensionUI(false));
        }
    }
}
