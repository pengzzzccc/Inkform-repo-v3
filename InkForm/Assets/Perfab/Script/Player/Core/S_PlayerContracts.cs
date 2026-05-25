using UnityEngine;

public interface IPlayerActor
{
    Rigidbody2D Rigidbody { get; }
    Collider2D Collider { get; }
    Transform BodyTransform { get; }
    bool IsFluidForm { get; }
    bool IsParalyzed { get; }
    bool IsSprintMomentumActive { get; }
    bool FacingRight { get; }

    void Teleport(Vector2 targetPos);
    void SetMovementLocked(bool locked);
    void ApplyParalyze(float duration, float slowMultiplier);
    void ForceSprintBreakthrough(float direction, float minimumHorizontalSpeed, float duration);
    void CancelSprintCharge();
    void CancelActiveSkills();
}

public static class S_PlayerLookup
{
    private const string PlayerTag = "Player";

    public static bool TryGet(Collider2D collider, out IPlayerActor player)
    {
        player = null;

        if (collider == null)
            return false;

        S_Player concretePlayer = collider.GetComponentInParent<S_Player>();
        if (concretePlayer != null)
        {
            player = concretePlayer;
            return true;
        }

        if (collider.CompareTag(PlayerTag) && S_Player.Instance != null)
        {
            player = S_Player.Instance;
            return true;
        }

        return false;
    }

    public static bool TryGet(Collision2D collision, out IPlayerActor player)
    {
        player = null;

        if (collision == null)
            return false;

        return TryGet(collision.collider, out player);
    }

    public static bool TryGetActive(out IPlayerActor player)
    {
        player = S_Player.Instance;
        return player != null;
    }

    public static bool IsPlayer(Collider2D collider)
    {
        return TryGet(collider, out _);
    }

    public static bool IsPlayer(Collision2D collision)
    {
        return TryGet(collision, out _);
    }
}
