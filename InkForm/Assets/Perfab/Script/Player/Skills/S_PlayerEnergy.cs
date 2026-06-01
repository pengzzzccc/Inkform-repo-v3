using UnityEngine;

[DisallowMultipleComponent]
public class S_PlayerEnergy : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField, Min(1f)] private float maxEnergy = 100f;
    [SerializeField, Min(0f)] private float regenDelay = 1f;
    [SerializeField, Min(0f)] private float regenPerSecond = 20f;
    [SerializeField] private bool resetOnCheckpointRespawn = true;

    private float currentEnergy;
    private float timeSinceLastUse;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float NormalizedEnergy => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;

    private void Awake()
    {
        currentEnergy = maxEnergy;
        timeSinceLastUse = regenDelay;
    }

    private void Start()
    {
        BroadcastEnergyChanged();
    }

    private void OnEnable()
    {
        S_GameEvent.OnRespawnRequested += HandleRespawnRequested;
        S_GameEvent.OnRunStartRequested += ResetEnergy;
    }

    private void OnDisable()
    {
        S_GameEvent.OnRespawnRequested -= HandleRespawnRequested;
        S_GameEvent.OnRunStartRequested -= ResetEnergy;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        timeSinceLastUse += Time.deltaTime;
        if (timeSinceLastUse < regenDelay)
            return;

        if (currentEnergy >= maxEnergy)
            return;

        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + regenPerSecond * Time.deltaTime);
        BroadcastEnergyChanged();
    }

    public bool CanStartSkill(S_SkillBase skill)
    {
        if (skill == null)
            return true;

        return currentEnergy >= skill.MinEnergyToStart;
    }

    public bool TryConsumeSkillEnergy(S_SkillBase skill, float deltaTime)
    {
        if (skill == null)
            return true;

        return TryConsumeAmount(skill.EnergyDrainPerSecond * Mathf.Max(0f, deltaTime));
    }

    public bool TryConsumeAmount(float amount)
    {
        if (amount <= 0f)
            return true;

        timeSinceLastUse = 0f;
        currentEnergy = Mathf.Max(0f, currentEnergy - amount);
        BroadcastEnergyChanged();
        return currentEnergy > 0f;
    }

    public bool TrySpendAmount(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentEnergy < amount)
            return false;

        timeSinceLastUse = 0f;
        currentEnergy -= amount;
        BroadcastEnergyChanged();
        return true;
    }

    public void NotifySkillUseStopped()
    {
        timeSinceLastUse = 0f;
    }

    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;
        timeSinceLastUse = regenDelay;
        BroadcastEnergyChanged();
    }

    private void HandleRespawnRequested()
    {
        if (resetOnCheckpointRespawn)
            ResetEnergy();
    }

    private void BroadcastEnergyChanged()
    {
        S_GameEvent.PlayerEnergyChanged(currentEnergy, maxEnergy);
    }
}
