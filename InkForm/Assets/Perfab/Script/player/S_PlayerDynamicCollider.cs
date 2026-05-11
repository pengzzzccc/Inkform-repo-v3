using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class S_PlayerDynamicCollider : MonoBehaviour
{
    [Header("Dynamic Collider")]
    [SerializeField] private bool useDynamicCollider = true;
    [SerializeField] private bool restoreColliderOnDisable = true;
    [SerializeField] private bool keepSurfaceContact = true;

    [Header("Radius")]
    [SerializeField][Range(0.45f, 1f)] private float minRadiusScale = 0.68f;
    [SerializeField][Range(0.45f, 1f)] private float crouchRadiusScale = 0.72f;
    [SerializeField][Range(0.45f, 1f)] private float wallRadiusScale = 0.86f;
    [SerializeField][Range(0.45f, 1f)] private float ceilingRadiusScale = 0.82f;
    [SerializeField][Range(0f, 0.25f)] private float maxSpeedShrink = 0.08f;
    [SerializeField][Range(1f, 30f)] private float speedForMaxShrink = 14f;
    [SerializeField][Range(0f, 0.25f)] private float impactShrink = 0.08f;

    [Header("Offsets")]
    [SerializeField][Range(0f, 1f)] private float crouchInputThreshold = 0.45f;
    [SerializeField][Range(-0.3f, 0f)] private float crouchOffsetY = -0.06f;
    [SerializeField][Range(0f, 0.2f)] private float surfaceOffset = 0.04f;

    [Header("Smoothing")]
    [SerializeField][Range(1f, 30f)] private float shrinkSpeed = 12f;
    [SerializeField][Range(1f, 30f)] private float expandSpeed = 7f;
    [SerializeField][Range(1f, 30f)] private float offsetSpeed = 12f;
    [SerializeField][Range(1f, 20f)] private float impactRecoverSpeed = 6f;

    [Header("Gizmos")]
    [SerializeField] private bool drawDynamicColliderGizmos = true;
    [SerializeField] private Color currentColliderGizmoColor = new Color(1f, 0.85f, 0.15f, 0.8f);
    [SerializeField] private Color targetColliderGizmoColor = new Color(0.2f, 0.95f, 1f, 0.45f);

    private CircleCollider2D circleCollider;
    private Rigidbody2D rig;
    private float baseRadius;
    private Vector2 baseOffset;
    private float currentRadius;
    private Vector2 currentOffset;
    private float targetRadius;
    private Vector2 targetOffset;
    private Vector2 previousVelocity;
    private float impactPulse;
    private bool initialized;

    public void Initialize(CircleCollider2D circle, Rigidbody2D targetRig)
    {
        circleCollider = circle != null ? circle : GetComponent<CircleCollider2D>();
        rig = targetRig != null ? targetRig : GetComponent<Rigidbody2D>();

        baseRadius = Mathf.Max(0.01f, circleCollider.radius);
        baseOffset = circleCollider.offset;
        currentRadius = circleCollider.radius;
        currentOffset = circleCollider.offset;
        targetRadius = currentRadius;
        targetOffset = currentOffset;
        initialized = true;
    }

    public void DynamicColliderTick(S_Player player, S_fluid_climb climbSkill)
    {
        if (!useDynamicCollider)
        {
            RestoreCollider();
            return;
        }

        if (!initialized)
            Initialize(GetComponent<CircleCollider2D>(), GetComponent<Rigidbody2D>());

        if (player == null || circleCollider == null)
            return;

        Rigidbody2D playerRig = player.GetRigidbody();
        Vector2 velocity = playerRig != null ? playerRig.linearVelocity : Vector2.zero;
        S_fluid_climb.SurfaceType surface = climbSkill != null ? climbSkill.GetSurface() : S_fluid_climb.SurfaceType.None;
        Vector2 surfaceNormal = climbSkill != null ? climbSkill.GetSurfNormal() : Vector2.zero;
        Vector2 moveInput = player.GetMoveVector();

        UpdateImpactPulse(velocity, surface);
        CalculateTarget(player, surface, surfaceNormal, moveInput, velocity);
        ApplyTarget(surface, surfaceNormal);

        previousVelocity = velocity;
    }

    public void DrawDynamicColliderGizmos()
    {
        if (!drawDynamicColliderGizmos)
            return;

        CircleCollider2D circle = circleCollider != null ? circleCollider : GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        float scale = GetWorldRadiusScale();

        Gizmos.color = currentColliderGizmoColor;
        Gizmos.DrawWireSphere(transform.TransformPoint(circle.offset), circle.radius * scale);

        Gizmos.color = targetColliderGizmoColor;
        Gizmos.DrawWireSphere(transform.TransformPoint(targetOffset), targetRadius * scale);
    }

    private void OnDisable()
    {
        if (restoreColliderOnDisable)
            RestoreCollider();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDynamicColliderGizmos();
    }

    private void CalculateTarget(
        S_Player player,
        S_fluid_climb.SurfaceType surface,
        Vector2 surfaceNormal,
        Vector2 moveInput,
        Vector2 velocity)
    {
        float radiusScale = 1f;
        bool isFluid = player.getForm();
        bool isCrouching = isFluid && moveInput.y < -crouchInputThreshold;

        if (isCrouching)
            radiusScale = Mathf.Min(radiusScale, crouchRadiusScale);

        if (surface == S_fluid_climb.SurfaceType.WallLeft || surface == S_fluid_climb.SurfaceType.WallRight)
            radiusScale = Mathf.Min(radiusScale, wallRadiusScale);
        else if (surface == S_fluid_climb.SurfaceType.Ceiling)
            radiusScale = Mathf.Min(radiusScale, ceilingRadiusScale);

        float speedRate = Mathf.InverseLerp(0f, speedForMaxShrink, velocity.magnitude);
        radiusScale -= speedRate * maxSpeedShrink;
        radiusScale -= impactPulse * impactShrink;
        radiusScale = Mathf.Clamp(radiusScale, minRadiusScale, 1f);

        targetRadius = baseRadius * radiusScale;
        targetOffset = baseOffset;

        if (isCrouching)
            targetOffset.y += crouchOffsetY;

        if (surface != S_fluid_climb.SurfaceType.None && surfaceNormal.sqrMagnitude > 0.001f)
            targetOffset += -surfaceNormal.normalized * surfaceOffset;
    }

    private void ApplyTarget(S_fluid_climb.SurfaceType surface, Vector2 surfaceNormal)
    {
        float deltaTime = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : Time.deltaTime;
        float radiusSpeed = targetRadius < currentRadius ? shrinkSpeed : expandSpeed;
        float nextRadius = Mathf.MoveTowards(currentRadius, targetRadius, baseRadius * radiusSpeed * deltaTime);
        Vector2 nextOffset = Vector2.MoveTowards(currentOffset, targetOffset, baseRadius * offsetSpeed * deltaTime);

        if (keepSurfaceContact && rig != null)
        {
            Vector2 anchorNormal = GetAnchorNormal(surface, surfaceNormal);
            float radiusDelta = nextRadius - currentRadius;
            if (anchorNormal.sqrMagnitude > 0.001f && Mathf.Abs(radiusDelta) > 0.0001f)
                rig.position += anchorNormal * radiusDelta;
        }

        circleCollider.radius = nextRadius;
        circleCollider.offset = nextOffset;
        currentRadius = nextRadius;
        currentOffset = nextOffset;
    }

    private void UpdateImpactPulse(Vector2 velocity, S_fluid_climb.SurfaceType surface)
    {
        bool landed = previousVelocity.y < -4f && Mathf.Abs(velocity.y) < 1f && surface == S_fluid_climb.SurfaceType.Floor;
        bool hitWall = Mathf.Abs(previousVelocity.x) > 5f
            && Mathf.Abs(velocity.x) < 1f
            && (surface == S_fluid_climb.SurfaceType.WallLeft || surface == S_fluid_climb.SurfaceType.WallRight);

        if (landed)
            impactPulse = 1f;
        else if (hitWall)
            impactPulse = 0.75f;

        float deltaTime = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : Time.deltaTime;
        impactPulse = Mathf.MoveTowards(impactPulse, 0f, impactRecoverSpeed * deltaTime);
    }

    private Vector2 GetAnchorNormal(S_fluid_climb.SurfaceType surface, Vector2 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude > 0.001f)
            return surfaceNormal.normalized;

        switch (surface)
        {
            case S_fluid_climb.SurfaceType.Floor:
                return Vector2.up;
            case S_fluid_climb.SurfaceType.WallLeft:
                return Vector2.right;
            case S_fluid_climb.SurfaceType.WallRight:
                return Vector2.left;
            case S_fluid_climb.SurfaceType.Ceiling:
                return Vector2.down;
            default:
                return Vector2.zero;
        }
    }

    private float GetWorldRadiusScale()
    {
        Vector3 scale = transform.lossyScale;
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    private void RestoreCollider()
    {
        if (!initialized)
            return;

        if (circleCollider == null)
            return;

        circleCollider.radius = baseRadius;
        circleCollider.offset = baseOffset;
        currentRadius = baseRadius;
        currentOffset = baseOffset;
        targetRadius = baseRadius;
        targetOffset = baseOffset;
    }
}
