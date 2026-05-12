using UnityEngine;
using UnityEngine.InputSystem;

public class S_Player : MonoBehaviour
{
    public static S_Player Instance { get; private set; }
    [SerializeField] private float MoveSpeed = 10f;
    [SerializeField] private float JumpSpeed = 10f;
    [SerializeField] private int MaxJump = 1;
    [SerializeField] private float JumpCoolDownTime = 0.1f;
    [SerializeField] private GameObject body;
    [SerializeField] private Sprite[] sprites;
    [Header("Wall Climbing Control")]
    [SerializeField] private PhysicsMaterial2D SolidMat;
    [SerializeField] private PhysicsMaterial2D FluidMat;
    [SerializeField] private S_fluid_climb fluidClimbSkill;
    [Header("Gravity Control")]
    [SerializeField] private float solidGravityScale = 4f;
    [Header("Kick Force")]
    [SerializeField] private float kickForceMultiplier = 10f;
    [Header("SFX")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip formSwitchClip;
    [Header("Paralyze")]
    [SerializeField] private float paralyzeSlowMultiplier = 0.5f;
    [SerializeField] private float defaultParalyzeDuration = 3f;
    [Header("Procedural Rendering")]
    [SerializeField] private bool useProceduralRenderer = true;
    [SerializeField] private S_PlayerProceduralRenderer proceduralRenderer;
    [Header("Dynamic Collider")]
    [SerializeField] private bool useDynamicCollider = true;
    [SerializeField] private S_PlayerDynamicCollider dynamicCollider;


    private float moveSpeed;
    private float jumpSpeed;
    private int maxJump;
    private float jumpCoolDownTime;
    private float jumpCoolDownTimer = 0;
    private int jumpCount = 0;
    private Rigidbody2D b_Rig;
    private SpriteRenderer b_Sprite;
    private Collider2D b_Col;
    private CircleCollider2D b_CircleCol;
    private int baseMaxJump;
    private float baseMoveSpeed;
    private Coroutine paralyzeCoroutine;
    private bool isParalyzed = false;

    private InputAction m_PlayerMove;
    private InputAction m_PlayerJump;
    private InputAction m_PlayerSprint;
    private InputAction m_PlayerGrep;


    private bool facingRight = true;

    private bool gripping = false;

    private bool isSprinting = false;
    private bool movementLocked = false;
    private RigidbodyConstraints2D constraintsBeforeMovementLock;

    public void SetSprinting(bool value)
    {
        if (value && isParalyzed) return;
        if (value && movementLocked) return;
        isSprinting = value;
    }

    public bool IsParalyzed => isParalyzed;

    private bool sprintMomentum = false;

    public void SetSprintMomentum(bool value) => sprintMomentum = value;


    public enum form
    {
        fluid,
        solid,
    }
    private form inkform;

    void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        moveSpeed = MoveSpeed;
        jumpSpeed = JumpSpeed;
        maxJump = MaxJump;
        jumpCoolDownTime = JumpCoolDownTime;
        baseMaxJump = MaxJump;
        baseMoveSpeed = MoveSpeed;

        b_Rig = body.GetComponent<Rigidbody2D>();
        b_Sprite = body.GetComponent<SpriteRenderer>();
        b_CircleCol = body.GetComponent<CircleCollider2D>();
        b_Col = b_CircleCol;

        InputSystem_Actions actions = S_InputBindingManager.Instance.Actions;
        m_PlayerMove = actions.Player.Move;
        m_PlayerJump = actions.Player.Jump;
        m_PlayerSprint = actions.Player.Sprint;
        m_PlayerGrep = actions.Player.grep;

        b_Sprite.sprite = sprites[0];
        b_Rig.gravityScale = solidGravityScale;

        b_Rig.interpolation = RigidbodyInterpolation2D.Interpolate;
        b_Rig.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        SetupProceduralRenderer();
        SetupDynamicCollider();

        inkform = form.fluid;
        SetForm(inkform);


        if (fluidClimbSkill != null)
            fluidClimbSkill.SetSurface(S_fluid_climb.SurfaceType.None);
    }

    void Update()
    {
        if (movementLocked)
        {
            MaintainMovementLock();
            return;
        }

        Jump();
        StateRunner();
    }

