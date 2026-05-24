using UnityEngine;

/// <summary>
/// Level exit gate. Locked by default, unlocks when enough keys are collected.
/// When unlocked, player contact loads the next level.
/// </summary>
public class S_ExitGate : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private int requiredKeys = 1;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer gateSprite;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = new Color(0.3f, 1f, 0.4f, 1f);

    private bool isUnlocked;

    private void Awake()
    {
        if (gateSprite == null)
            gateSprite = GetComponentInChildren<SpriteRenderer>();

        SetLocked();
    }

    private void OnEnable()
    {
        S_GameEvent.OnKeyCountChanged += HandleKeyCountChanged;
        // Re-check on enable (in case keys were collected before gate spawned)
        CheckUnlock();
    }

    private void OnDisable()
    {
        S_GameEvent.OnKeyCountChanged -= HandleKeyCountChanged;
    }

    private void HandleKeyCountChanged(int collected, int total)
    {
        if (collected >= requiredKeys)
            SetUnlocked();
    }

    private void CheckUnlock()
    {
        if (S_Key.CollectedKeys >= requiredKeys)
            SetUnlocked();
    }

    private void SetLocked()
    {
        isUnlocked = false;
        if (gateSprite != null)
            gateSprite.color = lockedColor;
    }

    private void SetUnlocked()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        if (gateSprite != null)
            gateSprite.color = unlockedColor;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isUnlocked) return;
        if (!S_PlayerLookup.IsPlayer(collision)) return;

        S_GameEvent.LevelExitRequested();
    }
}
