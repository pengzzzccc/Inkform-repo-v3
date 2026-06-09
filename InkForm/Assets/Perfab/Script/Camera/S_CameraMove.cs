using UnityEngine;

public class S_CameraMove : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private GameObject target;
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Y-Axis Dead Zone")]
    [Tooltip("Height of the vertical dead zone. Camera won't move Y while player stays within this band.")]
    [SerializeField, Min(0.1f)] private float deadZoneHeight = 3f;

    [Header("Camera Bounds (World Space)")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 80f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 50f;

    [Header("Manual Control")]
    [SerializeField] private float manualMoveSpeed = 8f;
    [SerializeField] private float manualMaxDistanceFromTarget = 8f;
    [SerializeField] private float recenterSpeed = 6f;

    [Header("Look Mode Zoom")]
    [SerializeField] private Camera cam;
    [SerializeField, Min(0.5f)] private float minZoom = 3f;
    [SerializeField, Min(0.5f)] private float maxZoom = 12f;
    [SerializeField, Min(0.1f)] private float zoomSpeed = 8f;

    [Header("Gravity Rotation")]
    [SerializeField] private bool rotateWithGravity = true;
    [SerializeField, Min(0f)] private float cameraRotateSpeed = 180f;

    [Header("Gizmos")]
    [SerializeField] private bool drawDeadZoneGizmo = true;
    [SerializeField] private bool drawBoundsGizmo = true;
    [SerializeField] private Color deadZoneColor = new Color(0.2f, 0.8f, 1f, 0.25f);
    [SerializeField] private Color boundsColor = new Color(1f, 0.9f, 0.2f, 0.5f);

    private bool manualControlActive = false;
    private float cameraCenterY;
    private float defaultZoom;

    void Start()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
        if (cam != null)
            defaultZoom = cam.orthographicSize;

        if (target != null)
        {
            Vector3 tPos = target.transform.position;
            transform.position = new Vector3(tPos.x, tPos.y, transform.position.z);
            cameraCenterY = tPos.y;
        }
    }

    void Update()
    {
        RotateWithGravity();

        if (target == null)
            return;

        if (manualControlActive)
            return;

        // Smoothly restore default zoom once look mode ends.
        if (cam != null && !Mathf.Approximately(cam.orthographicSize, defaultZoom))
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, defaultZoom, 1f - Mathf.Exp(-zoomSpeed * Time.deltaTime));

        Vector3 targetPos = target.transform.position;

        // X-axis: always smooth follow
        float newX = Mathf.Lerp(transform.position.x, targetPos.x, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        // Y-axis: dead zone logic
        float deltaY = targetPos.y - cameraCenterY;
        if (Mathf.Abs(deltaY) > deadZoneHeight * 0.5f)
        {
            // Player left dead zone — snap dead zone edge to player, then smooth
            float targetCenterY = targetPos.y - Mathf.Sign(deltaY) * deadZoneHeight * 0.5f;
            cameraCenterY = Mathf.Lerp(cameraCenterY, targetCenterY, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        }

        float newY = cameraCenterY;

        // Clamp to bounds
        newX = Mathf.Clamp(newX, minX, maxX);
        newY = Mathf.Clamp(newY, minY, maxY);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    public void BeginManualControl()
    {
        manualControlActive = true;
    }

    /// <summary>
    /// Drive the camera while in look mode: pan with input, zoom with the zoom buttons
    /// (persists, clamped), and linearly recenter on the player when there is no pan input.
    /// </summary>
    public void LookTick(Vector2 pan, bool zoomIn, bool zoomOut)
    {
        if (!manualControlActive || target == null)
            return;

        if (cam != null && (zoomIn ^ zoomOut))
        {
            float dir = zoomIn ? -1f : 1f; // zoom in -> smaller orthographic size
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize + dir * zoomSpeed * Time.unscaledDeltaTime,
                minZoom, maxZoom);
        }

        Vector3 targetCenter = new Vector3(target.transform.position.x, target.transform.position.y, transform.position.z);

        if (pan.sqrMagnitude > 0.0001f)
        {
            // Camera is rotated to match gravity, so pan along its own (screen) axes.
            Vector3 delta = transform.right * pan.x + transform.up * pan.y;
            if (delta.sqrMagnitude > 1f)
                delta.Normalize();

            transform.position += delta * (manualMoveSpeed * Time.unscaledDeltaTime);

            Vector3 offset = transform.position - targetCenter;
            offset.z = 0f;
            if (offset.magnitude > manualMaxDistanceFromTarget)
                transform.position = targetCenter + offset.normalized * manualMaxDistanceFromTarget;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetCenter, 1f - Mathf.Exp(-recenterSpeed * Time.unscaledDeltaTime));
        }

        transform.position = ClampToBounds(transform.position);
    }

    public void EndManualControl()
    {
        manualControlActive = false;
        if (target != null)
            cameraCenterY = transform.position.y;
    }

    /// <summary>Rotate the camera so screen-up aligns with the current gravity-up (linear).</summary>
    private void RotateWithGravity()
    {
        if (!rotateWithGravity)
            return;

        Vector2 up = S_GravityState.GravityUp;
        float targetAngle = Mathf.Atan2(up.y, up.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, cameraRotateSpeed * Time.deltaTime);
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY),
            transform.position.z);
    }

    /// <summary>
    /// Set camera bounds at runtime (e.g. from a level config).
    /// </summary>
    public void SetBounds(float newMinX, float newMaxX, float newMinY, float newMaxY)
    {
        minX = newMinX;
        maxX = newMaxX;
        minY = newMinY;
        maxY = newMaxY;
    }

    private void OnDrawGizmosSelected()
    {
        if (drawBoundsGizmo)
            DrawBoundsGizmo();

        if (drawDeadZoneGizmo)
            DrawDeadZoneGizmo();
    }

    private void DrawDeadZoneGizmo()
    {
        Gizmos.color = deadZoneColor;
        float camHeight = Camera.main != null ? Camera.main.orthographicSize * 2f : 10f;
        float camWidth = camHeight * (Camera.main != null ? Camera.main.aspect : 16f / 9f);
        Vector3 center = new Vector3(transform.position.x, transform.position.y, 0f);
        Gizmos.DrawCube(center, new Vector3(camWidth, deadZoneHeight, 0.01f));

        Gizmos.color = new Color(deadZoneColor.r, deadZoneColor.g, deadZoneColor.b, 0.8f);
        Gizmos.DrawWireCube(center, new Vector3(camWidth, deadZoneHeight, 0.01f));
    }

    private void DrawBoundsGizmo()
    {
        Gizmos.color = boundsColor;
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;
        float sizeX = maxX - minX;
        float sizeY = maxY - minY;
        Vector3 center = new Vector3(centerX, centerY, 0f);
        Gizmos.DrawWireCube(center, new Vector3(sizeX, sizeY, 0.01f));

        // Draw corner markers for clarity
        Gizmos.color = Color.yellow;
        float markSize = Mathf.Min(sizeX, sizeY) * 0.03f;
        Gizmos.DrawCube(new Vector3(minX, minY, 0f), Vector3.one * markSize);
        Gizmos.DrawCube(new Vector3(maxX, minY, 0f), Vector3.one * markSize);
        Gizmos.DrawCube(new Vector3(minX, maxY, 0f), Vector3.one * markSize);
        Gizmos.DrawCube(new Vector3(maxX, maxY, 0f), Vector3.one * markSize);
    }
}
