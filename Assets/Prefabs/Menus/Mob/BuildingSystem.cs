using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem instance;

    [Header("Grid + Tilemap")]
    public GridLayout gridLayout;
    private Grid grid;
    [SerializeField] private Tilemap MainTilemap;
    [SerializeField] private TileBase OccupiedTile;

    [Header("Prefabs")]
    public GameObject housePrefab;

    [Header("Mobile Settings")]
    [Tooltip("Rotation amount in degrees per tap on rotate button or gesture")]
    public float rotationStep = 90f;
    [Tooltip("Minimum rotation gesture angle to trigger rotation")]
    public float rotationGestureThreshold = 15f;

    private Placeble objectToPlace;
    private int groundLayer;

    // Mobile touch tracking
    private Vector2 lastTouchPosition;
    private bool isDragging = false;
    private float initialTwoFingerAngle;
    private bool isRotating = false;

    void Awake()
    {
        instance = this;

        if (gridLayout == null)
            gridLayout = GetComponentInChildren<GridLayout>();

        if (gridLayout != null)
            grid = gridLayout.gameObject.GetComponent<Grid>();

        groundLayer = LayerMask.NameToLayer("Ground");
    }

    void Update()
    {
        if (objectToPlace == null) return;

        // Handle input based on platform
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }

    void HandleTouchInput()
    {
        // Two-finger gestures: rotation or cancel
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Check for two-finger tap to cancel (both fingers just began)
            if (touch0.phase == TouchPhase.Began && touch1.phase == TouchPhase.Began)
            {
                // Cancel placement
                CancelPlacement();
                return;
            }

            // Two-finger rotation gesture
            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                // Start rotation tracking
                initialTwoFingerAngle = GetTwoFingerAngle(touch0.position, touch1.position);
                isRotating = true;
            }
            else if (isRotating && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
            {
                float currentAngle = GetTwoFingerAngle(touch0.position, touch1.position);
                float angleDelta = Mathf.DeltaAngle(initialTwoFingerAngle, currentAngle);

                if (Mathf.Abs(angleDelta) >= rotationGestureThreshold)
                {
                    // Rotate the object (always uses Rotate() - rotates 90 degrees each trigger)
                    objectToPlace.Rotate();
                    initialTwoFingerAngle = currentAngle;
                }
            }
            else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
            {
                isRotating = false;
            }

            return; // Don't process single-finger input during two-finger gesture
        }

        // Single finger: drag to position, tap to place
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            isRotating = false;

            // Skip if touching UI
            if (IsPointerOverUI(touch.position)) return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = touch.position;
                    isDragging = false;
                    break;

                case TouchPhase.Moved:
                    // Update object position based on touch
                    isDragging = true;
                    Vector3 worldPos = GetWorldPositionFromScreen(touch.position);
                    Vector3 snappedPos = SnapCoordinateToGrid(worldPos);
                    objectToPlace.transform.position = snappedPos;
                    break;

                case TouchPhase.Stationary:
                    // Keep updating position even when stationary
                    Vector3 stationaryWorldPos = GetWorldPositionFromScreen(touch.position);
                    Vector3 stationarySnappedPos = SnapCoordinateToGrid(stationaryWorldPos);
                    objectToPlace.transform.position = stationarySnappedPos;
                    break;

                case TouchPhase.Ended:
                    // If it was a quick tap (not a drag), try to place
                    float dragDistance = Vector2.Distance(touch.position, lastTouchPosition);

                    // Always try to place on touch end if object is in valid position
                    if (dragDistance < 20f) // Small threshold for "tap" vs "drag"
                    {
                        TryPlaceObject();
                    }
                    else
                    {
                        // For drag-and-release, also try to place
                        TryPlaceObject();
                    }
                    isDragging = false;
                    break;
            }
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            objectToPlace.Rotate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 snappedPos = SnapCoordinateToGrid(mousePos);
        objectToPlace.transform.position = snappedPos;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TryPlaceObject();
        }
    }

    void TryPlaceObject()
    {
        if (objectToPlace == null) return;

        if (CanBePlaced(objectToPlace))
        {
            Vector3Int start = gridLayout.WorldToCell(objectToPlace.GetStartPosition());
            TakeArea(start, objectToPlace.Size);
            objectToPlace.Place();
            SavePlacedObject(objectToPlace);
            objectToPlace = null;
        }
    }

    void CancelPlacement()
    {
        if (objectToPlace != null)
        {
            Destroy(objectToPlace.gameObject);
            objectToPlace = null;
        }
    }

    float GetTwoFingerAngle(Vector2 pos1, Vector2 pos2)
    {
        Vector2 direction = pos2 - pos1;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        int groundMask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            return hit.point;

        // Fallback: intersect with Y=0 plane
        float t = -ray.origin.y / ray.direction.y;
        return ray.origin + ray.direction * t;
    }

    bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    // UI Button callback for rotation (can be called from a UI button)
    public void OnRotateButtonPressed()
    {
        if (objectToPlace != null)
            objectToPlace.Rotate();
    }

    // UI Button callback for cancel (can be called from a UI button)
    public void OnCancelButtonPressed()
    {
        CancelPlacement();
    }

    // UI Button callback for confirm placement (can be called from a UI button)
    public void OnConfirmPlacementPressed()
    {
        TryPlaceObject();
    }

    public void StartPlacement(ShopItemSO item)
    {
        if (item == null) return;
        if (item.prefabToPlace == null) return;
        if (string.IsNullOrEmpty(item.id))
        {
            Debug.LogError("ShopItemSO id is empty on " + item.name);
            return;
        }

        // Cancel any existing placement
        CancelPlacement();

        Vector3 spawnPos;

        // Use touch position if available, otherwise mouse
        if (Input.touchCount > 0)
        {
            spawnPos = GetWorldPositionFromScreen(Input.GetTouch(0).position);
        }
        else
        {
            spawnPos = GetMouseWorldPosition();
        }

        Vector3 snappedPos = SnapCoordinateToGrid(spawnPos);

        GameObject obj = Instantiate(item.prefabToPlace, snappedPos, Quaternion.identity);
        objectToPlace = obj.GetComponentInChildren<Placeble>();

        if (objectToPlace == null)
        {
            Debug.LogError("No Placeble component found on prefab " + item.prefabToPlace.name);
            Destroy(obj);
            return;
        }

        objectToPlace.prefabId = item.id;
    }

    public bool IsPlacing()
    {
        return objectToPlace != null;
    }

    public static Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int groundMask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            return hit.point;

        float t = -ray.origin.y / ray.direction.y;
        return ray.origin + ray.direction * t;
    }

    public Vector3 SnapCoordinateToGrid(Vector3 position)
    {
        if (gridLayout == null || grid == null)
            return position;

        Vector3Int cellPos = gridLayout.WorldToCell(position);
        Vector3 center = grid.GetCellCenterWorld(cellPos);
        center.y = 0f;

        if (objectToPlace != null)
        {
            float bottomLocalY = 0f;

            var col = objectToPlace.GetComponent<BoxCollider>();
            if (col)
                bottomLocalY = col.center.y - col.size.y * 0.5f;
            else
            {
                var r = objectToPlace.GetComponentInChildren<Renderer>();
                if (r) bottomLocalY = -r.bounds.extents.y;
            }

            center.y -= bottomLocalY;
        }

        return center;
    }

    public bool CanBePlaced(Placeble placeble)
    {
        if (gridLayout == null || MainTilemap == null || OccupiedTile == null) return false;

        Vector3Int start = gridLayout.WorldToCell(placeble.GetStartPosition());
        BoundsInt area = new BoundsInt(start, placeble.Size);

        if (!IsOverGround(placeble))
        {
            Debug.Log("Cannot place — not over ground!");
            return false;
        }

        TileBase[] baseArray = GetTilesBlock(area, MainTilemap);
        foreach (var b in baseArray)
        {
            if (b == OccupiedTile)
                return false;
        }

        return true;
    }

    static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
    {
        TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
        int counter = 0;
        foreach (var v in area.allPositionsWithin)
        {
            Vector3Int pos = new Vector3Int(v.x, v.y, 0);
            array[counter] = tilemap.GetTile(pos);
            counter++;
        }
        return array;
    }

    bool IsOverGround(Placeble placeble)
    {
        Vector3 origin = placeble.transform.position + Vector3.up * 0.2f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f))
            return hit.collider.gameObject.layer == groundLayer;

        return false;
    }

    public void TakeArea(Vector3Int start, Vector3Int size)
    {
        if (MainTilemap == null || OccupiedTile == null) return;

        MainTilemap.BoxFill(
            start,
            OccupiedTile,
            start.x, start.y,
            start.x + size.x - 1,
            start.y + size.y - 1
        );
    }

    void SavePlacedObject(Placeble placeble)
    {
        string id = placeble.prefabId;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("prefabId is empty, cannot save!");
            return;
        }

        PlayerPrefs.SetFloat("MachinePosX_" + id, placeble.transform.position.x);
        PlayerPrefs.SetFloat("MachinePosY_" + id, placeble.transform.position.y);
        PlayerPrefs.SetFloat("MachinePosZ_" + id, placeble.transform.position.z);
        PlayerPrefs.SetFloat("MachineRotY_" + id, placeble.transform.eulerAngles.y);
        PlayerPrefs.SetInt("MachineOwned_" + id, 1);
        PlayerPrefs.Save();

        Debug.Log("Saved placement for " + id);
    }
}