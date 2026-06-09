using UnityEngine;

/// <summary>
/// Global gravity state. Holds the current "gravity up" unit vector (the direction opposite to
/// gravity) and drives Physics2D.gravity. Changing direction preserves the configured magnitude.
/// Broadcasts S_GameEvent.OnGravityChanged so the player (and anything else) can re-orient.
/// </summary>
public static class S_GravityState
{
    private static Vector2 gravityUp = Vector2.up;
    private static float magnitude = 9.81f;

    /// <summary>Unit vector pointing opposite to gravity (the player's "up").</summary>
    public static Vector2 GravityUp => gravityUp;

    /// <summary>Unit vector pointing along gravity (the player's "down").</summary>
    public static Vector2 GravityDir => -gravityUp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        magnitude = Mathf.Max(0.01f, Physics2D.gravity.magnitude);
        gravityUp = Vector2.up;
        Physics2D.gravity = new Vector2(0f, -magnitude);
    }

    /// <summary>Set the global gravity to point along <paramref name="direction"/> (keeps magnitude).</summary>
    public static void SetGravityDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector2 dir = direction.normalized;
        gravityUp = -dir;
        Physics2D.gravity = dir * magnitude;
        S_GameEvent.GravityChanged(gravityUp);
    }
}
