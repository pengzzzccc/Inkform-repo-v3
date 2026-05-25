using UnityEngine;

/// <summary>
/// Tracks suspicion and player hidden state for stealth gameplay.
/// New gameplay callers should request changes through S_GameEvent.
/// </summary>
public class S_SuspicionSystem : MonoBehaviour
{
    public static S_SuspicionSystem Instance { get; private set; }

    /// <summary>Compatibility state for older scripts. Prefer S_GameEvent hidden events for new code.</summary>
    private static bool playerHidden;
    public static bool PlayerHidden
    {
        get => playerHidden;
        set
        {
            if (Instance != null)
            {
                Instance.SetPlayerHidden(value);
                return;
            }

            SetPlayerHiddenValue(value);
        }
    }

    [Header("Suspicion Settings")]
    [SerializeField] private float maxSuspicion = 100f;
    [SerializeField] private float decayRate = 0f; // No decay by default; set > 0 for timed drain
    [SerializeField] private bool decayOnlyInSafeZone = true;
    [SerializeField] private float increasePerAlert = 20f;
    [SerializeField] private float hiddenDecayRate = 5f;

    [Header("Mission Completion")]
    [SerializeField] private int missionsToTriggerArrest = 3;

    private float currentSuspicion = 0f;
    private int completedMissions = 0;
    private bool arrestTriggered = false;

    public float CurrentSuspicion => currentSuspicion;
    public float SuspicionPercent => currentSuspicion / maxSuspicion;
    public float MaxSuspicion => maxSuspicion;
    public float LegacyIncreasePerAlert => increasePerAlert;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        S_GameEvent.OnGameRestart += HandleGameRestart;
        S_GameEvent.OnSuspicionResetRequested += HandleSuspicionResetRequested;
        S_GameEvent.OnSuspicionChangeRequested += HandleSuspicionChangeRequested;
        S_GameEvent.OnHiddenSuspicionDecayRequested += HandleHiddenSuspicionDecayRequested;
        S_GameEvent.OnPlayerHiddenChangeRequested += HandlePlayerHiddenChangeRequested;
    }

    private void OnDisable()
    {
        S_GameEvent.OnGameRestart -= HandleGameRestart;
        S_GameEvent.OnSuspicionResetRequested -= HandleSuspicionResetRequested;
        S_GameEvent.OnSuspicionChangeRequested -= HandleSuspicionChangeRequested;
        S_GameEvent.OnHiddenSuspicionDecayRequested -= HandleHiddenSuspicionDecayRequested;
        S_GameEvent.OnPlayerHiddenChangeRequested -= HandlePlayerHiddenChangeRequested;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (arrestTriggered) return;

        if (decayRate > 0f && !decayOnlyInSafeZone)
        {
            AddSuspicion(-decayRate * Time.deltaTime);
        }
    }

    /// <summary>Add (or subtract, for decay) from the suspicion meter.</summary>
    public void AddSuspicion(float amount)
    {
        if (arrestTriggered) return;

        currentSuspicion = Mathf.Clamp(currentSuspicion + amount, 0f, maxSuspicion);
        BroadcastSuspicionChanged();

        if (currentSuspicion >= maxSuspicion)
        {
            TriggerArrest("suspicion_at_max");
        }
    }

    /// <summary>Set suspicion to an exact value (0 to maxSuspicion). Clamped.</summary>
    public void SetSuspicion(float value)
    {
        if (arrestTriggered) return;

        currentSuspicion = Mathf.Clamp(value, 0f, maxSuspicion);
        BroadcastSuspicionChanged();

        if (currentSuspicion >= maxSuspicion)
        {
            TriggerArrest("suspicion_set_to_max");
        }
    }

    /// <summary>Call when a story mission is completed.</summary>
    public void CompleteMission()
    {
        if (arrestTriggered) return;

        completedMissions++;
        if (completedMissions >= missionsToTriggerArrest)
        {
            TriggerArrest("all_missions_complete");
        }
    }

    /// <summary>Decay suspicion while player is in a safe zone. Call from safe zone trigger.</summary>
    public void SafeZoneDecay()
    {
        if (arrestTriggered || decayRate <= 0f) return;
        AddSuspicion(-decayRate * Time.deltaTime);
    }

    /// <summary>Legacy direct call. New code should use S_GameEvent.HiddenSuspicionDecayRequested.</summary>
    public void HideDecay()
    {
        HandleHiddenSuspicionDecayRequested(Time.deltaTime);
    }

    private void HandleSuspicionChangeRequested(float amount, Transform source)
    {
        AddSuspicion(amount);
    }

    private void HandleHiddenSuspicionDecayRequested(float deltaTime)
    {
        if (arrestTriggered || hiddenDecayRate <= 0f || deltaTime <= 0f) return;
        AddSuspicion(-hiddenDecayRate * deltaTime);
    }

    private void HandlePlayerHiddenChangeRequested(bool hidden)
    {
        SetPlayerHidden(hidden);
    }

    private void TriggerArrest(string reason)
    {
        if (arrestTriggered) return;
        arrestTriggered = true;
        Debug.Log($"[S_SuspicionSystem] Arrest triggered: {reason}");
        S_GameEvent.ArrestTriggered();
    }

    private void HandleGameRestart()
    {
        ResetSuspicionState();
    }

    private void HandleSuspicionResetRequested()
    {
        ResetSuspicionState();
    }

    private void ResetSuspicionState()
    {
        currentSuspicion = 0f;
        completedMissions = 0;
        arrestTriggered = false;
        SetPlayerHidden(false);
        BroadcastSuspicionChanged();
    }

    private void SetPlayerHidden(bool hidden)
    {
        SetPlayerHiddenValue(hidden);
    }

    private static void SetPlayerHiddenValue(bool hidden)
    {
        if (playerHidden == hidden) return;

        playerHidden = hidden;
        Debug.Log($"[S_SuspicionSystem] PlayerHidden -> {hidden}");
        S_GameEvent.PlayerHiddenChanged(hidden);
    }

    private void BroadcastSuspicionChanged()
    {
        S_GameEvent.SuspicionChanged(currentSuspicion);
        S_GameEvent.SuspicionValueChanged(currentSuspicion, maxSuspicion);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
    }
}
