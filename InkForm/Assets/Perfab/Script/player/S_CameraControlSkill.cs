using UnityEngine;

[CreateAssetMenu(fileName = "Skill_CameraControl", menuName = "InkForm/Skills/CameraControl")]
public class S_CameraControlSkill : S_SkillBase
{
    [Header("Bullet Time")]
    [SerializeField, Range(0.01f, 1f)] private float bulletTimeScale = 0.2f;

    public float BulletTimeScale => Mathf.Clamp(bulletTimeScale, 0.01f, 1f);

    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(skillName))
            skillName = "CameraControl";

        availableSolid = true;
        availableFluid = true;
    }

    public override void Activate(S_Player player)
    {
        if (player == null)
            return;

        player.BeginCameraControl(this);
    }

    public override void OnUnlocked(S_Player player)
    {
        Debug.Log($"[SkillTree] unlocked skill {skillName}");
    }
}