    void LateUpdate()
    {
        UpdateSprite();
    }
    void StateRunner()
    {
        gripping = m_PlayerGrep.IsPressed();
    }

    void Jump()
    {

        if (inkform == form.solid || (inkform == form.fluid && !gripping))
        {
            if (jumpCoolDownTimer > 0) jumpCoolDownTimer -= Time.deltaTime;

            if (m_PlayerJump.WasPerformedThisFrame() && jumpCount < maxJump && jumpCoolDownTimer <= 0 && !isParalyzed)
            {
                S_GameEvent.PlaySFX(jumpClip);
                b_Rig.linearVelocity = new Vector2(b_Rig.linearVelocity.x, 0);
                b_Rig.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
                jumpCount++;
                jumpCoolDownTimer = jumpCoolDownTime;
            }

            if (m_PlayerSprint.WasPerformedThisFrame())
            {
                S_SkillTree.Instance.ActivateSkill("Sprint");
            }

            if (fluidClimbSkill != null && fluidClimbSkill.GetSurface() == S_fluid_climb.SurfaceType.Floor)
            {
                jumpCount = 0;
            }
        }
    }

    void FixedUpdate()
    {
        if (movementLocked)
        {
            MaintainMovementLock();
            return;
        }

        if (inkform == form.solid)
            SolidMovement();
        else
            FluidMovement();

        UpdateDynamicCollider();
    }

    void FluidMovement()
    {
        if (fluidClimbSkill != null && gripping && !isParalyzed)
        {
            fluidClimbSkill.FluidMovementTick(this);
            return;
        }
        else
        {
            SolidMovement();
        }
    }

    void SolidMovement()
    {
        b_Rig.gravityScale = solidGravityScale;
        float input = m_PlayerMove.ReadValue<Vector2>().x;

        if (fluidClimbSkill != null) fluidClimbSkill.ClassifySurface(GetCollider(), input);

        float moveV = 0;

        if (!isSprinting)
        {
            moveV = m_PlayerMove.ReadValue<Vector2>().x;
            float speed = isParalyzed ? moveSpeed * paralyzeSlowMultiplier : moveSpeed;
            b_Rig.linearVelocity = new Vector2(moveV * speed, b_Rig.linearVelocity.y);
        }

        if (moveV > 0 && !facingRight)
        {
            facingRight = true;
        }
        else if (moveV < 0 && facingRight)
        {
            facingRight = false;
        }
    }

