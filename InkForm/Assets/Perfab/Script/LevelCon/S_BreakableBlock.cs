using UnityEngine;

public class S_BreakableBlock : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minimumSprintBreakExitSpeed = 8f;
    [SerializeField, Min(0f)] private float sprintBreakthroughPreserveTime = 0.08f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        S_Player player = S_Player.Instance;
        if (player == null || player.getForm())
            return;

        if (player.IsSprintMomentumActive)
        {
            PreserveSprintMomentum(player, collision);
        }

        DisableColliders();
        Destroy(gameObject);
    }

    private void PreserveSprintMomentum(S_Player player, Collision2D collision)
    {
        Rigidbody2D playerRigidbody = player.GetRigidbody();
        if (playerRigidbody == null)
            return;

        float currentHorizontalSpeed = Mathf.Abs(playerRigidbody.linearVelocity.x);
        float collisionHorizontalSpeed = Mathf.Abs(collision.relativeVelocity.x);
        float exitSpeed = Mathf.Max(currentHorizontalSpeed, collisionHorizontalSpeed, minimumSprintBreakExitSpeed);
        float direction = GetBreakthroughDirection(player, playerRigidbody);

        player.ForceSprintBreakthrough(direction, exitSpeed, sprintBreakthroughPreserveTime);
    }

    private float GetBreakthroughDirection(S_Player player, Rigidbody2D playerRigidbody)
    {
        if (Mathf.Abs(playerRigidbody.linearVelocity.x) > 0.01f)
            return Mathf.Sign(playerRigidbody.linearVelocity.x);

        return player.GetFaceRight() ? 1f : -1f;
    }

    private void DisableColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D blockCollider in colliders)
        {
            blockCollider.enabled = false;
        }
    }
}
