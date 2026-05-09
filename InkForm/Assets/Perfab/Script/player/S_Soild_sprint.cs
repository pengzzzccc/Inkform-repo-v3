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

    public override void Activate(S_Player player)
    {
        if (player.IsParalyzed) return;

        if (Time.time - lastUsedTime < cooldown)
        {
            Debug.Log($"[Sprint] cooling down, {cooldown - (Time.time - lastUsedTime):F1}s remaining");
            return;
        }

        if (!availableSolid && !player.getForm()) return;
        if (!availableFluid && player.getForm()) return;

        lastUsedTime = Time.time;

        float dir = player.GetFaceRight() ? 1f : -1f;
        player.GetRigidbody().AddForce(new Vector2(dir * sprintSpeed, 0), ForceMode2D.Impulse);
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
                enemy.OnSprintHit(new Vector2(dir, 0f));
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
