using UnityEngine;

/// <summary>
/// Fires the ending when the player enters (e.g. the factory exit/vehicle).
/// Both endings funnel through S_GameEvent.EndingRequested() -> controller loads END.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class S_EndingTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !S_PlayerLookup.IsPlayer(other))
            return;

        triggered = true;
        S_GameEvent.EndingRequested();
    }
}