    public void SetForm(form newForm)
    {
        if (inkform != newForm)
            S_GameEvent.PlaySFX(formSwitchClip);
        inkform = newForm;
        switch (newForm)
        {
            case form.solid:
                b_Rig.sharedMaterial = SolidMat;
                break;

            case form.fluid:
                b_Rig.sharedMaterial = FluidMat;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (body == null)
            return;

        if (fluidClimbSkill != null)
        {
            Collider2D bodyCollider = GetCollider() != null ? GetCollider() : body.GetComponent<Collider2D>();
            fluidClimbSkill.DrawGripBufferGizmos(body.transform, bodyCollider, facingRight);
        }

        S_PlayerProceduralRenderer renderer = proceduralRenderer != null
            ? proceduralRenderer
            : body.GetComponent<S_PlayerProceduralRenderer>();
        if (renderer != null)
            renderer.DrawRendererGizmos();

        S_PlayerDynamicCollider colliderRenderer = dynamicCollider != null
            ? dynamicCollider
            : body.GetComponent<S_PlayerDynamicCollider>();
        if (colliderRenderer != null)
            colliderRenderer.DrawDynamicColliderGizmos();
    }

    void UpdateSprite()
    {
        if (useProceduralRenderer && proceduralRenderer != null)
        {
            proceduralRenderer.RenderTick(this, fluidClimbSkill);
            return;
        }

        if (b_Sprite == null) return;

        b_Sprite.enabled = true;
        b_Sprite.color = Color.white;
        int formOffset = (inkform == form.solid) ? 0 : 2;
        int dirOffset = facingRight ? 0 : 1;
        b_Sprite.sprite = sprites[formOffset + dirOffset];
    }

    private void SetupProceduralRenderer()
    {
        if (!useProceduralRenderer || body == null)
            return;

        if (proceduralRenderer == null)
            proceduralRenderer = body.GetComponent<S_PlayerProceduralRenderer>();

        if (proceduralRenderer == null)
            proceduralRenderer = body.AddComponent<S_PlayerProceduralRenderer>();

        proceduralRenderer.Initialize(b_Sprite, b_Rig, b_Col);
    }

    private void SetupDynamicCollider()
    {
        if (!useDynamicCollider || body == null || b_CircleCol == null)
            return;

        if (dynamicCollider == null)
            dynamicCollider = body.GetComponent<S_PlayerDynamicCollider>();

        if (dynamicCollider == null)
            dynamicCollider = body.AddComponent<S_PlayerDynamicCollider>();

        dynamicCollider.Initialize(b_CircleCol, b_Rig);
    }

    private void UpdateDynamicCollider()
    {
        if (!useDynamicCollider || dynamicCollider == null)
            return;

        dynamicCollider.DynamicColliderTick(this, fluidClimbSkill);
    }

    public Rigidbody2D GetRigidbody() => b_Rig;
    public Collider2D GetCollider()
    {
        if (useDynamicCollider && dynamicCollider != null)
        {
            Collider2D activeCollider = dynamicCollider.GetActiveCollider();
            if (activeCollider != null)
                return activeCollider;
        }

        return b_Col;
    }


    public float GetMoveInput() => m_PlayerMove.ReadValue<Vector2>().x;


    public float GetClimbInput() => m_PlayerMove.ReadValue<Vector2>().y;

    public Vector2 GetMoveVector() => m_PlayerMove.ReadValue<Vector2>();

    public float GetMoveSpeed() => moveSpeed;
    public Transform GetBodyTransform() => body.transform;


    public void SetFacingRight(bool right) => facingRight = right;


    public bool GetFaceRight() => facingRight;

    public bool IsMovementLocked => movementLocked;

    public void SetMovementLocked(bool locked)
    {
        if (movementLocked == locked)
            return;

        movementLocked = locked;

        if (b_Rig == null)
            return;

        if (locked)
        {
            constraintsBeforeMovementLock = b_Rig.constraints;
            SetSprinting(false);
            SetSprintMomentum(false);
            gripping = false;
            MaintainMovementLock();
            b_Rig.constraints = RigidbodyConstraints2D.FreezeAll;
            return;
        }

        b_Rig.constraints = constraintsBeforeMovementLock;
        b_Rig.gravityScale = solidGravityScale;
    }

    private void MaintainMovementLock()
    {
        if (b_Rig == null)
            return;

        b_Rig.linearVelocity = Vector2.zero;
        b_Rig.angularVelocity = 0f;
        b_Rig.gravityScale = 0f;
    }


    public bool getForm()
    {
        if (inkform == form.solid) return false;
        return true;
    }

    public void KickOut()
    {
        b_Rig.AddForce(b_Rig.linearVelocity * kickForceMultiplier, ForceMode2D.Impulse);
    }

    public void Teleport(Vector2 targetPos)
    {
        b_Rig.linearVelocity = Vector2.zero;
        b_Rig.angularVelocity = 0f;
        b_Rig.position = targetPos;
    }

    public void ApplyParalyze(float duration, float slowMultiplier)
    {
        // Use provided values or defaults
        float dur = duration > 0f ? duration : defaultParalyzeDuration;
        float mult = slowMultiplier > 0f ? slowMultiplier : paralyzeSlowMultiplier;

        if (paralyzeCoroutine != null)
            StopCoroutine(paralyzeCoroutine);

        paralyzeCoroutine = StartCoroutine(ParalyzeRoutine(dur, mult));
    }

    private System.Collections.IEnumerator ParalyzeRoutine(float duration, float slowMultiplier)
    {
        isParalyzed = true;
        maxJump = 1;
        moveSpeed = baseMoveSpeed * slowMultiplier;
        SetSprinting(false);

        yield return new WaitForSeconds(duration);

        isParalyzed = false;
        maxJump = baseMaxJump;
        moveSpeed = baseMoveSpeed;

        paralyzeCoroutine = null;
    }
}
