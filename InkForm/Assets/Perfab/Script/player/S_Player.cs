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

    // Sprint Charge
    private bool isSprintCharging;
    private float sprintChargeTimer;
    private int sprintChargeStage;
    private float chargeScaleMultiplier = 1f;
    private float chargeShakeTimer;
    private int previousChargeStage;
    private bool chargeRotationUnlocked;
    private PhysicsMaterial2D originalPhysicsMaterial;
    private S_Soild_sprint sprintSkill;
    private float sprintCooldownRemaining;
    private bool chargeVisualsActive;
    private AudioSource sprintChargeSource;

    private bool isCameraControlActive;
    private S_CameraControlSkill cameraControlSkill;
    private float timeScaleBeforeCameraControl = 1f;
    private float fixedDeltaTimeBeforeCameraControl = 0.02f;
    private bool isGroundedOnWalkableSurface;
    private Vector2 groundNormal = Vector2.up;
    private ContactPoint2D[] groundContacts;
    private float slopeAssistDisabledTimer;

    public bool IsSprintCharging => isSprintCharging;

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
        SetupSprintChargeAudioSource();

        InputSystem_Actions actions = S_InputBindingManager.Instance.Actions;
        m_PlayerMove = actions.Player.Move;
        m_PlayerJump = actions.Player.Jump;
        m_PlayerSprint = actions.Player.Sprint;
        m_PlayerGrep = actions.Player.grep;
        m_PlayerCameraControl = actions.Player.CameraControl;

        b_Sprite.sprite = sprites[0];
        b_Rig.gravityScale = solidGravityScale;

        b_Rig.interpolation = RigidbodyInterpolation2D.Interpolate;
        b_Rig.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        SetupProceduralRenderer();
        SetupDynamicCollider();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<S_CameraMove>();

        inkform = form.fluid;
        SetForm(inkform);


        if (fluidClimbSkill != null)
            fluidClimbSkill.SetSurface(S_fluid_climb.SurfaceType.None);
    }

    void Update()
    {
        HandleCameraControlInput();
        if (isCameraControlActive)
        {
            CameraControlTick();
            return;
        }

        Jump();
        StateRunner();

        if (isSprintCharging && m_PlayerSprint.WasReleasedThisFrame())
        {
            ReleaseSprintCharge();
        }

        if (sprintCooldownRemaining > 0)
            sprintCooldownRemaining -= Time.deltaTime;
    }

    void LateUpdate()
    {
        UpdateSprite();
    }
    void StateRunner()
    {
        if (movementLocked || isCameraControlActive)
        {
            gripping = false;
            return;
        }
        gripping = m_PlayerGrep.IsPressed();
    }

    void Jump()
    {
        if (isCameraControlActive) return;
        if (movementLocked) return;

        if (inkform == form.solid || (inkform == form.fluid && !gripping))
        {
            if (jumpCoolDownTimer > 0) jumpCoolDownTimer -= Time.deltaTime;

            if (m_PlayerJump.WasPerformedThisFrame() && jumpCount < maxJump && jumpCoolDownTimer <= 0 && !isParalyzed)
            {
                S_GameEvent.PlaySFX(jumpClip);
                slopeAssistDisabledTimer = slopeAssistDisableTime;
                b_Rig.linearVelocity = new Vector2(b_Rig.linearVelocity.x, 0);
                b_Rig.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
                jumpCount++;
                jumpCoolDownTimer = jumpCoolDownTime;
            }

            if (m_PlayerSprint.WasPerformedThisFrame() && !isSprintCharging)
            {
                BeginSprintCharge();
            }

            if (fluidClimbSkill != null && fluidClimbSkill.GetSurface() == S_fluid_climb.SurfaceType.Floor)
            {
                jumpCount = 0;
            }
        }
    }

    void FixedUpdate()
    {
        if (isCameraControlActive)
        {
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

        if (inkform == form.solid)
            SolidMovement();
        else
            FluidMovement();

        if (isSprintCharging)
            UpdateSprintCharge();

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
            b_Rig.linearVelocity = ShouldUseSlopeMovement(moveV)
                ? GetSlopeVelocity(moveV, speed)
                : new Vector2(moveV * speed, b_Rig.linearVelocity.y);
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

    private void UpdateSlopeAssistTimer()
    {
        if (slopeAssistDisabledTimer > 0f)
            slopeAssistDisabledTimer -= Time.fixedDeltaTime;
    }

    private void SampleWalkableGround()
    {
        isGroundedOnWalkableSurface = false;
        groundNormal = Vector2.up;

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
            float upDot = Vector2.Dot(normal, Vector2.up);
            if (upDot < bestDot)
                continue;

            bestDot = upDot;
            groundNormal = normal;
            isGroundedOnWalkableSurface = true;
        }
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

    private void HandleCameraControlInput()
    {
        if (m_PlayerCameraControl == null)
            return;

        if (!isCameraControlActive && m_PlayerCameraControl.WasPerformedThisFrame())
        {
            S_CameraControlSkill skill = S_SkillTree.Instance != null
                ? S_SkillTree.Instance.GetCameraControlSkill()
                : null;

            if (skill != null)
                skill.Activate(this);
        }

        if (isCameraControlActive && m_PlayerCameraControl.WasReleasedThisFrame())
        {
            EndCameraControl();
        }
    }

    public void BeginCameraControl(S_CameraControlSkill skill)
    {
        if (isCameraControlActive || skill == null)
            return;

        if (isParalyzed || movementLocked || isSprintCharging || Mathf.Approximately(Time.timeScale, 0f))
            return;

        cameraControlSkill = skill;
        isCameraControlActive = true;
        gripping = false;

        if (cameraController == null)
            cameraController = FindAnyObjectByType<S_CameraMove>();

        timeScaleBeforeCameraControl = Time.timeScale;
        fixedDeltaTimeBeforeCameraControl = Time.fixedDeltaTime;

        float bulletScale = cameraControlSkill.BulletTimeScale;
        Time.timeScale = timeScaleBeforeCameraControl * bulletScale;
        Time.fixedDeltaTime = fixedDeltaTimeBeforeCameraControl * bulletScale;

        if (cameraController != null)
            cameraController.BeginManualControl();
    }

    private void CameraControlTick()
    {
        if (cameraController != null)
            cameraController.ManualControlTick(m_PlayerMove.ReadValue<Vector2>());
    }

    private void EndCameraControl()
    {
        if (!isCameraControlActive)
            return;

        isCameraControlActive = false;

        Time.timeScale = timeScaleBeforeCameraControl;
        Time.fixedDeltaTime = fixedDeltaTimeBeforeCameraControl;

        if (cameraController != null)
            cameraController.EndManualControl();

        cameraControlSkill = null;
    }

    void OnEnable()
    {
        S_InputBindingManager.Instance.Actions.Player.Enable();
    }


    void OnDisable()
    {
        StopSprintChargeSfx();
        EndCameraControl();
        S_InputBindingManager.Instance.Actions.Player.Disable();
    }

    // Sprint Charge System
    private void BeginSprintCharge()
    {
        if (isSprintCharging) return;
        if (isParalyzed) return;
        if (sprintCooldownRemaining > 0) return;

        sprintSkill = S_SkillTree.Instance != null ? S_SkillTree.Instance.GetSprintSkill() : null;
        if (sprintSkill == null) return;
        if (!sprintSkill.availableSolid && !getForm()) return;
        if (!sprintSkill.availableFluid && getForm()) return;

        isSprintCharging = true;
        sprintChargeTimer = 0f;
        sprintChargeStage = 0;
        previousChargeStage = 0;
        chargeShakeTimer = 0f;
        chargeScaleMultiplier = 1f;
        chargeVisualsActive = false;
    }

    private void UpdateSprintCharge()
    {
        if (!isSprintCharging) return;

        sprintChargeTimer += Time.fixedDeltaTime;

        if (!chargeVisualsActive && sprintChargeTimer >= sprintSkill.BufferTime)
        {
            chargeVisualsActive = true;
            originalPhysicsMaterial = b_Rig.sharedMaterial;
            if (sprintSkill.ChargeBallMaterial != null)
                b_Rig.sharedMaterial = sprintSkill.ChargeBallMaterial;

            b_Rig.freezeRotation = false;
            chargeRotationUnlocked = true;

            if (proceduralRenderer != null)
                proceduralRenderer.SetChargeOverride(true);

            S_GameEvent.PlaySFX(sprintSkill.ChargeStartClip);
            PlaySprintChargeStageSfx(0);
        }

        if (!chargeVisualsActive) return;

        sprintChargeStage = sprintSkill.GetStage(sprintChargeTimer);

        if (sprintChargeStage != previousChargeStage)
        {
            chargeShakeTimer = 0f;
            previousChargeStage = sprintChargeStage;
            PlaySprintChargeStageSfx(sprintChargeStage);
        }

        chargeShakeTimer += Time.fixedDeltaTime;
        float baseScale = sprintSkill.GetStageScale(sprintChargeTimer);
        float shakeOffset = sprintSkill.GetShakeOffset(chargeShakeTimer);
        chargeScaleMultiplier = baseScale + shakeOffset;

        if (useDynamicCollider && dynamicCollider != null)
            dynamicCollider.SetChargeOverride(true, chargeScaleMultiplier);

        b_Rig.gravityScale = solidGravityScale;
    }

    public void ReleaseSprintCharge()
    {
        if (!isSprintCharging) return;

        float releaseDirection = facingRight ? 1f : -1f;

        if (!chargeVisualsActive)
        {
            sprintCooldownRemaining = sprintSkill.GetCooldown(0);
            StopSprintChargeSfx();
            sprintSkill.ActivateCharge(this, sprintSkill.MinSprintSpeed, releaseDirection);
            PlaySprintReleaseSfx();
            isSprintCharging = false;
            sprintChargeTimer = 0f;
            return;
        }

        float charge01 = Mathf.Clamp01(sprintChargeTimer / sprintSkill.MaxChargeTime);
        float finalSpeed = Mathf.Lerp(sprintSkill.MinSprintSpeed, sprintSkill.MaxSprintSpeed, charge01);

        sprintCooldownRemaining = sprintSkill.GetCooldown(sprintChargeStage);
        StopSprintChargeSfx();
        sprintSkill.ActivateCharge(this, finalSpeed, releaseDirection);
        PlaySprintReleaseSfx();

        isSprintCharging = false;
        chargeVisualsActive = false;
        chargeScaleMultiplier = 1f;
        sprintChargeTimer = 0f;
        sprintChargeStage = 0;
        chargeShakeTimer = 0f;

        if (chargeRotationUnlocked)
        {
            b_Rig.freezeRotation = true;
            b_Rig.rotation = 0f;
            chargeRotationUnlocked = false;
        }

        b_Rig.sharedMaterial = originalPhysicsMaterial;

        if (proceduralRenderer != null)
            proceduralRenderer.SetChargeOverride(false);

        if (useDynamicCollider && dynamicCollider != null)
            dynamicCollider.SetChargeOverride(false, 1f);
    }

    public void CancelSprintCharge()
    {
        if (!isSprintCharging && !chargeVisualsActive)
            return;

        bool restoreChargeVisuals = chargeVisualsActive;

        StopSprintChargeSfx();
        isSprintCharging = false;
        chargeVisualsActive = false;
        chargeScaleMultiplier = 1f;
        sprintChargeTimer = 0f;
        sprintChargeStage = 0;
        previousChargeStage = 0;
        chargeShakeTimer = 0f;

        if (chargeRotationUnlocked)
        {
            b_Rig.freezeRotation = true;
            b_Rig.rotation = 0f;
            chargeRotationUnlocked = false;
        }

        if (restoreChargeVisuals)
        {
            b_Rig.sharedMaterial = originalPhysicsMaterial;

            if (proceduralRenderer != null)
                proceduralRenderer.SetChargeOverride(false);

            if (useDynamicCollider && dynamicCollider != null)
                dynamicCollider.SetChargeOverride(false, 1f);
        }
    }

    private void PlaySprintChargeStageSfx(int stage)
    {
        if (sprintSkill == null || sprintChargeSource == null)
            return;

        AudioClip clip = sprintSkill.GetChargeStageClip(stage);
        if (clip == null)
            return;

        float pitch = sprintSkill.GetChargeStagePitch(stage);
        StopSprintChargeSfx();

        sprintChargeSource.clip = clip;
        sprintChargeSource.pitch = Mathf.Max(0.01f, pitch);
        sprintChargeSource.loop = true;
        sprintChargeSource.Play();
    }

    private void PlaySprintReleaseSfx()
    {
        if (sprintSkill == null)
            return;

        S_GameEvent.PlaySFX(sprintSkill.GetChargeReleaseClip());
    }

    private void StopSprintChargeSfx()
    {
        if (sprintChargeSource == null)
            return;

        if (sprintChargeSource.isPlaying)
            sprintChargeSource.Stop();

        sprintChargeSource.clip = null;
        sprintChargeSource.pitch = 1f;
    }

    private void SetupSprintChargeAudioSource()
    {
        GameObject sourceHost = body != null ? body : gameObject;
        sprintChargeSource = sourceHost.AddComponent<AudioSource>();
        sprintChargeSource.playOnAwake = false;
        sprintChargeSource.loop = true;
        sprintChargeSource.spatialBlend = 0f;
        sprintChargeSource.pitch = 1f;
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
