using UnityEngine;

[CreateAssetMenu(fileName = "Skill_FluidClimb", menuName = "InkForm/Skills/FluidClimb")]
public class S_fluid_climb : S_SkillBase
{
    [Header("Wall Climbing")]
    [SerializeField] private float stickyForce = 3f;
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float fluidGravityScale = 4f;
    [SerializeField] private float activeTime = 4.0f;
    [SerializeField] private LayerMask surfaceLayer = ~0;
    [SerializeField][Range(0.1f, 1.0f)] private float floorDotThreshold = 0.5f;
    [SerializeField][Range(0.1f, 1.0f)] private float ceilingDotThreshold = 0.5f;
    [Header("Grip Buffer")]
    [SerializeField][Range(0f, 1.5f)] private float gripBufferDistance = 0.35f;
    [SerializeField][Range(0f, 0.1f)] private float gripSnapSkin = 0.01f;
    [SerializeField][Range(0f, 1f)] private float gripInputThreshold = 0.1f;
    [SerializeField] private bool drawGripBufferGizmos = true;
    [SerializeField] private Color gripBufferGizmoColor = new Color(0f, 0.8f, 1f, 0.35f);

    public enum SurfaceType
    {
        None,
        Floor,
        WallLeft,
        WallRight,
        Ceiling,
    }

    [System.NonSerialized] private SurfaceType surface = SurfaceType.None;
    [System.NonSerialized] private Vector2 surfNormal = Vector2.up;
    [System.NonSerialized] private ContactPoint2D[] contacts;
    [System.NonSerialized] private RaycastHit2D[] gripBufferHits;
    [System.NonSerialized] private int noContactFrames = 0;
    [System.NonSerialized] private float climbTimer = 0f;
    [System.NonSerialized] private bool wasClimbing = false;
    [System.NonSerialized] private bool climbExhausted = false;
    private const int noContactThreshold = 3;

    public SurfaceType GetSurface() => surface;
    public void SetSurface(SurfaceType s) => surface = s;
    public Vector2 GetSurfNormal() => surfNormal;
    public float GetClimbTimer() => climbTimer;
    public float GetActiveTime() => activeTime;
    public void ResetClimbTimer() => climbTimer = 0f;

    public override void Activate(S_Player player)
    {
    }

    public void FluidMovementTick(S_Player player)
    {
        if (contacts == null) contacts = new ContactPoint2D[3];

        float inputX = player.GetMoveInput();
        float inputY = player.GetClimbInput();
        Rigidbody2D rig = player.GetRigidbody();
        Collider2D col = player.GetCollider();

        ClassifySurface(col, inputX, true);
        TryGripBuffer(player, col, rig, inputX);

        bool isClimbing = surface == SurfaceType.WallLeft || surface == SurfaceType.WallRight || surface == SurfaceType.Ceiling;
        if (isClimbing)
        {
            climbTimer += Time.fixedDeltaTime;
            wasClimbing = true;
            if (climbTimer >= activeTime)
            {
                surface = SurfaceType.None;
                rig.gravityScale = fluidGravityScale;
                climbTimer = 0f;
                wasClimbing = false;
                climbExhausted = true;
                return;
            }
        }
        else if (surface == SurfaceType.Floor)
        {
            climbTimer = 0f;
            wasClimbing = false;
            climbExhausted = false;
        }
        else if (surface == SurfaceType.None)
        {
            wasClimbing = false;
        }

        Vector2 moveVel = ComputeFluidVelocity(rig, inputX, inputY, player.GetMoveSpeed());
        rig.linearVelocity = moveVel;

        if (inputX > 0.01f) player.SetFacingRight(true);
        else if (inputX < -0.01f) player.SetFacingRight(false);

        Debug.DrawRay(player.GetBodyTransform().position, surfNormal, Color.yellow);
    }

