using UnityEngine;

/// <summary>
/// A platform/wall that flips global gravity. When the player passes over / touches its trigger,
/// the global gravity is set to the configured direction and stays that way until the player hits
/// another switcher with a different direction. Player skills are unaffected.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class S_GravitySwitcher : MonoBehaviour
{
    public enum GravityDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    [Tooltip("Direction gravity will point after the player touches this switcher.")]
    [SerializeField] private GravityDirection direction = GravityDirection.Down;
    [Tooltip("Fire only the first time the player touches this switcher.")]
    [SerializeField] private bool onlyOnce = false;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0.4f, 0.9f, 1f, 0.9f);

    private bool used;

    private static Vector2 ToVector(GravityDirection d)
    {
        switch (d)
        {
            case GravityDirection.Up: return Vector2.up;
            case GravityDirection.Left: return Vector2.left;
            case GravityDirection.Right: return Vector2.right;
            default: return Vector2.down;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!S_PlayerLookup.IsPlayer(other))
            return;

        if (onlyOnce && used)
            return;

        // Ignore if gravity is already pointing this way.
        if (S_GravityState.GravityDir == ToVector(direction))
            return;

        used = true;
        S_GravityState.SetGravityDirection(ToVector(direction));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 origin = transform.position;
        Vector3 dir = ToVector(direction);
        Vector3 tip = origin + dir * 1.2f;
        Gizmos.DrawLine(origin, tip);

        // Arrowhead
        Vector3 back = -dir * 0.35f;
        Vector3 side = new Vector3(-dir.y, dir.x, 0f) * 0.25f;
        Gizmos.DrawLine(tip, tip + back + side);
        Gizmos.DrawLine(tip, tip + back - side);
    }
}
