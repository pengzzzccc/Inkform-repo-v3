using UnityEngine;

public class S_BreakableBlock : MonoBehaviour
{
    [Header("Drop Resource")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private string resourceId = "block_fragment";
    [SerializeField, Min(0)] private int dropCount = 1;
    [SerializeField, Min(1)] private int resourceAmountPerDrop = 1;
    [SerializeField, Min(0f)] private float dropSpreadX = 1.2f;
    [SerializeField, Min(0f)] private float dropPopVelocityY = 3.5f;
    [SerializeField, Min(0f)] private float pickupDelay = 0.25f;
    [SerializeField, Min(0f)] private float dropLifetime = 12f;
    [SerializeField] private Vector2 dropSpawnOffset = new Vector2(0f, 0.35f);

    [Header("Sprint Breakthrough")]
    [SerializeField, Min(0f)] private float minimumSprintBreakExitSpeed = 8f;
    [SerializeField, Min(0f)] private float sprintBreakthroughPreserveTime = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] breakClips;

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

        PlayBreakSfx();
        SpawnDrops();
        DisableColliders();
        Destroy(gameObject);
    }

    private void SpawnDrops()
    {
        if (dropPrefab == null || dropCount <= 0)
            return;

        Vector3 spawnPosition = transform.position + (Vector3)dropSpawnOffset;

        for (int i = 0; i < dropCount; i++)
        {
            GameObject drop = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);
            if (drop.GetComponent<S_Key>() != null)
                continue;

            S_DroppedResourceItem droppedItem = drop.GetComponent<S_DroppedResourceItem>();
            if (droppedItem == null)
                droppedItem = drop.AddComponent<S_DroppedResourceItem>();

            Vector2 launchVelocity = CreateDropVelocity();
            droppedItem.Initialize(resourceId, resourceAmountPerDrop, launchVelocity, pickupDelay, dropLifetime);
        }
    }

    private Vector2 CreateDropVelocity()
    {
        float horizontalVelocity = dropSpreadX > 0f ? Random.Range(-dropSpreadX, dropSpreadX) : 0f;
        float verticalBonus = dropPopVelocityY > 0f ? Random.Range(0f, dropPopVelocityY * 0.25f) : 0f;
        return new Vector2(horizontalVelocity, dropPopVelocityY + verticalBonus);
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

    private void PlayBreakSfx()
    {
        AudioClip clip = GetRandomBreakClip();
        if (clip == null)
            return;

        S_GameEvent.PlaySFX(clip);
    }

    private AudioClip GetRandomBreakClip()
    {
        if (breakClips == null || breakClips.Length == 0)
            return null;

        int validClipCount = 0;
        for (int i = 0; i < breakClips.Length; i++)
        {
            if (breakClips[i] != null)
                validClipCount++;
        }

        if (validClipCount == 0)
            return null;

        int randomIndex = Random.Range(0, validClipCount);
        for (int i = 0; i < breakClips.Length; i++)
        {
            AudioClip clip = breakClips[i];
            if (clip == null)
                continue;

            if (randomIndex == 0)
                return clip;

            randomIndex--;
        }

        return null;
    }
}
