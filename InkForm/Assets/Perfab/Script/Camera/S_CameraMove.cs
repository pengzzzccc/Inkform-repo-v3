using System;
using UnityEngine;

public class S_CameraMove : MonoBehaviour
{
    private const string CameraPanInputLockId = "CameraPan";

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

    [Header("Gizmos")]
    [SerializeField] private bool drawDeadZoneGizmo = true;
    [SerializeField] private bool drawBoundsGizmo = true;
    [SerializeField] private Color deadZoneColor = new Color(0.2f, 0.8f, 1f, 0.25f);
    [SerializeField] private Color boundsColor = new Color(1f, 0.9f, 0.2f, 0.5f);

    private bool manualControlActive = false;
    private float cameraCenterY;

    void Start()
    {
        if (target != null)
        {
            Vector3 tPos = target.transform.position;
            transform.position = new Vector3(tPos.x, tPos.y, transform.position.z);
            cameraCenterY = tPos.y;
        }
    }

    void Update()
    {
        if (target == null)
            return;

        if (manualControlActive)
            return;

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

    public void ManualControlTick(Vector2 moveInput)
    {
        if (!manualControlActive || target == null)
            return;

        Vector3 delta = new Vector3(moveInput.x, moveInput.y, 0f);
        if (delta.sqrMagnitude > 1f)
            delta.Normalize();

        transform.position += delta * (manualMoveSpeed * Time.unscaledDeltaTime);

        Vector3 targetCenter = new Vector3(target.transform.position.x, target.transform.position.y, transform.position.z);
        Vector3 offset = transform.position - targetCenter;
        offset.z = 0f;

        if (offset.magnitude > manualMaxDistanceFromTarget)
            transform.position = targetCenter + offset.normalized * manualMaxDistanceFromTarget;
    }

    public void EndManualControl()
    {
        manualControlActive = false;
        if (target != null)
            cameraCenterY = transform.position.y;
    }

    /// <summary>
    /// Coroutine: pause player follow, pan camera to target world position,
    /// hold, then return to player and resume follow.
    /// </summary>
    public System.Collections.IEnumerator PanToTarget(
        Vector3 targetWorldPos,
        float moveDuration,
        float holdDuration,
        float returnDuration,
        Func<bool> skipRequested = null)
    {
        BeginManualControl();
        S_GameEvent.PushGameplayInputLock(CameraPanInputLockId);
        S_GameEvent.CameraPanStarted();
        bool skipped = false;
        try
        {
            yield return LerpCameraTo(targetWorldPos, moveDuration, skipRequested, () => skipped = true);
            if (skipped)
            {
                SnapCameraToPlayer();
                yield break;
            }

            if (holdDuration > 0f)
            {
                yield return WaitOrSkip(holdDuration, skipRequested, () => skipped = true);
                if (skipped)
                {
                    SnapCameraToPlayer();
                    yield break;
                }
            }

            if (target != null)
            {
                Vector3 playerPos = new Vector3(target.transform.position.x, target.transform.position.y, transform.position.z);
                yield return LerpCameraTo(playerPos, returnDuration, skipRequested, () => skipped = true);
                if (skipped)
                    SnapCameraToPlayer();
            }
        }
        finally
        {
            EndManualControl();
            S_GameEvent.CameraPanEnded();
            S_GameEvent.PopGameplayInputLock(CameraPanInputLockId);
        }
    }

    private System.Collections.IEnumerator LerpCameraTo(
        Vector3 destination,
        float duration,
        Func<bool> skipRequested,
        Action onSkipped)
    {
        Vector3 endPos = ClampToBounds(destination);

        if (duration <= 0f)
        {
            transform.position = endPos;
            yield break;
        }

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ShouldSkip(skipRequested))
            {
                onSkipped?.Invoke();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = ClampToBounds(Vector3.Lerp(startPos, endPos, t));
            yield return null;
        }

        transform.position = endPos;
    }

    private System.Collections.IEnumerator WaitOrSkip(float duration, Func<bool> skipRequested, Action onSkipped)
    {
        float endTime = Time.unscaledTime + duration;
        while (Time.unscaledTime < endTime)
        {
            if (ShouldSkip(skipRequested))
            {
                onSkipped?.Invoke();
                yield break;
            }

            yield return null;
        }
    }

    private static bool ShouldSkip(Func<bool> skipRequested)
    {
        return skipRequested != null && skipRequested();
    }

    private void SnapCameraToPlayer()
    {
        if (target == null)
            return;

        transform.position = ClampToBounds(new Vector3(
            target.transform.position.x,
            target.transform.position.y,
            transform.position.z));
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
