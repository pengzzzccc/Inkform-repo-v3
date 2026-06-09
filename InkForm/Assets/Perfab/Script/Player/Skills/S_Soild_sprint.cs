using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Skill_Sprint", menuName = "InkForm/Skills/Sprint")]
public class S_Soild_sprint : S_SkillBase
{
    [FormerlySerializedAs("sprintForce")]
    [SerializeField] private float sprintSpeed = 20f;
    [SerializeField] private float cooldown = 1.0f;
    [SerializeField] private float SprintLockTime = 0.1f;

    [System.NonSerialized] private float lastUsedTime = -999f;

    [Header("Stun on Contact")]
    [SerializeField] private float stunRadius = 2f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Sprint Charge")]
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float maxSprintSpeed = 200f;
    [SerializeField] private float minSprintSpeed = 20f;
    [SerializeField] private float stage1Scale = 1f;
    [SerializeField] private float stage2Scale = 1.3f;
    [SerializeField] private float stage3Scale = 1.6f;
    [SerializeField] private float stage2Time = 0.5f;
    [SerializeField] private float stage3Time = 1.2f;
    [SerializeField] private float shakeFrequency = 25f;
    [SerializeField] private float shakeAmplitude = 0.15f;
    [SerializeField] private float shakeDecay = 5f;
    [SerializeField] private float stage1Cooldown = 0.1f;
    [SerializeField] private float stage2Cooldown = 0.5f;
    [SerializeField] private float stage3Cooldown = 1.0f;
    [SerializeField] private float bufferTime = 0.15f;
    [SerializeField] private PhysicsMaterial2D chargeBallMaterial;
    [SerializeField] private AudioClip chargeStartClip;
    [SerializeField] private AudioClip chargeStageClip;
    [SerializeField] private AudioClip chargeStage3Clip;
    [SerializeField] private AudioClip chargeReleaseClip;
    [SerializeField, Min(0f)] private float quickTapEnergyCost = 12f;

    public float MaxChargeTime => maxChargeTime;
    public float MaxSprintSpeed => maxSprintSpeed;
    public float MinSprintSpeed => minSprintSpeed;
    public float BufferTime => bufferTime;
    public PhysicsMaterial2D ChargeBallMaterial => chargeBallMaterial;
    public AudioClip ChargeStartClip => chargeStartClip;
    public float QuickTapEnergyCost => quickTapEnergyCost;
    public float LegacyCooldown => cooldown;

    public int GetStage(float timer)
    {
        if (timer >= stage3Time) return 2;
        if (timer >= stage2Time) return 1;
        return 0;
    }

    public float GetStageScale(float timer)
    {
        if (timer >= stage3Time) return stage3Scale;
        if (timer >= stage2Time) return stage2Scale;
        return stage1Scale;
    }

    public float GetCooldown(int stage)
    {
        if (stage >= 2) return stage3Cooldown;
        if (stage >= 1) return stage2Cooldown;
        return stage1Cooldown;
    }

    public AudioClip GetChargeStageClip(int stage)
    {
        if (stage >= 2 && chargeStage3Clip != null)
            return chargeStage3Clip;

        return chargeStageClip;
    }

    public float GetChargeStagePitch(int stage)
    {
        if (stage >= 2) return GetInversePitch(stage3Scale);
        if (stage >= 1) return GetInversePitch(stage2Scale);
        return GetInversePitch(stage1Scale);
    }

    public AudioClip GetChargeReleaseClip() => chargeReleaseClip;

    private float GetInversePitch(float scale)
    {
        return 1f / Mathf.Max(0.01f, scale);
    }

    public float GetShakeOffset(float shakeTimer)
    {
        return Mathf.Sin(shakeTimer * shakeFrequency) * shakeAmplitude * Mathf.Exp(-shakeDecay * shakeTimer);
    }

    public void ActivateCharge(S_Player player, float finalSpeed, float direction)
    {
        if (player.IsParalyzed) return;

        lastUsedTime = Time.time;

        player.GetRigidbody().AddForce(player.GravityRight * (direction * finalSpeed), ForceMode2D.Impulse);
        player.SetSprintMomentum(true);
        player.StartCoroutine(SprintLock(player));

        Vector2 center = player.GetBodyTransform().position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, stunRadius, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            S_NPCEnemy enemy = hit.GetComponent<S_NPCEnemy>();
            if (enemy == null)
                enemy = hit.GetComponentInParent<S_NPCEnemy>();

            if (enemy != null)
            {
                Debug.Log($"[Sprint Charge] Hit enemy: {enemy.NPCName}");
                enemy.OnSprintHit(player.GravityRight * direction);
            }
        }
    }

    public override void Activate(S_Player player)
    {
        if (player.IsParalyzed) return;

        if (!availableSolid && !player.getForm()) return;
        if (!availableFluid && player.getForm()) return;
        if (player.Energy != null && (!player.Energy.CanStartSkill(this) || !player.Energy.TrySpendAmount(quickTapEnergyCost)))
            return;

        lastUsedTime = Time.time;

        float dir = player.GetFaceRight() ? 1f : -1f;
        player.GetRigidbody().AddForce(player.GravityRight * (dir * sprintSpeed), ForceMode2D.Impulse);
        player.SetSprintMomentum(true);
        player.StartCoroutine(SprintLock(player));

        // Stun nearby guards
        Vector2 center = player.GetBodyTransform().position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, stunRadius, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            S_NPCEnemy enemy = hit.GetComponent<S_NPCEnemy>();
            if (enemy == null)
                enemy = hit.GetComponentInParent<S_NPCEnemy>();

            if (enemy != null)
            {
                Debug.Log($"[Sprint] Hit enemy: {enemy.NPCName}");
                enemy.OnSprintHit(player.GravityRight * dir);
            }
        }
    }

    private IEnumerator SprintLock(S_Player player)
    {
        player.SetSprinting(true);
        S_Player.form originalForm = player.getForm() ? S_Player.form.fluid : S_Player.form.solid;
        player.SetForm(S_Player.form.solid);
        yield return new WaitForSeconds(SprintLockTime);
        player.SetForm(originalForm);
        player.SetSprinting(false);
        player.SetSprintMomentum(false);
    }

    public override void OnUnlocked(S_Player player)
    {
        Debug.Log($"[SkillTree] unlocked skill {skillName}");
    }
}
