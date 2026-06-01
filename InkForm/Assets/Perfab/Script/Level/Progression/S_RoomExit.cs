using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Facility exit door. When the player presses interact inside the trigger, requests a move to targetRoom.
/// The progression controller validates adjacency and loads the room (or routes to an ending).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class S_RoomExit : MonoBehaviour
{
    [SerializeField] private RoomId targetRoom = RoomId.None;
    [SerializeField] private Transform arrivalPoint;
    [SerializeField] private Vector2 arrivalOffset = Vector2.zero;
    [SerializeField] private bool arrivalFacingRight = true;
    [SerializeField] private Transform shakeTarget;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.18f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.08f;
    [SerializeField, Min(1f)] private float shakeFrequency = 24f;

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private IPlayerActor playerInRange;
    private Coroutine shakeRoutine;

    public RoomId TargetRoom => targetRoom;
    public bool ArrivalFacingRight => arrivalFacingRight;

    public Vector2 GetArrivalPosition()
    {
        return arrivalPoint != null
            ? (Vector2)arrivalPoint.position
            : (Vector2)transform.position + arrivalOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!S_PlayerLookup.TryGet(other, out IPlayerActor player))
            return;

        playerColliders.Add(other);
        playerInRange = player;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!playerColliders.Remove(other))
            return;

        if (playerColliders.Count == 0)
            playerInRange = null;
    }

    private void Update()
    {
        if (playerInRange == null || !S_PlayerInteractInput.WasPressedThisFrame())
            return;

        if (targetRoom == RoomId.None)
        {
            Shake();
            return;
        }

        S_GameEvent.RoomEnterRequested(new S_RoomTransitionRequest(targetRoom, S_RoomEntryMode.Door));
    }

    private void Shake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Transform target = shakeTarget != null ? shakeTarget : transform;
        Vector3 basePosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float offset = Mathf.Sin(elapsed * shakeFrequency) * shakeDistance * fade;
            target.localPosition = basePosition + Vector3.right * offset;
            yield return null;
        }

        target.localPosition = basePosition;
        shakeRoutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.8f);
        Vector3 arrivalPosition = arrivalPoint != null
            ? arrivalPoint.position
            : transform.position + (Vector3)arrivalOffset;
        Gizmos.DrawWireSphere(arrivalPosition, 0.18f);
        Gizmos.DrawLine(transform.position, arrivalPosition);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.9f, $"-> {targetRoom}");
#endif
    }
}
