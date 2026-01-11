using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class isometricCamera : MonoBehaviour
{
    [Header("Pan settings")]
    public float panSpeed = 0.5f;

    [Header("Zoom settings")]
    public float zoomSpeed = 0.1f;
    public float zoomSmoothness = 5f;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float rotationSpeed = 100f;

    [Header("Room Settings")]
    [Tooltip("Assign all  room objects here")]
    public GameObject[] rooms;

    public int currentRoomIndex = 0;

    [Header("Manual Bounds (if not using room objects)")]
    public bool useManualBounds = false;
    public Vector2 manualRoomSize = new Vector2(20f, 20f);
    public Vector3[] roomCenters;

    [Header("Transition Settings")]
    public bool smoothTransition = true;
    public float transitionSpeed = 5f;

    public GameObject mapObject;
    public float mapWidth;
    public float mapHeight;

    public static isometricCamera Instance { get; private set; }

    private Camera _camera;
    private Vector2 _lastPanPosition;
    private int _panFingerId;
    private bool _isPanning;
    private float _currentZoom;
    private Vector3 mapCenter;
    private Vector3 targetPosition;
    private bool isTransitioning = false;

    private void Awake()
    {
        Instance = this;

        _camera = GetComponentInChildren<Camera>();
        _currentZoom = _camera.orthographicSize;

        // Set initial room
        if (rooms != null && rooms.Length > 0 && currentRoomIndex < rooms.Length)
        {
            mapObject = rooms[currentRoomIndex];
        }

        if (mapObject != null) CalculateMapSize();
        RecalculateMaxZoom();
    }

    private void CalculateMapSize()
    {
        if (useManualBounds)
        {
            // Use manual bounds
            mapWidth = manualRoomSize.x;
            mapHeight = manualRoomSize.y;

            if (roomCenters != null && currentRoomIndex < roomCenters.Length)
            {
                mapCenter = roomCenters[currentRoomIndex];
            }
            else if (mapObject != null)
            {
                mapCenter = mapObject.transform.position;
            }
            return;
        }

        if (mapObject == null) return;

        MeshRenderer meshRenderer = mapObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            mapWidth = meshRenderer.bounds.size.x;
            mapHeight = meshRenderer.bounds.size.z;
            mapCenter = meshRenderer.bounds.center;
            return;
        }

        Collider col = mapObject.GetComponent<Collider>();
        if (col != null)
        {
            mapWidth = col.bounds.size.x;
            mapHeight = col.bounds.size.z;
            mapCenter = col.bounds.center;
            return;
        }

        Renderer[] renderers = mapObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            mapWidth = combinedBounds.size.x;
            mapHeight = combinedBounds.size.z;
            mapCenter = combinedBounds.center;
            return;
        }

        mapCenter = mapObject.transform.position;
        if (mapWidth <= 0) mapWidth = manualRoomSize.x;
        if (mapHeight <= 0) mapHeight = manualRoomSize.y;
    }

    private void RecalculateMaxZoom()
    {
        float maxByHeight = mapHeight / 2f;
        float maxByWidth = (mapWidth / 2f) / _camera.aspect;
        maxZoom = Mathf.Min(maxByHeight, maxByWidth);
    }

    void Start()
    {
    }

    void Update()
    {
        if (isTransitioning && smoothTransition)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * transitionSpeed);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                transform.position = targetPosition;
                isTransitioning = false;
            }

            ApplyZoom();
            return; // Don't allow input during transition
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleEditorInput();
#else
        HandleTouchInput();
