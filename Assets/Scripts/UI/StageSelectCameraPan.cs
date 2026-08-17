using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// StageSelect 맵을 좌우로 볼 때 Main Camera의 X 위치를 이동합니다.
/// </summary>
public class StageSelectCameraPan : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform mapBounds;
    [SerializeField] private GameObject blockPanWhenActive;
    [SerializeField] private float dragThresholdPixels = 12f;
    [SerializeField] private float wheelPanSpeed = 180f;

    private Vector2 _pressScreenPos;
    private Vector3 _pressCameraPos;
    private bool _pointerHeld;
    private bool _didDrag;
    private readonly Vector3[] _worldCorners = new Vector3[4];
    private readonly List<RaycastResult> _raycastHits = new List<RaycastResult>();

    public bool DidDragThisPointer => _didDrag;
    public bool InputLocked { get; set; }

    public void Configure(RectTransform bounds, GameObject blockWhenActive)
    {
        mapBounds = bounds;
        blockPanWhenActive = blockWhenActive;
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null && targetCamera.transform.localScale != Vector3.one)
            targetCamera.transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (targetCamera == null)
            return;

        if (InputLocked)
            return;

        if (blockPanWhenActive != null && blockPanWhenActive.activeInHierarchy)
            return;

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.scroll.ReadValue().y != 0f)
        {
            float scroll = mouse.scroll.ReadValue().y;
            SetCameraX(targetCamera.transform.position.x - scroll * wheelPanSpeed * WorldUnitsPerPixel());
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _pointerHeld = !IsPointerOnHud();
            _didDrag = false;
            _pressScreenPos = mouse.position.ReadValue();
            _pressCameraPos = targetCamera.transform.position;
        }

        if (_pointerHeld && mouse.leftButton.isPressed)
        {
            Vector2 current = mouse.position.ReadValue();
            float dx = current.x - _pressScreenPos.x;
            if (!_didDrag && Mathf.Abs(dx) >= dragThresholdPixels)
                _didDrag = true;

            if (_didDrag)
                SetCameraX(_pressCameraPos.x - dx * WorldUnitsPerPixel());
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            _pointerHeld = false;
    }

    public void FocusOnWorldX(float worldX)
    {
        if (targetCamera == null)
            return;

        SetCameraX(worldX);
    }

    private void SetCameraX(float worldX)
    {
        if (targetCamera == null)
            return;

        var pos = targetCamera.transform.position;
        pos.x = ClampCameraX(worldX);
        targetCamera.transform.position = pos;
    }

    private bool IsPointerOnHud()
    {
        if (EventSystem.current == null || Mouse.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        _raycastHits.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastHits);
        for (int i = 0; i < _raycastHits.Count; i++)
        {
            var canvas = _raycastHits[i].gameObject.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                return true;
        }

        return false;
    }

    private float ClampCameraX(float worldX)
    {
        if (mapBounds == null || targetCamera == null || !targetCamera.orthographic)
            return worldX;

        mapBounds.GetWorldCorners(_worldCorners);
        float minX = _worldCorners[0].x;
        float maxX = _worldCorners[2].x;
        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float left = minX + halfWidth;
        float right = maxX - halfWidth;
        if (right < left)
            return (minX + maxX) * 0.5f;

        return Mathf.Clamp(worldX, left, right);
    }

    private float WorldUnitsPerPixel()
    {
        if (targetCamera == null || Screen.height <= 0)
            return 0.01f;

        if (targetCamera.orthographic)
            return (targetCamera.orthographicSize * 2f) / Screen.height;

        return 0.01f;
    }
}
