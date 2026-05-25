using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "InkForm/Skill")]
public abstract class S_SkillBase : ScriptableObject
{
    [Header("Basics info")]
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("unlock condition")]
    public int requiredPoints = 1;
    public S_SkillBase[] prerequisites;

    [Header("form state")]
    public bool availableSolid = true;
    public bool availableFluid = false;

    [Header("Energy")]
    [SerializeField, Min(0f)] private float minEnergyToStart = 10f;
    [SerializeField, Min(0f)] private float energyDrainPerSecond = 20f;

    [System.NonSerialized] public bool isUnlocked = false;

    public float MinEnergyToStart => minEnergyToStart;
    public float EnergyDrainPerSecond => energyDrainPerSecond;

    public bool CanUnlock()
    {
        foreach (var pre in prerequisites)
        {
            if (pre != null && !pre.isUnlocked)
                return false;
        }
        return true;
    }

    public abstract void Activate(S_Player player);

    public virtual void OnUnlocked(S_Player player) { }
}
