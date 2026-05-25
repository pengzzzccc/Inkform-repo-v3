using System.Collections.Generic;
using UnityEngine;

public class S_SkillTree : MonoBehaviour
{
    public static S_SkillTree Instance { get; private set; }

    [Header("Skill list")]
    [SerializeField] private S_SkillBase[] allSkills;

    [Header("init skill point")]
    [SerializeField] private int skillPoints = 0;

    [Header("Default Runtime Unlocks")]
    [SerializeField] private bool autoUnlockDefaultSkills = true;
    [SerializeField] private string[] defaultUnlockedSkills = { "Sprint", "FluidClimb", "CameraControl" };

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
            if (autoUnlockDefaultSkills)
                UnlockDefaultSkills();
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
        return unlockedMap.TryGetValue("Sprint", out S_SkillBase skill)
            ? skill as S_Soild_sprint
            : null;
    }

    public S_CameraControlSkill GetCameraControlSkill()
    {
        return unlockedMap.TryGetValue("CameraControl", out S_SkillBase skill)
            ? skill as S_CameraControlSkill
            : null;
    }

    public void ApplyTutorialSkillSet(string[] skillNames)
    {
        ResetUnlockedSkills();

        if (skillNames == null)
            return;

        foreach (string skillName in skillNames)
        {
            if (!string.IsNullOrWhiteSpace(skillName))
                UnlockSkillDirect(skillName.Trim(), true);
        }
    }

    public void RestoreDefaultSkillSet()
    {
        ResetUnlockedSkills();

        if (autoUnlockDefaultSkills)
            UnlockDefaultSkills();
    }

    public void ResetUnlockedSkills()
    {
        if (allSkills != null)
        {
            foreach (S_SkillBase skill in allSkills)
            {
                if (skill != null)
                    skill.isUnlocked = false;
            }
        }

        unlockedMap.Clear();
    }

    private void UnlockDefaultSkills()
    {
        if (defaultUnlockedSkills == null)
            return;

        foreach (string skillName in defaultUnlockedSkills)
        {
            if (string.IsNullOrWhiteSpace(skillName))
                continue;

            string trimmedName = skillName.Trim();
            UnlockSkillDirect(trimmedName, false);
        }
    }

    private bool UnlockSkillDirect(string skillName, bool logWarnings)
    {
        if (unlockedMap.ContainsKey(skillName))
            return true;

        S_SkillBase skill = FindSkill(skillName);
        if (skill == null)
        {
            if (skillName == "CameraControl")
            {
                skill = ScriptableObject.CreateInstance<S_CameraControlSkill>();
                skill.skillName = "CameraControl";
                skill.requiredPoints = 0;
            }
            else
            {
                if (logWarnings)
                    Debug.LogWarning($"[SkillTree] can't find skill: {skillName}");

                return false;
            }
        }

        skill.isUnlocked = true;
        unlockedMap[skill.skillName] = skill;
        skill.OnUnlocked(S_Player.Instance);
        return true;
    }

    private S_SkillBase FindSkill(string name)
    {
        if (allSkills == null)
            return null;

        foreach (var s in allSkills)
            if (s != null && s.skillName == name) return s;
        return null;
    }
}
