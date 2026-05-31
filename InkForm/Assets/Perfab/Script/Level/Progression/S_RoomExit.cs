using UnityEngine;

/// <summary>
/// Facility exit door. When the player enters the trigger, requests a move to targetRoom.
/// The progression controller validates adjacency and loads the room (or routes to an ending).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class S_RoomExit : MonoBehaviour
{
    [SerializeField] private RoomId targetRoom = RoomId.None;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!S_PlayerLookup.IsPlayer(other))
            return;

        if (targetRoom == RoomId.None)
        {
            Debug.LogWarning($"[RoomExit] '{name}' has no target room set.");
            return;
        }

        S_GameEvent.RoomEnterRequested(targetRoom.ToString());
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.9f, $"-> {targetRoom}");
#endif
    }
}