    public void ClassifySurface(Collider2D col, float input, bool allowDirectCeilingEntry = false)
    {
        if (contacts == null) contacts = new ContactPoint2D[3];

        int count = col.GetContacts(contacts);

        bool onFloor = false;
        bool onWallL = false;
        bool onWallR = false;
        bool onCeill = false;
        Vector2 wallNormal = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            Vector2 n = contacts[i].normal;

            if (surfaceLayer != (~0) && (surfaceLayer.value & (1 << contacts[i].collider.gameObject.layer)) == 0)
                continue;

            float dot = Vector2.Dot(n, Vector2.up);

            if (dot > floorDotThreshold) { onFloor = true; surfNormal = n; }
            else if (dot < -ceilingDotThreshold) { onCeill = true; surfNormal = n; }
            else
            {
                if (n.x > 0.1f) { onWallL = true; wallNormal = n; }
                else if (n.x < -0.1f) { onWallR = true; wallNormal = n; }
            }
        }

        if (count == 0)
        {
            noContactFrames++;
            if (noContactFrames >= noContactThreshold)
            {
                surface = SurfaceType.None;
                surfNormal = Vector2.zero;
            }
            return;
        }
        noContactFrames = 0;

        switch (surface)
        {
            case SurfaceType.None:
            case SurfaceType.Floor:
                if (!climbExhausted)
                {
                    if (allowDirectCeilingEntry && onCeill) { surface = SurfaceType.Ceiling; }
                    else if (onWallL && input < -0.01f) { surface = SurfaceType.WallLeft; surfNormal = wallNormal; }
                    else if (onWallR && input > 0.01f) { surface = SurfaceType.WallRight; surfNormal = wallNormal; }
                    else if (onFloor) surface = SurfaceType.Floor;
                }
                else if (onFloor) surface = SurfaceType.Floor;
                break;
            case SurfaceType.Ceiling:
                if (climbExhausted) { surface = onFloor ? SurfaceType.Floor : SurfaceType.None; break; }
                if (onCeill) surface = SurfaceType.Ceiling;
                else if (onWallL) { surface = SurfaceType.WallLeft; surfNormal = wallNormal; }
                else if (onWallR) { surface = SurfaceType.WallRight; surfNormal = wallNormal; }
                break;
            case SurfaceType.WallLeft:
                if (climbExhausted) { surface = onFloor ? SurfaceType.Floor : SurfaceType.None; break; }
                if (onWallL) { surface = SurfaceType.WallLeft; surfNormal = wallNormal; }
                if (onCeill) { surface = SurfaceType.Ceiling; }
                else if (!onWallL && onFloor) { surface = SurfaceType.Floor; }
                break;
            case SurfaceType.WallRight:
                if (climbExhausted) { surface = onFloor ? SurfaceType.Floor : SurfaceType.None; break; }
                if (onWallR) { surface = SurfaceType.WallRight; surfNormal = wallNormal; }
                if (onCeill) { surface = SurfaceType.Ceiling; }
                else if (!onWallR && onFloor) { surface = SurfaceType.Floor; }
                break;
        }
    }

    private bool TryGripBuffer(S_Player player, Collider2D col, Rigidbody2D rig, float inputX)
    {
        if (gripBufferDistance <= 0f || climbExhausted)
            return false;

        if (surface != SurfaceType.None && surface != SurfaceType.Floor)
            return false;

        if (col == null || rig == null)
            return false;

        if (gripBufferHits == null) gripBufferHits = new RaycastHit2D[4];

        float directionX = GetGripDirection(player, inputX);
        Vector2 direction = new Vector2(directionX, 0f);
        ContactFilter2D filter = CreateSurfaceFilter();
        int hitCount = col.Cast(direction, filter, gripBufferHits, gripBufferDistance);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = gripBufferHits[i];
            if (hit.collider == null)
                continue;

            if (!IsWallHitForDirection(hit.normal, directionX))
                continue;

            surface = directionX < 0f ? SurfaceType.WallLeft : SurfaceType.WallRight;
            surfNormal = hit.normal;
            noContactFrames = 0;

            float snapDistance = Mathf.Max(hit.distance - gripSnapSkin, 0f);
            rig.position += direction * snapDistance;
            rig.linearVelocity = new Vector2(0f, rig.linearVelocity.y);
            return true;
        }

        return false;
    }

    private float GetGripDirection(S_Player player, float inputX)
    {
        if (Mathf.Abs(inputX) > gripInputThreshold)
            return Mathf.Sign(inputX);

        return player.GetFaceRight() ? 1f : -1f;
    }

    private bool IsWallHitForDirection(Vector2 normal, float directionX)
    {
        if (directionX < 0f)
            return normal.x > 0.1f;

        return normal.x < -0.1f;
    }

    private ContactFilter2D CreateSurfaceFilter()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(surfaceLayer);
        filter.useTriggers = false;
        return filter;
    }

    public void DrawGripBufferGizmos(Transform bodyTransform, Collider2D bodyCollider, bool facingRight)
    {
        if (!drawGripBufferGizmos || bodyTransform == null || gripBufferDistance <= 0f)
            return;

        Bounds bounds = bodyCollider != null
            ? bodyCollider.bounds
            : new Bounds(bodyTransform.position, Vector3.one);

        Vector3 leftCenter = bounds.center + Vector3.left * (bounds.extents.x + gripBufferDistance * 0.5f);
        Vector3 rightCenter = bounds.center + Vector3.right * (bounds.extents.x + gripBufferDistance * 0.5f);
        Vector3 bufferSize = new Vector3(gripBufferDistance, bounds.size.y, 0.05f);

        Color bufferColor = gripBufferGizmoColor;
        Gizmos.color = bufferColor;
        Gizmos.DrawWireCube(leftCenter, bufferSize);
        Gizmos.DrawWireCube(rightCenter, bufferSize);

        Vector3 gripDirection = facingRight ? Vector3.right : Vector3.left;
        Vector3 origin = bounds.center;
        Vector3 end = origin + gripDirection * (bounds.extents.x + gripBufferDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, 0.05f);
    }

    private Vector2 ComputeFluidVelocity(Rigidbody2D rig, float inputX, float inputY, float moveSpeed)
    {
        if (surface == SurfaceType.None)
        {
            bool hasInput = Mathf.Abs(inputX) > 0.01f;
            rig.gravityScale = fluidGravityScale;
            if (hasInput)
                return new Vector2(inputX * moveSpeed * 0.6f, rig.linearVelocity.y);
            else
                return new Vector2(rig.linearVelocity.x * 0.9f, rig.linearVelocity.y);
        }

        rig.gravityScale = 0f;

        Vector2 stick = -surfNormal * stickyForce;
        Vector2 tangent = new Vector2(surfNormal.y, -surfNormal.x);

        bool isWall = surface == SurfaceType.WallLeft || surface == SurfaceType.WallRight;
        bool isCeiling = surface == SurfaceType.Ceiling;

        if (isWall)
        {
            bool hasClimbInput = Mathf.Abs(inputY) > 0.01f;
            if (hasClimbInput)
                return stick + Vector2.up * (inputY * climbSpeed);
            else
                return stick;
        }
        else if (isCeiling)
        {
            bool hasInput = Mathf.Abs(inputX) > 0.01f;
            if (hasInput)
                return stick + Vector2.right * (inputX * climbSpeed);
            else
                return stick;
        }
        else
        {
            bool hasInput = Mathf.Abs(inputX) > 0.01f;
            if (hasInput)
                return stick + tangent * (inputX * climbSpeed);
            else
                return stick;
        }
    }

    public override void OnUnlocked(S_Player player)
    {
        Debug.Log($"[SkillTree] unlocked skill {skillName}");
    }
}
