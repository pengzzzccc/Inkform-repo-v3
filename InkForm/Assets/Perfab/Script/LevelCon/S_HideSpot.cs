using UnityEngine;

/// <summary>
/// Attach to a trigger collider on any object (cabinets, pillars, machines).
/// Allows the player to hide from guards, causing them to lose their target.
/// </summary>
public class S_HideSpot : MonoBehaviour
{
    [Header("Hide Settings")]
    [SerializeField] private float hideDuration = 0f;
    [SerializeField] private float cooldownAfterHide = 2f;
    [SerializeField] private Vector2 exitOffset = new Vector2(1f, 0f);
    [SerializeField] private float defaultGravityScale = 4f;

    private bool isHiding = false;
    private float hideTimer = 0f;
    private float cooldownTimer = 0f;
    private bool onCooldown = false;

    private SpriteRenderer playerSprite;
    private Collider2D playerCol;
    private Rigidbody2D playerRig;
    private bool playerInRange = false;

    private void Update()
    {
        if (onCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
                onCooldown = false;
        }

        if (isHiding)
        {
            // Notify suspicion system to decay while hidden
            if (S_SuspicionSystem.Instance != null)
                S_SuspicionSystem.Instance.HideDecay();

            if (hideDuration > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                    ExitHide();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !onCooldown && !isHiding)
        {
            playerInRange = true;
            CachePlayerComponents(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerInRange)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (isHiding)
                    ExitHide();
                else if (!onCooldown)
                    EnterHide();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void CachePlayerComponents(Collider2D player)
    {
        playerSprite = player.GetComponent<SpriteRenderer>();
        playerCol = player;
        playerRig = player.attachedRigidbody;
    }

    private void EnterHide()
    {
        isHiding = true;
        S_SuspicionSystem.PlayerHidden = true;

        if (playerSprite != null) playerSprite.enabled = false;
        if (playerCol != null) playerCol.enabled = false;
        if (playerRig != null)
        {
            playerRig.linearVelocity = Vector2.zero;
            playerRig.gravityScale = 0f;
        }

        if (hideDuration > 0f)
            hideTimer = hideDuration;
    }

    private void ExitHide()
    {
        isHiding = false;
        S_SuspicionSystem.PlayerHidden = false;

        if (playerSprite != null) playerSprite.enabled = true;
        if (playerCol != null) playerCol.enabled = true;
        if (playerRig != null)
        {
            playerRig.gravityScale = defaultGravityScale;
            playerRig.position += exitOffset;
        }

        onCooldown = true;
        cooldownTimer = cooldownAfterHide;
    }

    private void OnDisable()
    {
        if (isHiding)
        {
            ExitHide();
        }
    }

}