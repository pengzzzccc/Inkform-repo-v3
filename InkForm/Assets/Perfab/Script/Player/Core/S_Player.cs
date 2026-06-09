using UnityEngine;
using UnityEngine.InputSystem;

public class S_Player : MonoBehaviour, IPlayerActor
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
    [SerializeField] private float gravityAlignSpeed = 540f;
    private Vector2 gravityUp = Vector2.up;
    public Vector2 GravityUp => gravityUp;
    public Vector2 GravityRight => new Vector2(gravityUp.y, -gravityUp.x);
    [Header("Slope Movement")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField, Range(0.1f, 1f)] private float walkableSlopeMinDot = 0.55f;
    [SerializeField, Min(0f)] private float maxSlopeVerticalSpeed = 5f;
    [SerializeField, Min(0f)] private float slopeAssistDisableTime = 0.12f;
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
    [Header("Camera Control")]
    [SerializeField] private S_CameraMove cameraController;

    [Header("Hook Skill")]
    [SerializeField] private S_HookTentacleRenderer hookTentacleRenderer;

    private S_PlayerSkillController skillController;
    private S_PlayerEnergy playerEnergy;

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
    private InputAction m_PlayerCameraControl;


    private bool facingRight = true;

    private bool gripping = false;

    private bool isSprinting = false;

    public void SetSprinting(bool value)
    {
        if (value && isParalyzed) return;
        isSprinting = value;
    }

    public bool IsParalyzed => isParalyzed;

    private bool sprintMomentum = false;

    public void SetSprintMomentum(bool value) => sprintMomentum = value;

    public bool IsSprintMomentumActive => sprintMomentum || isSprinting;

    private float sprintBreakthroughTimer;
    private float sprintBreakthroughDirection;
    private float sprintBreakthroughMinimumSpeed;

    public void ForceSprintBreakthrough(float direction, float minimumHorizontalSpeed, float duration)
    {
        if (!IsSprintMomentumActive)
            return;

        if (Mathf.Approximately(direction, 0f))
            direction = facingRight ? 1f : -1f;

        sprintBreakthroughDirection = Mathf.Sign(direction);
        sprintBreakthroughMinimumSpeed = Mathf.Max(0f, minimumHorizontalSpeed);
        sprintBreakthroughTimer = Mathf.Max(sprintBreakthroughTimer, Mathf.Max(0f, duration));

        ApplySprintBreakthroughVelocity();
    }


    public enum form
    {
        fluid,
        solid,
    }
    private form inkform;

    private bool isGroundedOnWalkableSurface;
    private Vector2 groundNormal = Vector2.up;
    private ContactPoint2D[] groundContacts;
    private float slopeAssistDisabledTimer;

    [Header("Hook Launch Momentum")]
    [SerializeField, Min(0f)] private float hookLaunchMomentumTime = 1.2f;
    private float hookLaunchMomentumTimer;
    public void BeginHookLaunchMomentum() => hookLaunchMomentumTimer = hookLaunchMomentumTime;
    private bool HookLaunchMomentumActive => hookLaunchMomentumTimer > 0f;

    public bool IsSprintCharging => skillController != null && skillController.IsSprintCharging;
    public bool IsCameraControlActive => skillController != null && skillController.IsCameraControlActive;
    public bool IsHookActive => skillController != null && skillController.IsHookActive;
    public S_PlayerEnergy Energy => playerEnergy;

    private bool movementLocked = false;
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked)
        {
            b_Rig.linearVelocity = Vector2.zero;
            b_Rig.angularVelocity = 0f;
        }
    }
    public bool IsMovementLocked => movementLocked;

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

        InputSystem_Actions actions = S_Input.Actions;
        m_PlayerMove = actions.Player.Move;
        m_PlayerJump = actions.Player.Jump;
        m_PlayerSprint = actions.Player.Sprint;
        m_PlayerGrep = actions.Player.grep;
        m_PlayerCameraControl = actions.Camera.CameraControl;

        b_Sprite.sprite = sprites[0];
        b_Rig.gravityScale = solidGravityScale;

        b_Rig.interpolation = RigidbodyInterpolation2D.Interpolate;
        b_Rig.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        SetupProceduralRenderer();
        SetupDynamicCollider();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<S_CameraMove>();

        SetupEnergy();
        SetupSkillController();

        inkform = form.fluid;
        SetForm(inkform);


        if (fluidClimbSkill != null)
            fluidClimbSkill.SetSurface(S_fluid_climb.SurfaceType.None);
    }

    void Update()
    {
        skillController?.HandleCameraControlInput();
        if (IsCameraControlActive)
        {
            skillController.CameraControlTick();
            return;
        }

        skillController?.HandleHookInput();
        if (IsHookActive)
            return;

        Jump();
        StateRunner();

        if (IsSprintCharging && skillController.SprintReleasedThisFrame())
        {
            ReleaseSprintCharge();
        }

        skillController?.TickCooldown();
    }

    void LateUpdate()
    {
        UpdateSprite();
        if (IsHookActive)
            skillController.HookRenderTick();

        AlignToGravity();
    }

    private void HandleGravityChanged(Vector2 newUp)
    {
        gravityUp = newUp.sqrMagnitude > 0.0001f ? newUp.normalized : Vector2.up;
        if (fluidClimbSkill != null)
            fluidClimbSkill.SetGravity(gravityUp);
    }

    private void AlignToGravity()
    {
        if (body == null)
            return;

        // Rotate the body (which holds the Rigidbody2D) around its own centre. Rotating the
        // S_Player root instead would swing the offset body in an arc and fling it via physics.
        float targetAngle = Mathf.Atan2(gravityUp.y, gravityUp.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, targetRot, gravityAlignSpeed * Time.deltaTime);
    }
    void StateRunner()
    {
        if (movementLocked || IsCameraControlActive)
        {
            gripping = false;
            return;
        }
        gripping = m_PlayerGrep.IsPressed() && IsFluidClimbSkillAvailable();
    }

    void Jump()
    {
        if (IsCameraControlActive) return;
        if (IsHookActive) return;
        if (movementLocked) return;

        if (inkform == form.solid || (inkform == form.fluid && !gripping))
        {
            if (jumpCoolDownTimer > 0) jumpCoolDownTimer -= Time.deltaTime;

            if (m_PlayerJump.WasPerformedThisFrame() && jumpCount < maxJump && jumpCoolDownTimer <= 0 && !isParalyzed)
            {
                S_GameEvent.PlaySFX(jumpClip);
                slopeAssistDisabledTimer = slopeAssistDisableTime;
                Vector2 jumpVel = b_Rig.linearVelocity;
                jumpVel -= gravityUp * Vector2.Dot(jumpVel, gravityUp);
                b_Rig.linearVelocity = jumpVel;
                b_Rig.AddForce(gravityUp * jumpSpeed, ForceMode2D.Impulse);
                jumpCount++;
                jumpCoolDownTimer = jumpCoolDownTime;
            }

            if (m_PlayerSprint.WasPerformedThisFrame() && !IsSprintCharging)
            {
                BeginSprintCharge();
            }

        }
    }

    void FixedUpdate()
    {
        if (IsCameraControlActive)
        {
            UpdateDynamicCollider();
            return;
        }

        if (IsHookActive)
        {
            skillController.FixedTickHook();
            UpdateDynamicCollider();
            return;
        }

        if (movementLocked)
        {
            b_Rig.linearVelocity = Vector2.zero;
            b_Rig.angularVelocity = 0f;
            UpdateDynamicCollider();
            return;
        }

        UpdateSlopeAssistTimer();
        SampleWalkableGround();
        ResetJumpCountIfGrounded();

        if (hookLaunchMomentumTimer > 0f)
        {
            hookLaunchMomentumTimer -= Time.fixedDeltaTime;
            if (isGroundedOnWalkableSurface)
                hookLaunchMomentumTimer = 0f;
        }

        if (inkform == form.solid)
            SolidMovement();
        else
            FluidMovement();

        if (IsSprintCharging)
            skillController?.FixedTickSprintCharge();

        UpdateSprintBreakthrough();
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
            moveV = input;
            float speed = isParalyzed ? moveSpeed * paralyzeSlowMultiplier : moveSpeed;
            if (HookLaunchMomentumActive)
            {
                // Keep the swing-launch inertia; allow light air steering but never kill speed.
                float currentX = b_Rig.linearVelocity.x;
                float desiredX = moveV * speed;
                float newX;
                if (Mathf.Abs(moveV) < 0.01f)
                    newX = currentX;
                else if (Mathf.Sign(desiredX) == Mathf.Sign(currentX))
                    newX = Mathf.Abs(desiredX) > Mathf.Abs(currentX) ? desiredX : currentX;
                else
                    newX = desiredX;
                b_Rig.linearVelocity = new Vector2(newX, b_Rig.linearVelocity.y);
            }
            else
            {
                b_Rig.linearVelocity = ShouldUseSlopeMovement(moveV)
                    ? GetSlopeVelocity(moveV, speed)
                    : GravityRight * (moveV * speed) + gravityUp * Vector2.Dot(b_Rig.linearVelocity, gravityUp);
            }
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

    private void SetupSkillController()
    {
        if (skillController == null)
            skillController = GetComponent<S_PlayerSkillController>();

        if (skillController == null)
            skillController = gameObject.AddComponent<S_PlayerSkillController>();

        SetupHookTentacleRenderer();

        skillController.Initialize(
            this,
            m_PlayerMove,
            m_PlayerSprint,
            m_PlayerCameraControl,
            cameraController,
            proceduralRenderer,
            dynamicCollider,
            body,
            b_Rig,
            solidGravityScale,
            useDynamicCollider,
            m_PlayerJump,
            hookTentacleRenderer);
    }

    private void SetupHookTentacleRenderer()
    {
        if (body == null)
            return;

        if (hookTentacleRenderer == null)
            hookTentacleRenderer = body.GetComponent<S_HookTentacleRenderer>();

        if (hookTentacleRenderer == null)
            hookTentacleRenderer = body.AddComponent<S_HookTentacleRenderer>();

        hookTentacleRenderer.Initialize(body.transform, b_Sprite);
    }

    private void SetupEnergy()
    {
        if (playerEnergy == null)
            playerEnergy = GetComponent<S_PlayerEnergy>();

        if (playerEnergy == null)
            playerEnergy = gameObject.AddComponent<S_PlayerEnergy>();
    }

    private void UpdateDynamicCollider()
    {
        if (!useDynamicCollider || dynamicCollider == null)
            return;

        dynamicCollider.DynamicColliderTick(this, fluidClimbSkill);
    }

    private void UpdateSlopeAssistTimer()
    {
        if (slopeAssistDisabledTimer > 0f)
            slopeAssistDisabledTimer -= Time.fixedDeltaTime;
    }

    private void SampleWalkableGround()
    {
        isGroundedOnWalkableSurface = false;
        groundNormal = gravityUp;

        Collider2D activeCollider = GetCollider();
        if (activeCollider == null)
            return;

        if (groundContacts == null || groundContacts.Length == 0)
            groundContacts = new ContactPoint2D[8];

        int contactCount = activeCollider.GetContacts(groundContacts);
        float bestDot = walkableSlopeMinDot;

        for (int i = 0; i < contactCount; i++)
        {
            Collider2D contactCollider = groundContacts[i].collider;
            if (contactCollider == null || contactCollider.isTrigger)
                continue;

            if (!IsInGroundLayer(contactCollider.gameObject.layer))
                continue;

            Vector2 normal = groundContacts[i].normal;
            if (normal.sqrMagnitude <= 0.001f)
                continue;

            normal.Normalize();
            float upDot = Vector2.Dot(normal, gravityUp);
            if (upDot < bestDot)
                continue;

            bestDot = upDot;
            groundNormal = normal;
            isGroundedOnWalkableSurface = true;
        }
    }

    private void ResetJumpCountIfGrounded()
    {
        if (slopeAssistDisabledTimer > 0f)
            return;

        bool groundedByPlayerContacts = isGroundedOnWalkableSurface;
        bool groundedByClimbSurface = fluidClimbSkill != null
            && fluidClimbSkill.GetSurface() == S_fluid_climb.SurfaceType.Floor;

        if (groundedByPlayerContacts || groundedByClimbSurface)
            jumpCount = 0;
    }

    private bool ShouldUseSlopeMovement(float moveInput)
    {
        if (!isGroundedOnWalkableSurface || slopeAssistDisabledTimer > 0f)
            return false;

        if (Mathf.Abs(moveInput) <= 0.01f)
            return false;

        return Mathf.Abs(groundNormal.x) > 0.001f;
    }

    private Vector2 GetSlopeVelocity(float moveInput, float speed)
    {
        Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x).normalized;
        if (Mathf.Sign(tangent.x) != Mathf.Sign(moveInput))
            tangent = -tangent;

        Vector2 slopeVelocity = tangent * (Mathf.Abs(moveInput) * speed);
        slopeVelocity.y = Mathf.Clamp(slopeVelocity.y, -maxSlopeVerticalSpeed, maxSlopeVerticalSpeed);
        return slopeVelocity;
    }

    private bool IsInGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }

    public Rigidbody2D GetRigidbody() => b_Rig;
    public Rigidbody2D Rigidbody => b_Rig;
    public S_PlayerEnergy GetEnergy() => playerEnergy;

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

    public Collider2D Collider => GetCollider();


    public float GetMoveInput() => m_PlayerMove.ReadValue<Vector2>().x;


    public float GetClimbInput() => m_PlayerMove.ReadValue<Vector2>().y;

    public Vector2 GetMoveVector() => m_PlayerMove.ReadValue<Vector2>();

    public float GetMoveSpeed() => moveSpeed;
    public Transform GetBodyTransform() => body.transform;
    public Transform BodyTransform => GetBodyTransform();


    public void SetFacingRight(bool right) => facingRight = right;


    public bool GetFaceRight() => facingRight;
    public bool FacingRight => facingRight;






    public bool getForm()
    {
        if (inkform == form.solid) return false;
        return true;
    }

    public bool IsFluidForm => getForm();

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

    public void BeginCameraControl(S_CameraControlSkill skill) => skillController?.BeginCameraControl(skill);

    private void EndCameraControl() => skillController?.EndCameraControl();

    internal void ClearGripState() => gripping = false;

    private bool IsFluidClimbSkillAvailable()
    {
        return S_SkillTree.Instance == null || S_SkillTree.Instance.IsUnlocked("FluidClimb");
    }

    void OnEnable()
    {
        S_Input.Actions.Player.Enable();
        gravityUp = S_GravityState.GravityUp;
        if (fluidClimbSkill != null)
            fluidClimbSkill.SetGravity(gravityUp);
        S_GameEvent.OnGravityChanged += HandleGravityChanged;
    }


    void OnDisable()
    {
        CancelSprintCharge();
        EndCameraControl();

        S_Input.Actions.Player.Disable();
        S_GameEvent.OnGravityChanged -= HandleGravityChanged;
    }

    private void BeginSprintCharge() => skillController?.BeginSprintCharge();

    public void ReleaseSprintCharge() => skillController?.ReleaseSprintCharge();

    public void CancelSprintCharge() => skillController?.CancelSprintCharge();

    public void CancelActiveSkills()
    {
        CancelSprintCharge();
        EndCameraControl();
        skillController?.CancelHook();
        ClearGripState();
    }

    private void UpdateSprintBreakthrough()
    {
        if (sprintBreakthroughTimer <= 0f)
            return;

        if (!IsSprintMomentumActive)
        {
            sprintBreakthroughTimer = 0f;
            return;
        }

        ApplySprintBreakthroughVelocity();
        sprintBreakthroughTimer -= Time.fixedDeltaTime;
    }

    private void ApplySprintBreakthroughVelocity()
    {
        if (sprintBreakthroughMinimumSpeed <= 0f)
            return;

        Vector2 currentVelocity = b_Rig.linearVelocity;
        float currentHorizontalSpeed = Mathf.Abs(currentVelocity.x);
        bool movingAgainstBreakthrough = Mathf.Sign(currentVelocity.x) != sprintBreakthroughDirection && currentHorizontalSpeed > 0.01f;

        if (movingAgainstBreakthrough || currentHorizontalSpeed < sprintBreakthroughMinimumSpeed)
        {
            b_Rig.linearVelocity = new Vector2(sprintBreakthroughDirection * sprintBreakthroughMinimumSpeed, currentVelocity.y);
        }
    }

}
