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

        ClassifySurface(col, inputX);

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

    public void ClassifySurface(Collider2D col, float input)
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
                if (onFloor) surface = SurfaceType.Floor;
                if (!climbExhausted)
                {
                    if (onWallL && input < -0.01f) { surface = SurfaceType.WallLeft; surfNormal = wallNormal; }
                    else if (onWallR && input > 0.01f) { surface = SurfaceType.WallRight; surfNormal = wallNormal; }
                }
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