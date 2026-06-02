using UnityEngine;

/// <summary>
/// Grappling / tentacle hook skill (fluid form only). Detects nearby hook anchors,
/// shoots a procedural tentacle to the chosen one, and lets the player swing like a
/// pendulum whose rope steadily shortens (the player rises toward the hook and auto-
/// detaches at the top). The actual state machine lives in S_PlayerSkillController;
/// this ScriptableObject only holds the tunables (mirrors how Sprint/CameraControl work).
/// </summary>
[CreateAssetMenu(fileName = "Skill_Hook", menuName = "InkForm/Skills/Hook")]
public class S_HookSkill : S_SkillBase
{
    [Header("Detection")]
    [Tooltip("Radius around the player to search for hook anchors.")]
    [SerializeField, Min(0.5f)] private float detectionRadius = 6f;

    [Header("Rope")]
    [Tooltip("Longest rope allowed; the attach distance is clamped to this.")]
    [SerializeField, Min(0.5f)] private float maxRopeLength = 8f;
    [Tooltip("When the rope shrinks to this length the player auto-detaches (reached the hook).")]
    [SerializeField, Min(0.1f)] private float minRopeLength = 0.6f;
    [Tooltip("How fast the rope shortens per second (player rises toward the hook).")]
    [SerializeField, Min(0f)] private float riseSpeed = 1.5f;

    [Header("Swing")]
    [Tooltip("Tangential acceleration added by the left/right move keys while swinging.")]
    [SerializeField, Min(0f)] private float swingAccel = 40f;
    [Tooltip("Maximum swing speed (clamps the rigidbody velocity while hooked).")]
    [SerializeField, Min(0f)] private float maxSwingSpeed = 18f;

    [Header("Release")]
    [Tooltip("Extra upward impulse added when releasing with the jump key.")]
    [SerializeField, Min(0f)] private float jumpOffBoost = 4f;

    public float DetectionRadius => detectionRadius;
    public float MaxRopeLength => maxRopeLength;
    public float MinRopeLength => minRopeLength;
    public float RiseSpeed => riseSpeed;
    public float SwingAccel => swingAccel;
    public float MaxSwingSpeed => maxSwingSpeed;
    public float JumpOffBoost => jumpOffBoost;

    // The skill is driven entirely by S_PlayerSkillController; nothing to do on Activate.
    public override void Activate(S_Player player) { }
}
