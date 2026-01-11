using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProductionMachine : MonoBehaviour
{
    [Header("UI Panel")]
    public ProductionMachineUI ui;
    public List<ProductionRecipeSO> recipes = new List<ProductionRecipeSO>();

    public Transform inputPoint;
    public Transform outputPoint;
    public GameObject inputEffectPrefab;
    public GameObject outputEffectPrefab;
    public float effectLifetime = 1.5f;

    [Min(0.1f)] public float secondsPerProduct = 4f;

    [Header("Rules")]
    [Min(0)] public int misfitScrapThreshold = 3;

    [Header("Blimp")]
    public bool enableBlimp = true;
    public float blimpHeight = 1.6f;
    public Vector2 blimpSize = new Vector2(140f, 90f);
    public Sprite blimpBackgroundSprite;

    [Header("Timer")]
    public bool showTimer = true;
    public Sprite hourglassSprite;
    public Vector2 timerSize = new Vector2(64f, 64f);
    public float timerSpinSpeed = 180f;
    public Vector3 timerOffset = new Vector3(0.3f, 0f, 0f);

    public Canvas overlayCanvas;
    public RectTransform storageAnchor;
    public Vector2 flyIconSize = new Vector2(36f, 36f);
    public float flyDuration = 0.7f;
    public int flyBurst = 4;
    public AnimationCurve flyCurve;
    public bool autoFindStorageUI = true;
    public string overlayCanvasTag = "OverlayCanvas";
    public string storageAnchorName = "StorageAnchor";
    public float uiProbeInterval = 0.5f;

    public bool spawnWorldOutputPrefab = true;
    public float worldPrefabLifetime = 2.5f;

    StorageManager storage;
    Placeble placeble;
    Camera mainCam;
    JobManager jobManager;

    ProductionBlimp blimp;
    Transform blimpRoot;
    TextMeshProUGUI blimpCountText;

    Canvas timerCanvas;
    RectTransform timerRect;

    int pendingCount;
    bool busy;
    ItemSO currentOutput;
    bool currentHasMisfits;
    float nextProbeTime;

    void Awake()
    {
        storage = FindFirstObjectByType<StorageManager>();
        placeble = GetComponent<Placeble>();
        mainCam = Camera.main;
        jobManager = FindFirstObjectByType<JobManager>();

        if (flyCurve == null || flyCurve.length == 0)
            flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        EnsureBlimp();
        EnsureTimer();
        AutoWireStorage();
        AutoWireUI();
    }


    void Update()
    {
        if (!mainCam) mainCam = Camera.main;

        if (autoFindStorageUI && Time.unscaledTime >= nextProbeTime)
        {
            nextProbeTime = Time.unscaledTime + uiProbeInterval;
            AutoWireStorage();
        }

        if (blimp)
            blimp.FaceCamera(mainCam);

        if (timerCanvas && timerCanvas.gameObject.activeSelf)
        {
            FaceCanvas(timerCanvas.transform, mainCam);
            if (timerRect)
                timerRect.Rotate(0f, 0f, -timerSpinSpeed * Time.deltaTime);
        }

        if (timerCanvas && blimpRoot)
            timerCanvas.transform.position = blimpRoot.position + timerOffset;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleTapOrClick(touch.position);
            }
        }
    }

    void OnMouseDown()
    {
        if (PlayerController.IsInputLocked)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (busy) return;
        if (placeble && !placeble.placed) return;
        if (!ui) AutoWireUI();
        if (!ui) return;

        IList<ProductionRecipeSO> source = recipes;

        if (RecipeUnlockManager.Instance != null)
            source = RecipeUnlockManager.Instance.FilterUnlocked(recipes);

        int recipeCount = 0;
        if (source != null)
        {
            foreach (var r in source)
            {
                if (r != null)
                    recipeCount++;
            }
        }

        ui.gameObject.SetActive(true);
        ui.Init(this, source);
        PlayerController.IsInputLocked = true;

    }

    void HandleTapOrClick(Vector2 screenPosition)
    {
        if (PlayerController.IsInputLocked) return;

        // Check if tapping on UI
        if (EventSystem.current != null && IsPointerOverUI(screenPosition)) return;

        // Raycast to check if this object was tapped
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                TryOpenUI();
            }
        }
    }

    bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    void TryOpenUI()
    {
        if (busy) return;
        if (placeble && !placeble.placed) return;
        if (!ui) AutoWireUI();
        if (!ui) return;

        IList<ProductionRecipeSO> source = recipes;

        if (RecipeUnlockManager.Instance != null)
            source = RecipeUnlockManager.Instance.FilterUnlocked(recipes);

        int recipeCount = 0;
        if (source != null)
        {
            foreach (var r in source)
            {
                if (r != null)
                    recipeCount++;
            }
        }

        ui.gameObject.SetActive(true);
        ui.Init(this, source);
        PlayerController.IsInputLocked = true;
    }



    public void OnAssemble(ProductionRecipeSO recipe, int errors)
    {
        if (recipe == null) return;
        if (busy)
        {
            return;
        }

        currentOutput = recipe.finishedProduct;
        if (!currentOutput)
        {
            return;
        }

        currentHasMisfits = errors > 0;

        StartCoroutine(ProcessOneProduct(secondsPerProduct));
    }

    IEnumerator ProcessOneProduct(float seconds)
    {
        busy = true;
        SetTimer(true);

        if (inputEffectPrefab && inputPoint)
        {
            GameObject fxIn = Instantiate(inputEffectPrefab, inputPoint.position, Quaternion.identity);
            if (effectLifetime > 0f) Destroy(fxIn, effectLifetime);
        }

        yield return new WaitForSeconds(seconds);

        if (outputPoint)
        {
            if (outputEffectPrefab)
            {
                GameObject fxOut = Instantiate(outputEffectPrefab, outputPoint.position, Quaternion.identity);
                if (effectLifetime > 0f) Destroy(fxOut, effectLifetime);
            }

            if (spawnWorldOutputPrefab && currentOutput != null && currentOutput.worldPrefab != null)
            {
                GameObject world = Instantiate(currentOutput.worldPrefab, outputPoint.position, outputPoint.rotation);
                if (worldPrefabLifetime > 0f)
                    Destroy(world, worldPrefabLifetime);
            }
        }

        pendingCount += 1;
        UpdateBlimpUI();

        if (jobManager != null && currentOutput != null)
            jobManager.ReportProductBuilt(currentOutput, currentHasMisfits);


        SetTimer(false);
        busy = false;
        currentHasMisfits = false;
    }
    void UpdateBlimpUI()
    {
        if (!enableBlimp) return;

        if (blimpRoot)
            blimpRoot.gameObject.SetActive(pendingCount > 0);

        if (blimpCountText)
            blimpCountText.text = "x" + pendingCount.ToString();
    }

    internal void CollectBlimp()
    {
        if (pendingCount <= 0) return;
        if (!storage || !currentOutput)
        {
            pendingCount = 0;
            UpdateBlimpUI();
            return;
        }

        Vector3 startWorld = transform.position + new Vector3(0f, blimpHeight, 0f);
        if (blimpRoot)
            startWorld = blimpRoot.position;

        int collected = pendingCount;
        pendingCount = 0;
        UpdateBlimpUI();

        storage.Put(currentOutput, collected);

        if (overlayCanvas && storageAnchor && currentOutput.icon)
            StartCoroutine(FlyToStorageRoutine(startWorld, collected, currentOutput.icon));
    }

    void EnsureBlimp()
    {
        if (!enableBlimp) return;

        GameObject canvasObj = new GameObject("ProductionBlimpCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, blimpHeight, 0f);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCam;
        canvas.sortingOrder = 2100;

        RectTransform rc = canvas.GetComponent<RectTransform>();
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = blimpSize;

        float scale = 0.6f / Mathf.Max(1f, blimpSize.x);
        canvas.transform.localScale = new Vector3(scale, scale, scale);

        GameObject btnObj = new GameObject("ProductionBlimp", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRc = btnObj.GetComponent<RectTransform>();
        btnRc.anchorMin = btnRc.anchorMax = btnRc.pivot = new Vector2(0.5f, 0.5f);
        btnRc.sizeDelta = blimpSize;

        Image bg = btnObj.GetComponent<Image>();
        if (blimpBackgroundSprite)
        {
            bg.sprite = blimpBackgroundSprite;
            bg.type = Image.Type.Sliced;
        }
        else
        {
            bg.color = new Color(0f, 0f, 0f, 0.55f);
        }

        GameObject textObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRc = textObj.GetComponent<RectTransform>();
        textRc.anchorMin = textRc.anchorMax = new Vector2(0.5f, 0f);
        textRc.pivot = new Vector2(0.5f, 0f);
        textRc.anchoredPosition = new Vector2(0f, 6f);
        textRc.sizeDelta = new Vector2(blimpSize.x, 28f);

        TextMeshProUGUI countText = textObj.GetComponent<TextMeshProUGUI>();
        countText.text = "x0";
        countText.alignment = TextAlignmentOptions.Center;
        countText.enableAutoSizing = true;
        countText.fontSizeMin = 26;
        countText.fontSizeMax = 36;
        countText.color = Color.black;

        Button bgButton = btnObj.GetComponent<Button>();
        ProductionBlimp bl = canvasObj.AddComponent<ProductionBlimp>();
        bl.Init(this, countText, bgButton);

        blimp = bl;
        blimpRoot = canvasObj.transform;
        blimpCountText = countText;

        canvasObj.SetActive(false);
    }

    void EnsureTimer()
    {
        if (!showTimer) return;

        GameObject obj = new GameObject("ProductionTimerCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, blimpHeight, 0f) + timerOffset;

        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCam;
        canvas.sortingOrder = 2110;

        RectTransform rc = canvas.GetComponent<RectTransform>();
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
        rc.sizeDelta = timerSize;
        float scale = 0.4f / Mathf.Max(1f, timerSize.x);
        canvas.transform.localScale = new Vector3(scale, scale, scale);

        GameObject imgObj = new GameObject("Hourglass", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(obj.transform, false);
        timerRect = imgObj.GetComponent<RectTransform>();
        timerRect.anchorMin = timerRect.anchorMax = timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.sizeDelta = timerSize;

        Image img = imgObj.GetComponent<Image>();
        img.sprite = hourglassSprite;
        img.color = new Color(1f, 1f, 1f, 0.9f);

        timerCanvas = canvas;
        obj.SetActive(false);
    }

    void SetTimer(bool on)
    {
        if (!timerCanvas) return;
        timerCanvas.gameObject.SetActive(on);
        if (timerRect)
            timerRect.localRotation = Quaternion.identity;
    }
    void AutoWireUI()
    {
        if (ui) return;

        ProductionMachineUI[] panels = Resources.FindObjectsOfTypeAll<ProductionMachineUI>();
        for (int i = 0; i < panels.Length; i++)
        {
            var panel = panels[i];
            if (!panel) continue;
            GameObject go = panel.gameObject;
            if (!go.scene.IsValid()) continue;
            ui = panel;
            break;
        }
    }


    void AutoWireStorage()
    {
        if (!overlayCanvas && autoFindStorageUI)
        {
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (!c || !c.gameObject.scene.IsValid()) continue;
                if (!string.IsNullOrEmpty(overlayCanvasTag) && c.CompareTag(overlayCanvasTag))
                {
                    overlayCanvas = c;
                    break;
                }
            }

            if (!overlayCanvas)
            {
                Canvas[] canvasesAll = Resources.FindObjectsOfTypeAll<Canvas>();
                for (int i = 0; i < canvasesAll.Length; i++)
                {
                    Canvas c = canvasesAll[i];
                    if (!c || !c.gameObject.scene.IsValid()) continue;
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        overlayCanvas = c;
                        break;
                    }
                }
            }
        }

        if (overlayCanvas && !storageAnchor)
        {
            RectTransform[] rects = overlayCanvas.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].name == storageAnchorName)
                {
                    storageAnchor = rects[i];
                    break;
                }
            }
        }

        if (overlayCanvas && !storageAnchor)
        {
            GameObject go = new GameObject(storageAnchorName, typeof(RectTransform));
            go.transform.SetParent(overlayCanvas.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-32f, -32f);
            rt.sizeDelta = new Vector2(8f, 8f);
            storageAnchor = rt;
        }
    }

    IEnumerator FlyToStorageRoutine(Vector3 startWorld, int count, Sprite icon)
    {
        int icons = Mathf.Clamp(count, 1, flyBurst);
        RectTransform canvasRect = overlayCanvas.transform as RectTransform;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(mainCam, startWorld);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            startScreen,
            overlayCanvas.worldCamera,
            out Vector2 startLocal
        );

        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(
            overlayCanvas ? overlayCanvas.worldCamera : null,
            storageAnchor.position
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            endScreen,
            overlayCanvas ? overlayCanvas.worldCamera : null,
            out Vector2 endLocal
        );

        Vector2 control = Vector2.Lerp(startLocal, endLocal, 0.5f) + Vector2.up * 80f;

        for (int i = 0; i < icons; i++)
        {
            float delay = i * 0.03f;
            StartCoroutine(SingleFlyIcon(startLocal, control, endLocal, icon, delay));
        }

        yield return new WaitForSeconds(flyDuration + 0.15f);
        StartCoroutine(PulseStorageAnchor());
    }

    IEnumerator SingleFlyIcon(Vector2 start, Vector2 control, Vector2 end, Sprite icon, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RectTransform canvasRect = overlayCanvas.transform as RectTransform;

        GameObject iconObj = new GameObject("ProductFlyIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        iconObj.transform.SetParent(canvasRect, false);

        RectTransform rc = iconObj.GetComponent<RectTransform>();
        rc.sizeDelta = flyIconSize;
        rc.anchoredPosition = start;

        Image img = iconObj.GetComponent<Image>();
        img.sprite = icon;
        img.raycastTarget = false;

        CanvasGroup grp = iconObj.GetComponent<CanvasGroup>();
        grp.alpha = 1f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, flyDuration);
            float k = Mathf.Clamp01(t);
            float e = flyCurve != null && flyCurve.length > 0 ? flyCurve.Evaluate(k) : k;

            Vector2 p = QuadraticBezier(start, control, end, e);
            rc.anchoredPosition = p;

            float s = Mathf.Lerp(1.0f, 0.65f, e);
            rc.localScale = new Vector3(s, s, 1f);
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

    IEnumerator PulseStorageAnchor()
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

    void FaceCanvas(Transform t, Camera cam)
    {
        if (!cam) return;
        Vector3 dir = t.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(dir);
    }
}
