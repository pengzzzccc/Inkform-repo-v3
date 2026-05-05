using UnityEngine;

/// <summary>
/// Base class for all NPC types in InkForm.
/// Subclasses: S_NPCEnemy (guard patrol/chase), S_NPCDialogue (dialogue scenes),
/// S_NPCStory (K-01 fixed sequences), S_NPCCamera (drones).
/// </summary>
public class S_NPCbase : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] protected string npcName = "Unnamed";
    [SerializeField] protected string npcID = "npc_000";

    [Header("Interaction")]
    [SerializeField] protected bool canInteract = true;
    [SerializeField] protected float interactRange = 3f;

    [Header("Dialogue (Optional)")]
    [SerializeField] protected TextAsset dialogueAsset;

    protected SpriteRenderer npcSprite;
    protected Rigidbody2D npcRig;
    protected Collider2D npcCol;

    protected bool isActive = true;
    protected bool facingRight = true;

    public string NPCName => npcName;
    public string NPCID => npcID;
    public bool CanInteract => canInteract;

    protected virtual void Awake()
    {
        npcSprite = GetComponent<SpriteRenderer>();
        npcRig = GetComponent<Rigidbody2D>();
        npcCol = GetComponent<Collider2D>();
    }

    protected virtual void OnEnable()
    {
        S_GameEvent.OnGameStart += HandleGameStart;
        S_GameEvent.OnGameRestart += HandleGameRestart;
    }

    protected virtual void OnDisable()
    {
        S_GameEvent.OnGameStart -= HandleGameStart;
        S_GameEvent.OnGameRestart -= HandleGameRestart;
    }

    /// <summary>Called when player presses interact near this NPC.</summary>
    public virtual void OnInteract()
    {
        if (!canInteract || !isActive) return;
        S_GameEvent.NPCInteract(npcID);
    }

    protected virtual void HandleGameStart() { }
    protected virtual void HandleGameRestart() { }

    /// <summary>Flip sprite to face a world-space X direction.</summary>
    protected void FlipSprite(float directionX)
    {
        if (directionX == 0f) return;
        bool shouldFaceRight = directionX > 0f;
        if (shouldFaceRight == facingRight) return;
        facingRight = shouldFaceRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    /// <summary>Set NPC active/inactive (disable patrol, hide, etc).</summary>
    public virtual void SetActive(bool active)
    {
        isActive = active;
        if (npcSprite != null) npcSprite.enabled = active;
        if (npcCol != null) npcCol.enabled = active;
    }

    /// <summary>Distance from this NPC to the player's body (Rigidbody2D), if player exists.</summary>
    protected float DistanceToPlayer()
    {
        if (S_Player.Instance == null) return float.MaxValue;
        return Vector2.Distance(transform.position, S_Player.Instance.GetBodyTransform().position);
    }

    /// <summary>Is the player within interactRange?</summary>
    protected bool PlayerInRange()
    {
        return DistanceToPlayer() <= interactRange;
    }
}