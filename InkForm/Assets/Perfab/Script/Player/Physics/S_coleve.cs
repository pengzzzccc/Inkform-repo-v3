using UnityEngine;

/// <summary>
/// Collision Events handler — manages ground detection and hazard (lava) collision for the player.
/// Attached to the player's body child GameObject.
///
/// Responsibilities:
/// - Tracks whether the player is standing on a ground surface (playerOnGround)
/// - Fires the PlayerDied event when the player touches a "lava" tagged object
///
/// Ground detection uses contact normal direction rather than simple enter/exit,
/// ensuring only downward-facing contacts count as ground (prevents false positives
/// from wall or ceiling collisions).
/// </summary>
public class S_coleve : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────

    /// <summary>Singleton instance for accessing ground state from other scripts.</summary>
    public static S_coleve Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Runtime State
    // ──────────────────────────────────────────────

    /// <summary>True when the player is in contact with a ground surface. Reset on exit.</summary>
    private bool playerOnGround;

    // ──────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────

    /// <summary>Initializes the singleton instance.</summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ──────────────────────────────────────────────
    //  Public Queries
    // ──────────────────────────────────────────────

    /// <summary>Returns whether the player is currently on the ground.</summary>
    public bool getPlayerOnGround()
    {
        return playerOnGround;
    }

    // ──────────────────────────────────────────────
    //  Collision Callbacks
    // ──────────────────────────────────────────────

    /// <summary>
    /// Called when this collider begins touching another collider.
    /// 1. Checks contact normals to determine if the contact is a ground surface
    ///    (normal pointing upward, dot > 0.5)
    /// 2. If the colliding object is tagged "lava", fires the PlayerDied event
    /// </summary>
    /// <param name="collision">Collision data including contacts and the other GameObject.</param>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check contact normals to determine if this is a ground collision
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // A contact normal pointing upward (y > 0.5) indicates standing on ground
            if (contact.normal.y > 0.5f)
            {
                playerOnGround = true;
                break;
            }
        }

        // Hazard check: touching lava kills the player
        if (collision.gameObject.CompareTag("lava")) S_GameEvent.PlayerDied();
    }

    /// <summary>
    /// Called when this collider stops touching another collider.
    /// Resets ground state only when leaving objects on the "Ground" layer,
    /// preventing false resets from leaving non-ground objects.
    /// </summary>
    /// <param name="collision">Collision data for the separation event.</param>
    void OnCollisionExit2D(Collision2D collision)
    {
        // Only reset ground state when leaving ground-layer objects
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            playerOnGround = false;
    }
}