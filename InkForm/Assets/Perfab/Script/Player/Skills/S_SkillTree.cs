using System.Collections.Generic;
using UnityEngine;

public class S_SkillTree : MonoBehaviour
{
    public static S_SkillTree Instance { get; private set; }

    [Header("Skill list")]
    [SerializeField] private S_SkillBase[] allSkills;

    [Header("init skill point")]
    [SerializeField] private int skillPoints = 0;

    private Dictionary<string, S_SkillBase> unlockedMap = new();
    private bool initialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        Instance = this;

        if (!initialized)
        {
            initialized = true;
            AddSkillPoints(5);
            TryUnlock("Sprint");
            TryUnlock("FluidClimb");
            EnsureCameraControlSkill();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddSkillPoints(int amount)
    {
        skillPoints += amount;
        Debug.Log($"[SkillTree] get {amount} point, current {skillPoints} point in total");
    }

    public bool TryUnlock(string skillName)
    {
        S_SkillBase skill = FindSkill(skillName);
        if (skill == null)
        {
            Debug.LogWarning($"[SkillTree] can't find skill: {skillName}");
            return false;
        }
        if (skill.isUnlocked)
        {
            Debug.Log($"[SkillTree] {skillName} is already unlocked");
            return false;
        }
        if (!skill.CanUnlock())
        {
            Debug.Log($"[SkillTree] {skillName} prerequisite conditions not met");
            return false;
        }
        if (skillPoints < skill.requiredPoints)
        {
            Debug.Log($"[SkillTree] Insufficient points (required {skill.requiredPoints}, current {skillPoints})");
            return false;
        }

        skillPoints -= skill.requiredPoints;
        skill.isUnlocked = true;
        unlockedMap[skill.skillName] = skill;
        skill.OnUnlocked(S_Player.Instance);

        Debug.Log($"[SkillTree] Unlocked successfully: {skillName}, Remaining points {skillPoints}");
        return true;
    }

    public void ActivateSkill(string skillName)
    {
        if (unlockedMap.TryGetValue(skillName, out S_SkillBase skill))
        {
            skill.Activate(S_Player.Instance);
        }
        else
        {
            Debug.Log($"[SkillTree] {skillName} is not unlocked, cannot activate");
        }
    }

    public bool IsUnlocked(string skillName)
    {
        return unlockedMap.ContainsKey(skillName);
    }

    public int GetSkillPoints() => skillPoints;

    public S_Soild_sprint GetSprintSkill()
    {
        S_SkillBase skill = FindSkill("Sprint");
        return skill as S_Soild_sprint;
    }

    public S_CameraControlSkill GetCameraControlSkill()
    {
        EnsureCameraControlSkill();
        return unlockedMap.TryGetValue("CameraControl", out S_SkillBase skill)
            ? skill as S_CameraControlSkill
            : null;
    }

    private void EnsureCameraControlSkill()
    {
        if (unlockedMap.ContainsKey("CameraControl"))
            return;

        S_CameraControlSkill skill = FindSkill("CameraControl") as S_CameraControlSkill;
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<S_CameraControlSkill>();
            skill.skillName = "CameraControl";
            skill.requiredPoints = 0;
        }

        skill.isUnlocked = true;
        unlockedMap[skill.skillName] = skill;
        skill.OnUnlocked(S_Player.Instance);
    }

    private S_SkillBase FindSkill(string name)
    {
        foreach (var s in allSkills)
            if (s != null && s.skillName == name) return s;
        return null;
    }
}