#endif
        ApplyZoom();
        ClampPosition();
    }

    private void HandleEditorInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Use camera-relative movement for keyboard input too
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (right * h + forward * v) * panSpeed;
        transform.Translate(move, Space.World);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            _currentZoom -= scroll * zoomSpeed * 100f * Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            float mouseDeltaX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, mouseDeltaX * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Don't start panning if touching UI
                if (IsPointerOverUI(touch.position))
                {
                    _isPanning = false;
                    return;
                }

                _lastPanPosition = touch.position;
                _panFingerId = touch.fingerId;
                _isPanning = true;
            }
            else if (touch.fingerId == _panFingerId &&
                     touch.phase == TouchPhase.Moved &&
                     _isPanning)
            {
                Vector2 touchDelta = touch.position - _lastPanPosition;

                // Convert screen delta to world movement based on camera rotation
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                float zoomFactor = _camera.orthographicSize / 10f;

                Vector3 move = (-right * touchDelta.x - forward * touchDelta.y)
                               * panSpeed * zoomFactor * Time.deltaTime;

                transform.Translate(move, Space.World);
                _lastPanPosition = touch.position;
            }
            else if (touch.fingerId == _panFingerId &&
                     (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                _isPanning = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            _isPanning = false;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (IsPointerOverUI(t0.position) || IsPointerOverUI(t1.position))
                return;

            float prevDist = (t0.position - t0.deltaPosition -
                              (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = prevDist - currDist;

            _currentZoom += delta * zoomSpeed * Time.deltaTime;

            Vector2 prevDir = (t0.position - t0.deltaPosition) -
                              (t1.position - t1.deltaPosition);
            Vector2 currDir = t0.position - t1.position;
            float angle = Vector2.SignedAngle(prevDir, currDir);
            transform.Rotate(Vector3.up, angle * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    public void SetMapObject(GameObject newMap)
    {
        mapObject = newMap;
        if (mapObject != null)
        {
            CalculateMapSize();
            RecalculateMaxZoom();
            ClampPosition();
        }
    }
    public void SwitchToRoom(int roomIndex)
    {
        if (rooms == null || rooms.Length == 0)
        {
            Debug.LogWarning("No rooms assigned to isometricCamera!");
            return;
        }

        if (roomIndex < 0 || roomIndex >= rooms.Length)
        {
            Debug.LogWarning($"Room index {roomIndex} out of range! Valid range: 0-{rooms.Length - 1}");
            return;
        }

        currentRoomIndex = roomIndex;
        mapObject = rooms[roomIndex];

        CalculateMapSize();
        RecalculateMaxZoom();

        _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);

        if (smoothTransition)
        {
            targetPosition = new Vector3(mapCenter.x, transform.position.y, mapCenter.z);
            isTransitioning = true;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.x = mapCenter.x;
            pos.z = mapCenter.z;
            transform.position = pos;
            ClampPosition();
        }
    }
    public void SwitchToRoom(string roomName)
    {
        if (rooms == null) return;

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null && rooms[i].name == roomName)
            {
                SwitchToRoom(i);
                return;
            }
        }

        Debug.LogWarning($"Room '{roomName}' not found!");
    }
    public void SwitchToRoom(GameObject room)
    {
        if (rooms == null || room == null) return;

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] == room)
            {
                SwitchToRoom(i);
                return;
            }
        }

        mapObject = room;
        CalculateMapSize();
        RecalculateMaxZoom();
        _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);

        if (smoothTransition)
        {
            targetPosition = new Vector3(mapCenter.x, transform.position.y, mapCenter.z);
            isTransitioning = true;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.x = mapCenter.x;
            pos.z = mapCenter.z;
            transform.position = pos;
            ClampPosition();
        }
    }
    public void MoveTo(Vector3 worldPosition, bool instant = false)
    {
        Vector3 newPos = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);

        if (instant || !smoothTransition)
        {
            transform.position = newPos;
            ClampPosition();
        }
        else
        {
            targetPosition = newPos;
            isTransitioning = true;
        }
    }

    public int GetCurrentRoomIndex()
    {
        return currentRoomIndex;
    }

    public GameObject GetCurrentRoom()
    {
        return mapObject;
    }

    private void ApplyZoom()
    {
        _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);

        _camera.orthographicSize = Mathf.Lerp(
            _camera.orthographicSize,
            _currentZoom,
            Time.deltaTime * zoomSmoothness
        );
    }

    private void ClampPosition()
    {
        if (_camera == null || mapObject == null) return;

        float vertExtent = _camera.orthographicSize;
        float horzExtent = vertExtent * _camera.aspect;

        if (mapWidth < horzExtent * 2f || mapHeight < vertExtent * 2f)
            return;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        float minX = mapCenter.x - halfWidth + horzExtent;
        float maxX = mapCenter.x + halfWidth - horzExtent;
        float minZ = mapCenter.z - halfHeight + vertExtent;
        float maxZ = mapCenter.z + halfHeight - vertExtent;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }
}