using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class S_PlayerDynamicCollider : MonoBehaviour
{
    private enum ColliderMode
    {
        Circle,
        CrouchCapsule,
        WallCapsule,
        CeilingCapsule,
    }

    [Header("Dynamic Collider")]
    [SerializeField] private bool useDynamicCollider = true;
    [SerializeField] private bool useCapsuleCollider = true;
    [SerializeField] private bool restoreColliderOnDisable = true;
    [SerializeField] private bool keepSurfaceContact = true;

    [Header("Circle Radius Fallback")]
    [SerializeField][Range(0.45f, 1f)] private float minRadiusScale = 0.68f;
    [SerializeField][Range(0.45f, 1f)] private float crouchRadiusScale = 0.72f;
    [SerializeField][Range(0.45f, 1f)] private float wallRadiusScale = 0.86f;
    [SerializeField][Range(0.45f, 1f)] private float ceilingRadiusScale = 0.82f;
    [SerializeField][Range(0f, 0.25f)] private float maxSpeedShrink = 0.08f;
    [SerializeField][Range(1f, 30f)] private float speedForMaxShrink = 14f;
    [SerializeField][Range(0f, 0.25f)] private float impactShrink = 0.08f;

    [Header("Capsule Shape")]
    [SerializeField][Range(0.7f, 1.6f)] private float crouchCapsuleWidthScale = 1.12f;
    [SerializeField][Range(0.3f, 1f)] private float crouchCapsuleHeightScale = 0.56f;
    [SerializeField][Range(0.3f, 1f)] private float wallCapsuleWidthScale = 0.58f;
    [SerializeField][Range(0.7f, 1.6f)] private float wallCapsuleHeightScale = 1.14f;
    [SerializeField][Range(0.7f, 1.6f)] private float ceilingCapsuleWidthScale = 1.16f;
    [SerializeField][Range(0.3f, 1f)] private float ceilingCapsuleHeightScale = 0.5f;
    [SerializeField][Range(0.05f, 0.5f)] private float minCapsuleThickness = 0.22f;
    [SerializeField][Range(0f, 0.4f)] private float capsuleSpeedFlatten = 0.08f;
    [SerializeField][Range(0f, 0.4f)] private float capsuleImpactFlatten = 0.12f;

    [Header("Offsets")]
    [SerializeField][Range(0f, 1f)] private float crouchInputThreshold = 0.45f;
    [SerializeField][Range(0f, 1f)] private float crouchReleaseThreshold = 0.18f;
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
    private CapsuleCollider2D capsuleCollider;
    private Rigidbody2D rig;
    private float baseRadius;
    private Vector2 baseOffset;
    private Vector2 baseCapsuleSize;
    private float currentRadius;
    private Vector2 currentOffset;
    private Vector2 currentCapsuleSize;
    private Vector2 currentCapsuleOffset;
    private float targetRadius;
    private Vector2 targetOffset;
    private Vector2 targetCapsuleSize;
    private Vector2 targetCapsuleOffset;
    private CapsuleDirection2D targetCapsuleDirection = CapsuleDirection2D.Vertical;
    private ColliderMode currentMode = ColliderMode.Circle;
    private ColliderMode targetMode = ColliderMode.Circle;
    private Vector2 previousVelocity;
    private float impactPulse;
    private bool crouchInputHeld;
    private bool initialized;

    public Collider2D GetActiveCollider()
    {
        if (!useDynamicCollider)
            return circleCollider;

        if (useCapsuleCollider && capsuleCollider != null && capsuleCollider.enabled)
            return capsuleCollider;

        return circleCollider;
    }

    public void Initialize(CircleCollider2D circle, Rigidbody2D targetRig)
    {
        circleCollider = circle != null ? circle : GetComponent<CircleCollider2D>();
        rig = targetRig != null ? targetRig : GetComponent<Rigidbody2D>();

        if (circleCollider == null)
            return;

        baseRadius = Mathf.Max(0.01f, circleCollider.radius);
        baseOffset = circleCollider.offset;
        currentRadius = circleCollider.radius;
        currentOffset = circleCollider.offset;
        targetRadius = currentRadius;
        targetOffset = currentOffset;
        baseCapsuleSize = Vector2.one * baseRadius * 2f;

        capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (useCapsuleCollider)
            EnsureCapsuleCollider();

        SetCircleActive(true);
        SetCapsuleActive(false);
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

        if (useCapsuleCollider && capsuleCollider == null)
            EnsureCapsuleCollider();

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

        float radius = initialized ? currentRadius : circle.radius;
        Vector2 offset = initialized ? currentOffset : circle.offset;

        Gizmos.color = currentColliderGizmoColor;
        if (capsuleCollider != null && capsuleCollider.enabled)
            DrawCapsuleGizmo(capsuleCollider.offset, capsuleCollider.size, capsuleCollider.direction);
        else
            Gizmos.DrawWireSphere(transform.TransformPoint(offset), radius * GetWorldRadiusScale());

        Gizmos.color = targetColliderGizmoColor;
        if (initialized && targetMode != ColliderMode.Circle && useCapsuleCollider)
            DrawCapsuleGizmo(targetCapsuleOffset, targetCapsuleSize, targetCapsuleDirection);
        else
            Gizmos.DrawWireSphere(transform.TransformPoint(initialized ? targetOffset : circle.offset), (initialized ? targetRadius : circle.radius) * GetWorldRadiusScale());
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
        bool isFluid = player.getForm();
        bool isCrouching = UpdateCrouchInput(isFluid, moveInput.y);
        targetMode = GetTargetMode(surface, isCrouching);

        if (targetMode == ColliderMode.Circle || !useCapsuleCollider)
            CalculateCircleTarget(isCrouching, surface, surfaceNormal, velocity);
        else
            CalculateCapsuleTarget(surface, surfaceNormal, velocity);
    }

    private ColliderMode GetTargetMode(S_fluid_climb.SurfaceType surface, bool isCrouching)
    {
        if (!useCapsuleCollider)
            return ColliderMode.Circle;

        if (surface == S_fluid_climb.SurfaceType.WallLeft || surface == S_fluid_climb.SurfaceType.WallRight)
            return ColliderMode.WallCapsule;

        if (surface == S_fluid_climb.SurfaceType.Ceiling)
            return ColliderMode.CeilingCapsule;

        if (isCrouching)
            return ColliderMode.CrouchCapsule;

        return ColliderMode.Circle;
    }

    private bool UpdateCrouchInput(bool isFluid, float verticalInput)
    {
        if (!isFluid)
        {
            crouchInputHeld = false;
            return false;
        }

        if (verticalInput < -crouchInputThreshold)
            crouchInputHeld = true;
        else if (verticalInput > -crouchReleaseThreshold)
            crouchInputHeld = false;

        return crouchInputHeld;
    }

    private void CalculateCircleTarget(
        bool isCrouching,
        S_fluid_climb.SurfaceType surface,
        Vector2 surfaceNormal,
        Vector2 velocity)
    {
        float radiusScale = 1f;

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

    private void CalculateCapsuleTarget(
        S_fluid_climb.SurfaceType surface,
        Vector2 surfaceNormal,
        Vector2 velocity)
    {
        targetRadius = baseRadius;
        targetOffset = baseOffset;
        targetCapsuleDirection = GetCapsuleDirection(targetMode);
        targetCapsuleSize = GetCapsuleBaseSize(targetMode);

        float speedRate = Mathf.InverseLerp(0f, speedForMaxShrink, velocity.magnitude);
        float flatten = speedRate * capsuleSpeedFlatten + impactPulse * capsuleImpactFlatten;
        targetCapsuleSize = ApplyCapsuleFlatten(targetCapsuleSize, targetCapsuleDirection, flatten);
        targetCapsuleOffset = GetAnchoredCapsuleOffset(targetMode, targetCapsuleSize, surfaceNormal);

        if (ShouldApplySurfaceOffset(surface, surfaceNormal))
            targetCapsuleOffset += -surfaceNormal.normalized * surfaceOffset;
    }

    private bool ShouldApplySurfaceOffset(S_fluid_climb.SurfaceType surface, Vector2 surfaceNormal)
    {
        if (surface == S_fluid_climb.SurfaceType.None || surfaceNormal.sqrMagnitude <= 0.001f)
            return false;

        return targetMode != ColliderMode.CrouchCapsule || surface != S_fluid_climb.SurfaceType.Floor;
    }

    private Vector2 GetCapsuleBaseSize(ColliderMode mode)
    {
        float diameter = baseRadius * 2f;
        switch (mode)
        {
            case ColliderMode.CrouchCapsule:
                return new Vector2(diameter * crouchCapsuleWidthScale, diameter * crouchCapsuleHeightScale);
            case ColliderMode.WallCapsule:
                return new Vector2(diameter * wallCapsuleWidthScale, diameter * wallCapsuleHeightScale);
            case ColliderMode.CeilingCapsule:
                return new Vector2(diameter * ceilingCapsuleWidthScale, diameter * ceilingCapsuleHeightScale);
            default:
                return Vector2.one * diameter;
        }
    }

    private Vector2 GetAnchoredCapsuleOffset(ColliderMode mode, Vector2 size, Vector2 surfaceNormal)
    {
        Vector2 offset = baseOffset;

        switch (mode)
        {
            case ColliderMode.CrouchCapsule:
                offset.y = baseOffset.y - baseRadius + size.y * 0.5f;
                break;
            case ColliderMode.CeilingCapsule:
                offset.y = baseOffset.y + baseRadius - size.y * 0.5f;
                break;
            case ColliderMode.WallCapsule:
                if (surfaceNormal.x > 0.001f)
                    offset.x = baseOffset.x - baseRadius + size.x * 0.5f;
                else if (surfaceNormal.x < -0.001f)
                    offset.x = baseOffset.x + baseRadius - size.x * 0.5f;
                break;
        }

        return offset;
    }

    private CapsuleDirection2D GetCapsuleDirection(ColliderMode mode)
    {
        return mode == ColliderMode.WallCapsule
            ? CapsuleDirection2D.Vertical
            : CapsuleDirection2D.Horizontal;
    }

    private Vector2 ApplyCapsuleFlatten(Vector2 size, CapsuleDirection2D direction, float amount)
    {
        float flatten = Mathf.Clamp01(amount);
        if (direction == CapsuleDirection2D.Horizontal)
        {
            size.x *= 1f + flatten * 0.25f;
            size.y *= 1f - flatten;
        }
        else
        {
            size.x *= 1f - flatten;
            size.y *= 1f + flatten * 0.25f;
        }

        size.x = Mathf.Max(size.x, minCapsuleThickness);
        size.y = Mathf.Max(size.y, minCapsuleThickness);
        return size;
    }

    private void ApplyTarget(S_fluid_climb.SurfaceType surface, Vector2 surfaceNormal)
    {
        if (targetMode == ColliderMode.Circle || !useCapsuleCollider || capsuleCollider == null)
        {
            ApplyCircleTarget(surface, surfaceNormal);
            return;
        }

        ApplyCapsuleTarget(surface, surfaceNormal);
    }

    private void ApplyCircleTarget(S_fluid_climb.SurfaceType surface, Vector2 surfaceNormal)
    {
        SetCircleActive(true);
        SetCapsuleActive(false);
        currentMode = ColliderMode.Circle;

        float deltaTime = GetPhysicsDeltaTime();
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

    private void ApplyCapsuleTarget(S_fluid_climb.SurfaceType surface, Vector2 surfaceNormal)
    {
        if (capsuleCollider == null)
            return;

        if (currentMode == ColliderMode.Circle || !capsuleCollider.enabled)
        {
            currentCapsuleSize = GetCapsuleEntrySize(targetCapsuleSize, currentRadius);
            currentCapsuleOffset = GetCapsuleEntryOffset(targetMode, currentCapsuleSize, surfaceNormal);
        }

        SetCapsuleActive(true);
        SetCircleActive(false);
        currentMode = targetMode;

        float deltaTime = GetPhysicsDeltaTime();
        Vector2 nextSize = MoveSizeTowards(currentCapsuleSize, targetCapsuleSize, deltaTime);
        Vector2 nextOffset = Vector2.MoveTowards(currentCapsuleOffset, targetCapsuleOffset, baseRadius * offsetSpeed * deltaTime);

        if (keepSurfaceContact && rig != null && targetMode != ColliderMode.CrouchCapsule)
        {
            Vector2 anchorNormal = GetAnchorNormal(surface, surfaceNormal);
            Vector2 sizeDelta = nextSize - currentCapsuleSize;
            float anchorDelta = GetAnchorSizeDelta(anchorNormal, sizeDelta);
            if (anchorNormal.sqrMagnitude > 0.001f && Mathf.Abs(anchorDelta) > 0.0001f)
                rig.position += anchorNormal * anchorDelta * 0.5f;
        }

        capsuleCollider.direction = targetCapsuleDirection;
        capsuleCollider.size = nextSize;
        capsuleCollider.offset = nextOffset;
        currentCapsuleSize = nextSize;
        currentCapsuleOffset = nextOffset;
    }

    private Vector2 GetCapsuleEntrySize(Vector2 desiredSize, float sourceRadius)
    {
        float sourceDiameter = Mathf.Max(minCapsuleThickness, sourceRadius * 2f);
        return new Vector2(
            Mathf.Min(desiredSize.x, sourceDiameter),
            Mathf.Min(desiredSize.y, sourceDiameter));
    }

    private Vector2 GetCapsuleEntryOffset(ColliderMode mode, Vector2 size, Vector2 surfaceNormal)
    {
        Vector2 offset = GetAnchoredCapsuleOffset(mode, size, surfaceNormal);
        if (mode == ColliderMode.Circle)
            offset = currentOffset;

        return offset;
    }

    private Vector2 MoveSizeTowards(Vector2 currentSize, Vector2 desiredSize, float deltaTime)
    {
        float xSpeed = desiredSize.x < currentSize.x ? shrinkSpeed : expandSpeed;
        float ySpeed = desiredSize.y < currentSize.y ? shrinkSpeed : expandSpeed;
        float diameter = Mathf.Max(baseRadius * 2f, minCapsuleThickness);

        return new Vector2(
            Mathf.MoveTowards(currentSize.x, desiredSize.x, diameter * xSpeed * deltaTime),
            Mathf.MoveTowards(currentSize.y, desiredSize.y, diameter * ySpeed * deltaTime));
    }

    private float GetAnchorSizeDelta(Vector2 anchorNormal, Vector2 sizeDelta)
    {
        if (anchorNormal.sqrMagnitude <= 0.001f)
            return 0f;

        Vector2 normal = anchorNormal.normalized;
        return Mathf.Abs(normal.x) > Mathf.Abs(normal.y) ? sizeDelta.x : sizeDelta.y;
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

        impactPulse = Mathf.MoveTowards(impactPulse, 0f, impactRecoverSpeed * GetPhysicsDeltaTime());
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

    private void EnsureCapsuleCollider()
    {
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider2D>();

        if (capsuleCollider == null)
            capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();

        capsuleCollider.sharedMaterial = circleCollider.sharedMaterial;
        capsuleCollider.isTrigger = circleCollider.isTrigger;
        capsuleCollider.direction = CapsuleDirection2D.Vertical;
        capsuleCollider.size = baseCapsuleSize;
        capsuleCollider.offset = baseOffset;

        currentCapsuleSize = capsuleCollider.size;
        currentCapsuleOffset = capsuleCollider.offset;
        targetCapsuleSize = currentCapsuleSize;
        targetCapsuleOffset = currentCapsuleOffset;
    }

    private void SetCircleActive(bool active)
    {
        if (circleCollider != null && circleCollider.enabled != active)
            circleCollider.enabled = active;
    }

    private void SetCapsuleActive(bool active)
    {
        if (capsuleCollider != null && capsuleCollider.enabled != active)
            capsuleCollider.enabled = active;
    }

    private float GetPhysicsDeltaTime()
    {
        return Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : Time.deltaTime;
    }

    private float GetWorldRadiusScale()
    {
        Vector3 scale = transform.lossyScale;
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    private void DrawCapsuleGizmo(Vector2 localOffset, Vector2 localSize, CapsuleDirection2D direction)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 center = transform.TransformPoint(localOffset);
        float width = Mathf.Abs(localSize.x * scale.x);
        float height = Mathf.Abs(localSize.y * scale.y);

        if (direction == CapsuleDirection2D.Horizontal)
        {
            float radius = height * 0.5f;
            float halfSegment = Mathf.Max(0f, (width - height) * 0.5f);
            Vector3 right = transform.right * halfSegment;
            Vector3 up = transform.up * radius;
            Gizmos.DrawWireSphere(center - right, radius);
            Gizmos.DrawWireSphere(center + right, radius);
            Gizmos.DrawLine(center - right + up, center + right + up);
            Gizmos.DrawLine(center - right - up, center + right - up);
        }
        else
        {
            float radius = width * 0.5f;
            float halfSegment = Mathf.Max(0f, (height - width) * 0.5f);
            Vector3 up = transform.up * halfSegment;
            Vector3 right = transform.right * radius;
            Gizmos.DrawWireSphere(center - up, radius);
            Gizmos.DrawWireSphere(center + up, radius);
            Gizmos.DrawLine(center - up + right, center + up + right);
            Gizmos.DrawLine(center - up - right, center + up - right);
        }
    }

    private void RestoreCollider()
    {
        if (!initialized)
            return;

        if (circleCollider == null)
            return;

        SetCircleActive(true);
        SetCapsuleActive(false);
        currentMode = ColliderMode.Circle;

        circleCollider.radius = baseRadius;
        circleCollider.offset = baseOffset;
        currentRadius = baseRadius;
        currentOffset = baseOffset;
        targetRadius = baseRadius;
        targetOffset = baseOffset;

        if (capsuleCollider != null)
        {
            capsuleCollider.direction = CapsuleDirection2D.Vertical;
            capsuleCollider.size = baseCapsuleSize;
            capsuleCollider.offset = baseOffset;
            currentCapsuleSize = baseCapsuleSize;
            currentCapsuleOffset = baseOffset;
            targetCapsuleSize = baseCapsuleSize;
            targetCapsuleOffset = baseOffset;
        }
    }
}
