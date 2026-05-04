using UnityEngine;

/// <summary>
/// Trigger helper for S_MoveBlock — detects when block-type objects enter/exit the trigger collider
/// and sets the parent MoveBlock's trigger state accordingly.
///
/// Attach this to a child trigger collider of an S_MoveBlock object.
/// When a "block"-tagged object enters the trigger, the parent block starts moving toward side_2.
/// When the object exits, the block returns to side_1.
///
/// Uses CompareTag() for zero-allocation tag comparison.
/// </summary>
public class S_setTrigger : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Runtime State
    // ──────────────────────────────────────────────

    /// <summary>Reference to the parent S_MoveBlock component. Cached on Awake.</summary>
    private S_MoveBlock parent;

    // ──────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────

    /// <summary>
    /// Caches the reference to the parent S_MoveBlock component.
    /// Uses GetComponentInParent to find the S_MoveBlock on any ancestor GameObject.
    /// </summary>
    void Awake()
    {
        parent = GetComponentInParent<S_MoveBlock>();
    }

    // ──────────────────────────────────────────────
    //  Trigger Callbacks
    // ──────────────────────────────────────────────

    /// <summary>
    /// Called when a collider enters this trigger zone.
    /// If the entering object is tagged "block", activates the parent MoveBlock
    /// to start moving toward its target position (side_2).
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("block")) parent.SetTriggered(true);
    }

    /// <summary>
    /// Called when a collider exits this trigger zone.
    /// If the exiting object is tagged "block", deactivates the parent MoveBlock
    /// to start returning to its default position (side_1).
    /// </summary>
    /// <param name="collision">The collider that exited the trigger.</param>
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("block")) parent.SetTriggered(false);
    }
}