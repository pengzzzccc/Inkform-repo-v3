using UnityEngine;

/// <summary>
/// Tracks the suspicion meter throughout Chapter 2 (The Nurserie).
/// Fires OnSuspicionChanged on value updates and OnArrestTriggered at 100/100
/// or when all 3 story missions complete.
/// 
/// Thresholds (from Story_Outline):
///   0–33:   Normal — guards follow standard patrols
///   34–66:  Elevated — additional patrols, faster guards
///   67–99:  Critical — guards actively search, alarm pre-warning
///   100:    Arrest Event triggered — EMP storm
/// 
/// Usage: AddSuspicion(value) from level triggers, camera drones, or player actions.
/// </summary>
public class S_SuspicionSystem : MonoBehaviour
{
    public static S_SuspicionSystem Instance { get; private set; }

    /// <summary>True while player is hiding. Set by S_HideSpot, read by S_NPCEnemy.</summary>
    private static bool playerHidden;
    public static bool PlayerHidden
    {
        get => playerHidden;
        set
        {
            if (playerHidden == value) return;
            playerHidden = value;
            Debug.Log($"[S_SuspicionSystem] PlayerHidden → {value}");
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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        S_GameEvent.OnGameRestart += HandleGameRestart;
        S_GameEvent.OnAlertTriggered += HandleAlert;
    }

    private void OnDisable()
    {
        S_GameEvent.OnGameRestart -= HandleGameRestart;
        S_GameEvent.OnAlertTriggered -= HandleAlert;
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
        S_GameEvent.SuspicionChanged(currentSuspicion);

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
        S_GameEvent.SuspicionChanged(currentSuspicion);

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

    /// <summary>Called by S_HideSpot each frame while player is hidden.</summary>
    public void HideDecay()
    {
        if (arrestTriggered || hiddenDecayRate <= 0f) return;
        AddSuspicion(-hiddenDecayRate * Time.deltaTime);
    }

    private void HandleAlert(Transform npc)
    {
        AddSuspicion(increasePerAlert);
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
        currentSuspicion = 0f;
        completedMissions = 0;
        arrestTriggered = false;
        PlayerHidden = false;
        S_GameEvent.SuspicionChanged(0f);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw suspicion meter boundaries in Scene view for debugging
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
    }
}