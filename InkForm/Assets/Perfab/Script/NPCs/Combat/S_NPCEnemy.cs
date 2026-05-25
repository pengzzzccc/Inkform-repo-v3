using UnityEngine;

using System.Collections.Generic;

/// <summary>
/// Enemy guard with full state machine: Patrol, Chase, Aim, Arrest, Stunned.
/// Aim stops and fires one shot once started, even if the player leaves range.
/// Arrest is a timed chase window; if it fails, the guard returns to Chase.
/// </summary>
public class S_NPCEnemy : S_NPCbase
{
    private static readonly HashSet<S_NPCEnemy> activeEnemies = new HashSet<S_NPCEnemy>();

    [Header("State Machine")]
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float loseRange = 12f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float arrestRange = 1.5f;
    [SerializeField] private float stunDuration = 3f;

    [Header("Vision Detection")]
    [SerializeField] private float detectionViewDistance = 30f;
    [SerializeField] private Vector2 visionOriginOffset = new Vector2(0f, 0.5f);
    [SerializeField] private bool drawVisionGizmos = true;

    [Header("Aim (Sniper Shot)")]
    [SerializeField] private float attackWindupTime = 0.5f;
    [SerializeField] private float aimCooldownTime = 2f;

    [Header("Arrest (Timed Chase)")]
    [SerializeField] private float arrestDuration = 3f;

    [Header("State Colors")]
    [SerializeField] private Color aimColor = new Color(1f, 0.65f, 0f, 1f);
    [SerializeField] private Color arrestColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Facing Visual")]
    [SerializeField] private bool showFacingIndicator = true;
    [SerializeField] private Color facingIndicatorColor = new Color(0.1f, 0.95f, 1f, 1f);
    [SerializeField] private Vector2 facingIndicatorOffset = new Vector2(0.35f, 0.25f);
    [SerializeField] private float facingIndicatorSize = 0.18f;
    [SerializeField] private float facingIndicatorLineWidth = 0.035f;

    [Header("Health")]
    [SerializeField, Min(1)] private int hitsToDie = 2;
    [SerializeField] private Color hitVisualColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float hitVisualDuration = 0.15f;
    [SerializeField] private bool hideSpriteOnDeath = true;
    [SerializeField] private bool disableColliderOnDeath = true;

    [Header("Attack (EM Projectile)")]
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private Transform firePoint;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waypointWaitTime = 1f;

    [Header("Movement Speed")]
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Movement Mode")]
    [SerializeField] private bool useRigidbodyMovement = true;
    [SerializeField] private bool requireGroundForTransformMovement = true;
    [SerializeField] private float transformGroundProbeDistance = 0.15f;
    [SerializeField] private float transformGravity = 25f;
    [SerializeField] private float transformMaxFallSpeed = 12f;
    [SerializeField] private float transformCollisionSkin = 0.02f;

    [Header("Ground Detection")]
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float groundNormalThreshold = 0.5f;

    [Header("Sprint Knockback")]
    [SerializeField] private float sprintKnockbackSpeed = 8f;
    [SerializeField] private float sprintKnockbackDuration = 0.25f;
    [SerializeField] private float sprintKnockbackDamping = 24f;
    [SerializeField] private LayerMask knockbackObstacleLayer = ~0;
    [SerializeField] private float knockbackObstacleSkin = 0.02f;

    [Header("SFX for All")]
    [SerializeField] private AudioClip[] allAudio;

    [Header("Idle Wandering")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderWalkTimeMin = 1f;
    [SerializeField] private float wanderWalkTimeMax = 3f;
    [SerializeField] private float wanderPauseTimeMin = 0.5f;
    [SerializeField] private float wanderPauseTimeMax = 2f;

    [Header("Jump Ability")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCooldown = 1.0f;
    [SerializeField] private float obstacleDetectDistance = 1.0f;
    [SerializeField] private float gapDetectDistance = 1.5f;
    [SerializeField] private float gapDetectHeight = 0.5f;
    [SerializeField] private float gapScanStep = 0.3f;
    [SerializeField] private int gapScanMaxSteps = 10;
    [SerializeField] private float airControlFactor = 0.5f;
    [SerializeField] private float playerAboveThreshold = 1f;
    [SerializeField] private float playerAboveMaxHeight = 4f;
    [SerializeField] private float maxJumpHorizontalBoost = 3f;
    [SerializeField] private LayerMask jumpObstacleLayer = ~0;

    private enum State
    {
        Patrol,
        Chase,
        Aim,
        Arrest,
        Stunned,
        Disabled
    }

    private State currentState = State.Patrol;

    private int currentWaypointIndex = 0;
    private float fireTimer = 0f;
    private float stunTimer = 0f;
    private float arrestTimer = 0f;
    private float aimCooldownTimer = 0f;
    private int currentHitCount = 0;
    private float hitVisualTimer = 0f;

    private float waypointWaitTimer = 0f;

    private Transform playerTransform;
    private bool isPlayerHidden;

    private float diagnosticLogTimer = 0f;
    private const float DIAG_LOG_INTERVAL = 2f;

    // Ground detection & idle wandering
    private Vector2 spawnPosition;
    private bool isGrounded;
    private ContactPoint2D[] groundContacts;
    private RaycastHit2D[] transformGroundHits;
    private RaycastHit2D[] knockbackHits;
    private Vector2 knockbackVelocity;
    private float knockbackTimer = 0f;
    private float transformVerticalVelocity = 0f;

    // Idle wandering timers
    private float wanderWalkTimer;
    private float wanderPauseTimer;
    private Vector2 wanderDirection;

    // Jump ability
    private float jumpCooldownTimer;
    private bool shouldJump;
    private float cachedJumpForce;
    private float cachedHorizBoost;
    private RaycastHit2D[] jumpScanHits;
    private bool originalSpriteEnabled = true;
    private bool originalColliderEnabled = true;
    private RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
    private RigidbodyConstraints2D originalConstraints = RigidbodyConstraints2D.None;
    private float originalGravityScale = 0f;
    private LineRenderer facingIndicatorLine;

    public bool IsStunned => currentState == State.Stunned;
    private bool UseRigidbodyMovement => npcRig != null && useRigidbodyMovement;
    private LayerMask TransformCollisionMask
    {
        get
        {
            LayerMask mask = default;
            mask.value = groundLayer.value | knockbackObstacleLayer.value;
            return mask;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        spawnPosition = transform.position;
        originalSpriteEnabled = npcSprite == null || npcSprite.enabled;
        originalColliderEnabled = npcCol == null || npcCol.enabled;

        if (npcRig != null)
        {
            npcRig.interpolation = RigidbodyInterpolation2D.Interpolate;
            npcRig.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (useRigidbodyMovement)
            {
                npcRig.gravityScale = gravityScale;
            }
            else
            {
                npcRig.bodyType = RigidbodyType2D.Kinematic;
                npcRig.gravityScale = 0f;
                npcRig.linearVelocity = Vector2.zero;
            }

            originalBodyType = npcRig.bodyType;
            originalConstraints = npcRig.constraints;
            originalGravityScale = npcRig.gravityScale;
        }

        ValidatePlayerReference();
        EnsureFacingIndicator();
        UpdateFacingIndicator();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isPlayerHidden = S_SuspicionSystem.PlayerHidden;
        S_GameEvent.OnPlayerHiddenChanged += HandlePlayerHiddenChanged;
        activeEnemies.Add(this);
    }

    protected override void OnDisable()
    {
        S_GameEvent.OnPlayerHiddenChanged -= HandlePlayerHiddenChanged;
        activeEnemies.Remove(this);
        base.OnDisable();
    }

    private void HandlePlayerHiddenChanged(bool hidden)
    {
        isPlayerHidden = hidden;
    }

    private void Update()
    {
        if (!isActive || currentState == State.Disabled)
            return;

        // Robust player reference 鈥?handles scene reload / execution order issues
        ValidatePlayerReference();

        // Diagnostic log every 2 seconds
        diagnosticLogTimer -= Time.deltaTime;
        if (diagnosticLogTimer <= 0f)
        {
            diagnosticLogTimer = DIAG_LOG_INTERVAL;
            DiagnosticLog();
        }

        // Tick cooldowns everywhere
        if (aimCooldownTimer > 0f)
            aimCooldownTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;

        UpdateStateMachine();
        UpdateHitVisual();
        UpdateFacingIndicator();
    }

    private void FixedUpdate()
    {
        if (!isActive || currentState == State.Disabled)
            return;

        ValidatePlayerReference();
        UpdateGroundCheck();
        if (ExecuteKnockback())
        {
            UpdateTransformVerticalMovement();
            return;
        }

        EvaluateJump();
        ExecuteJump();
        ExecuteMovement();
        UpdateTransformVerticalMovement();
    }

    /// <summary>
    /// Ensure playerTransform points to the player's body (Rigidbody2D).
    /// The body is the physically-moving child, not the root S_Player GameObject.
    /// </summary>
    private void ValidatePlayerReference()
    {
        if (S_Player.Instance == null)
        {
            playerTransform = null;
            return;
        }

        // Always refresh to body transform 鈥?the root GameObject doesn't move
        playerTransform = S_Player.Instance.GetBodyTransform();
    }

    private void UpdateStateMachine()
    {
        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrolTransitions();
                break;
            case State.Chase:
                UpdateChaseTransitions();
                break;
            case State.Aim:
                UpdateAimTransitions();
                break;
            case State.Stunned:
                UpdateStunnedTransitions();
                break;
            case State.Arrest:
                UpdateArrestTransitions();
                break;
        }
    }

    private void UpdatePatrolTransitions()
    {
        if (CanSeePlayer(detectionViewDistance))
        {
            EnterState(State.Chase);
        }
    }

    private void UpdateChaseTransitions()
    {
        bool canSeePlayer = CanSeePlayer(detectionViewDistance);
        if (!canSeePlayer)
        {
            EnterState(State.Patrol);
            return;
        }

        float dist = DistanceToPlayer();

        // Aim: stop, wind up 0.5s, fire one shot (only if cooldown elapsed)
        if (dist <= attackRange && aimCooldownTimer <= 0f)
        {
            EnterState(State.Aim);
            return;
        }

    }

    private void UpdateAimTransitions()
    {
        if (!CanSeePlayer(detectionViewDistance))
        {
            EnterState(State.Patrol);
            return;
        }
    }

    private void UpdateStunnedTransitions()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            EnterState(State.Patrol);
        }
    }

    private void UpdateArrestTransitions()
    {
        if (!CanSeePlayer(detectionViewDistance))
        {
            EnterState(State.Patrol);
            return;
        }

        float dist = DistanceToPlayer();

        // Timer runs in ExecuteArrest
        if (arrestTimer <= 0f)
        {
            // Out of time 鈥?go back to Chase
            EnterState(State.Chase);
            return;
        }

        // If player is paralyzed and within arrest range 鈫?success
        if (S_Player.Instance != null && S_Player.Instance.IsParalyzed && dist <= arrestRange)
        {
            TriggerArrest();
        }
    }

    private bool CanSeePlayer(float maxViewDistance)
    {
        if (isPlayerHidden || playerTransform == null)
            return false;

        Vector2 origin = GetVisionOrigin();
        Vector2 target = playerTransform.position;
        Vector2 toPlayer = target - origin;

        if (toPlayer.sqrMagnitude <= 0.0001f)
            return true;

        if (maxViewDistance > 0f && toPlayer.sqrMagnitude > maxViewDistance * maxViewDistance)
            return false;

        if (Mathf.Abs(toPlayer.x) > 0.01f)
        {
            bool playerOnRight = toPlayer.x > 0f;
            if (playerOnRight != (GetFacingDirectionX() > 0f))
                return false;
        }

        RaycastHit2D[] hits = Physics2D.LinecastAll(origin, target);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            if (hitCollider == npcCol || hitCollider.transform == playerTransform || hitCollider.attachedRigidbody != null && hitCollider.attachedRigidbody.transform == playerTransform)
                continue;

            if (hitCollider.GetComponentInParent<S_Player>() != null)
                continue;

            if (hitCollider.isTrigger && hitCollider.GetComponentInParent<S_HideSpot>() == null)
                continue;

            if (hitCollider.GetComponentInParent<S_HideSpot>() != null)
                return false;
        }

        return true;
    }

    private Vector2 GetVisionOrigin()
    {
        return transform.TransformPoint(visionOriginOffset);
    }

    private float GetFacingDirectionX()
    {
        if (Application.isPlaying)
            return facingRight ? 1f : -1f;

        return transform.localScale.x >= 0f ? 1f : -1f;
    }

    private void EnterState(State newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        UpdateStateColor();

        switch (newState)
        {
            case State.Patrol:
                fireTimer = 0f;
                break;
            case State.Chase:
                // Start Aim cooldown to prevent immediate re-Aim
                aimCooldownTimer = aimCooldownTime;
                break;
            case State.Aim:
                fireTimer = attackWindupTime;
                break;
            case State.Stunned:
                stunTimer = stunDuration;
                break;
            case State.Arrest:
                arrestTimer = arrestDuration;
                break;
        }
    }

    private void UpdateStateColor()
    {
        if (npcSprite == null) return;
        if (hitVisualTimer > 0f)
        {
            npcSprite.color = hitVisualColor;
            return;
        }

        npcSprite.color = currentState switch
        {
            State.Aim => aimColor,
            State.Arrest => arrestColor,
            _ => defaultColor,
        };
    }

    /// <summary>Set horizontal velocity if grounded; freeze X otherwise (let gravity drop).</summary>
    private void MoveHorizontally(float speedX)
    {
        if (UseRigidbodyMovement)
        {
            if (isGrounded)
                npcRig.linearVelocity = new Vector2(speedX, npcRig.linearVelocity.y);
            else
                npcRig.linearVelocity = new Vector2(speedX * airControlFactor, npcRig.linearVelocity.y);
            return;
        }

        if (!requireGroundForTransformMovement || isGrounded)
            MoveTransformWithCollision(new Vector2(speedX * Time.deltaTime, 0f));
    }

    private void UpdateGroundCheck()
    {
        if (npcCol == null)
        {
            isGrounded = true;
            return;
        }

        if (!UseRigidbodyMovement)
        {
            UpdateTransformGroundCheck();
            return;
        }

        groundContacts ??= new ContactPoint2D[3];
        int count = npcCol.GetContacts(groundContacts);
        isGrounded = false;
        for (int i = 0; i < count; i++)
        {
            if ((groundLayer.value & (1 << groundContacts[i].collider.gameObject.layer)) == 0)
                continue;
            if (Vector2.Dot(groundContacts[i].normal, Vector2.up) > groundNormalThreshold)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void UpdateTransformGroundCheck()
    {
        transformGroundHits ??= new RaycastHit2D[3];

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = groundLayer;
        filter.useTriggers = false;

        int count = npcCol.Cast(Vector2.down, filter, transformGroundHits, transformGroundProbeDistance);
        isGrounded = false;
        for (int i = 0; i < count; i++)
        {
            if (transformGroundHits[i].collider == null) continue;
            if (Vector2.Dot(transformGroundHits[i].normal, Vector2.up) <= groundNormalThreshold) continue;

            isGrounded = true;
            break;
        }
    }

    private void StopHorizontalMovement()
    {
        if (UseRigidbodyMovement)
            npcRig.linearVelocity = new Vector2(0f, npcRig.linearVelocity.y);
    }

    private bool MoveTransformWithCollision(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f) return true;

        if (npcCol != null)
        {
            knockbackHits ??= new RaycastHit2D[4];
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = TransformCollisionMask;
            filter.useTriggers = false;

            int hitCount = npcCol.Cast(delta.normalized, filter, knockbackHits, delta.magnitude + knockbackObstacleSkin);
            float allowedDistance = delta.magnitude;
            for (int i = 0; i < hitCount; i++)
            {
                if (knockbackHits[i].collider == null) continue;
                if (Vector2.Dot(knockbackHits[i].normal, -delta.normalized) <= 0.5f) continue;

                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, knockbackHits[i].distance - transformCollisionSkin));
            }

            if (allowedDistance <= 0f)
                return false;

            delta = delta.normalized * allowedDistance;
        }

        transform.position += (Vector3)delta;
        return true;
    }

    private void UpdateTransformVerticalMovement()
    {
        if (UseRigidbodyMovement)
            return;

        if (npcCol == null)
        {
            isGrounded = true;
            transformVerticalVelocity = 0f;
            return;
        }

        if (isGrounded && transformVerticalVelocity <= 0f)
        {
            transformVerticalVelocity = 0f;
            SnapTransformToGround();
            return;
        }

        transformVerticalVelocity = Mathf.Max(transformVerticalVelocity - transformGravity * Time.deltaTime, -transformMaxFallSpeed);
        Vector2 delta = Vector2.up * (transformVerticalVelocity * Time.deltaTime);

        bool moved = MoveTransformWithCollision(delta);
        if (!moved && transformVerticalVelocity < 0f)
        {
            transformVerticalVelocity = 0f;
            isGrounded = true;
        }
    }

    private void SnapTransformToGround()
    {
        if (npcCol == null) return;

        knockbackHits ??= new RaycastHit2D[4];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = groundLayer;
        filter.useTriggers = false;

        float castDistance = transformGroundProbeDistance + transformCollisionSkin;
        int hitCount = npcCol.Cast(Vector2.down, filter, knockbackHits, castDistance);
        float snapDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            if (knockbackHits[i].collider == null) continue;
            if (Vector2.Dot(knockbackHits[i].normal, Vector2.up) <= groundNormalThreshold) continue;

            snapDistance = Mathf.Min(snapDistance, Mathf.Max(0f, knockbackHits[i].distance - transformCollisionSkin));
        }

        if (!float.IsPositiveInfinity(snapDistance) && snapDistance > 0f)
            transform.position += Vector3.down * snapDistance;
    }

    private bool ExecuteKnockback()
    {
        if (knockbackTimer <= 0f)
            return false;

        knockbackTimer -= Time.deltaTime;

        if (UseRigidbodyMovement)
        {
            if (knockbackTimer <= 0f)
                StopHorizontalMovement();

            return true;
        }

        Vector2 delta = knockbackVelocity * Time.deltaTime;
        bool moved = MoveTransformWithCollision(delta);

        knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, sprintKnockbackDamping * Time.deltaTime);

        if (!moved || knockbackTimer <= 0f || knockbackVelocity.sqrMagnitude < 0.01f)
        {
            knockbackTimer = 0f;
            knockbackVelocity = Vector2.zero;
        }

        return true;
    }

    private void ApplySprintKnockback(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = facingRight ? Vector2.right : Vector2.left;

        direction.Normalize();
        direction.y = 0f;

        knockbackTimer = sprintKnockbackDuration;

        if (UseRigidbodyMovement)
        {
            npcRig.linearVelocity = new Vector2(direction.x * sprintKnockbackSpeed, npcRig.linearVelocity.y);
            return;
        }

        knockbackVelocity = direction * sprintKnockbackSpeed;
    }

    private void ExecuteMovement()
    {
        switch (currentState)
        {
            case State.Patrol:
                ExecutePatrol();
                break;
            case State.Chase:
                ExecuteChase();
                break;
            case State.Aim:
                ExecuteAim();
                break;
            case State.Arrest:
                ExecuteArrest();
                break;
            case State.Stunned:
                // No movement, just stay still (still affected by gravity)
                if (UseRigidbodyMovement && !isGrounded)
                    npcRig.linearVelocity = new Vector2(0f, npcRig.linearVelocity.y);
                break;
        }
    }

    private void ExecutePatrol()
    {
        ExecuteIdleWandering();
    }

    private void ExecuteWaypointPatrol()
    {
        Transform target = waypoints[currentWaypointIndex];

        if (waypointWaitTimer > 0f)
        {
            waypointWaitTimer -= Time.deltaTime;
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        if (dist < 0.1f)
        {
            waypointWaitTimer = waypointWaitTime;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            return;
        }

        Vector2 moveDir = toTarget.normalized;
        MoveHorizontally(moveDir.x * patrolSpeed);
        FlipSprite(moveDir.x);
    }

    private void ExecuteIdleWandering()
    {
        // Don't wander in the air 鈥?wait for ground
        if (!isGrounded)
        {
            StopHorizontalMovement();
            return;
        }

        if (wanderPauseTimer > 0f)
        {
            wanderPauseTimer -= Time.deltaTime;
            StopHorizontalMovement();
            return;
        }

        if (wanderWalkTimer > 0f)
        {
            wanderWalkTimer -= Time.deltaTime;

            // Check boundary 鈥?if close to the wander edge, return toward spawn.
            Vector2 toSpawn = spawnPosition - (Vector2)transform.position;
            float distanceFromSpawn = Mathf.Abs(transform.position.x - spawnPosition.x);
            float returnThreshold = Mathf.Max(0f, wanderRadius * 0.85f);
            if (wanderRadius > 0f && distanceFromSpawn >= returnThreshold && Mathf.Abs(toSpawn.x) > 0.01f)
                wanderDirection = toSpawn.x > 0f ? Vector2.right : Vector2.left;

            float moveDir = wanderDirection.x >= 0f ? 1f : -1f;
            if (!HasGroundAhead(moveDir))
            {
                float oppositeDir = -moveDir;
                if (HasGroundAhead(oppositeDir))
                {
                    moveDir = oppositeDir;
                    wanderDirection = moveDir > 0f ? Vector2.right : Vector2.left;
                }
                else
                {
                    StopHorizontalMovement();
                    wanderWalkTimer = 0f;
                    wanderPauseTimer = Random.Range(wanderPauseTimeMin, wanderPauseTimeMax);
                    return;
                }
            }

            MoveHorizontally(moveDir * patrolSpeed);
            FlipSprite(moveDir);

            if (wanderWalkTimer <= 0f)
            {
                StopHorizontalMovement();
                wanderPauseTimer = Random.Range(wanderPauseTimeMin, wanderPauseTimeMax);
            }
            return;
        }

        // Start new walk cycle 鈥?pick a 2D side-scroller direction.
        wanderDirection = Random.value < 0.5f ? Vector2.left : Vector2.right;
        wanderWalkTimer = Random.Range(wanderWalkTimeMin, wanderWalkTimeMax);
    }

    private bool HasGroundAhead(float directionX)
    {
        if (npcCol == null)
            return true;

        float dir = directionX >= 0f ? 1f : -1f;
        float probeDistance = Mathf.Max(0.1f, gapDetectDistance);
        Vector2 origin = (Vector2)transform.position + Vector2.right * dir * probeDistance + Vector2.up * gapDetectHeight;
        float rayDistance = gapDetectHeight * 2f + 1f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayDistance, groundLayer);
        return hit.collider != null;
    }

    private void ExecuteChase()
    {
        if (playerTransform == null)
            return;

        Vector2 toPlayer = playerTransform.position - transform.position;
        Vector2 moveDir = toPlayer.normalized;

        // Stop just outside attackRange
        if (toPlayer.magnitude > attackRange * 0.9f)
        {
            MoveHorizontally(moveDir.x * chaseSpeed);
        }
        FlipSprite(moveDir.x);
    }

    private void ExecuteAim()
    {
        if (playerTransform == null)
            return;

        // Face player and stand still
        Vector2 toPlayer = playerTransform.position - transform.position;
        FlipSprite(toPlayer.x);

        // Wait for windup, then fire one shot
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile(toPlayer.normalized);
            EnterState(State.Chase);
        }
    }

    private void ExecuteArrest()
    {
        if (playerTransform == null)
            return;

        Vector2 toPlayer = playerTransform.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist < 0.3f)
        {
            // Contact 鈥?arrest successful
            TriggerArrest();
            return;
        }

        Vector2 moveDir = toPlayer.normalized;
        MoveHorizontally(moveDir.x * chaseSpeed);
        FlipSprite(moveDir.x);

        arrestTimer -= Time.deltaTime;
    }

    private void FireProjectile(Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        S_EMProjectile em = proj.GetComponent<S_EMProjectile>();
        if (em != null)
        {
            em.Launch(direction, this);
        }
    }

    /// <summary>
    /// Called by S_EMProjectile when any guard projectile hits the player.
    /// The hit alert pushes every active guard into Arrest together.
    /// </summary>
    public void OnProjectileHitPlayer()
    {
        TriggerGlobalArrest();
    }

    private static void TriggerGlobalArrest()
    {
        foreach (S_NPCEnemy enemy in activeEnemies)
        {
            if (enemy == null || !enemy.isActive || enemy.currentState == State.Disabled)
                continue;

            enemy.EnterGlobalArrest();
        }
    }

    private void EnterGlobalArrest()
    {
        ValidatePlayerReference();
        knockbackTimer = 0f;
        knockbackVelocity = Vector2.zero;
        StopHorizontalMovement();
        EnterState(State.Arrest);
    }

    private void TriggerArrest()
    {
        S_GameEvent.ArrestTriggered();
        S_GameEvent.PlayerDied();
        EnterState(State.Disabled);
    }

    /// <summary>
    /// Called externally (e.g., by Sprint) to stun this enemy.
    /// </summary>
    public void Stun()
    {
        if (currentState == State.Disabled || currentState == State.Arrest)
            return;

        knockbackTimer = 0f;
        knockbackVelocity = Vector2.zero;
        StopHorizontalMovement();
        EnterState(State.Stunned);
    }

    /// <summary>
    /// Called by Sprint when this enemy is hit. Works with or without Rigidbody2D.
    /// </summary>
    public void OnSprintHit(Vector2 hitDirection)
    {
        if (currentState == State.Disabled || currentState == State.Arrest)
            return;

        RegisterPlayerHit();
        if (currentState == State.Disabled)
            return;

        EnterState(State.Stunned);
        ApplySprintKnockback(hitDirection);
    }

    private void RegisterPlayerHit()
    {
        currentHitCount++;
        ShowHitVisual();
        PlaykSfx();

        if (currentHitCount >= Mathf.Max(1, hitsToDie))
            Die();
    }

    private void ShowHitVisual()
    {
        hitVisualTimer = hitVisualDuration;

        if (npcSprite != null)
            npcSprite.color = hitVisualColor;
    }

    private void UpdateHitVisual()
    {
        if (hitVisualTimer <= 0f)
            return;

        hitVisualTimer -= Time.deltaTime;
        if (hitVisualTimer <= 0f)
        {
            hitVisualTimer = 0f;
            UpdateStateColor();
        }
        else if (npcSprite != null)
        {
            npcSprite.color = hitVisualColor;
        }
    }

    private void Die()
    {
        knockbackTimer = 0f;
        knockbackVelocity = Vector2.zero;
        transformVerticalVelocity = 0f;

        StopHorizontalMovement();
        if (npcRig != null)
        {
            npcRig.linearVelocity = Vector2.zero;
            npcRig.angularVelocity = 0f;
            npcRig.gravityScale = 0f;
            npcRig.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        EnterState(State.Disabled);
        isActive = false;

        if (hideSpriteOnDeath && npcSprite != null)
            npcSprite.enabled = false;

        if (disableColliderOnDeath && npcCol != null)
            npcCol.enabled = false;

        UpdateFacingIndicator();
    }

    private void RestoreAliveState()
    {
        isActive = true;
        currentHitCount = 0;
        hitVisualTimer = 0f;

        if (npcSprite != null)
        {
            npcSprite.enabled = originalSpriteEnabled;
            npcSprite.color = defaultColor;
        }

        if (npcCol != null)
            npcCol.enabled = originalColliderEnabled;

        if (npcRig != null)
        {
            npcRig.bodyType = originalBodyType;
            npcRig.constraints = originalConstraints;
            npcRig.gravityScale = originalGravityScale;
            npcRig.linearVelocity = Vector2.zero;
            npcRig.angularVelocity = 0f;
        }

        UpdateFacingIndicator();
    }

    protected override void HandleGameStart()
    {
        base.HandleGameStart();
        RestoreAliveState();
        currentWaypointIndex = 0;
        waypointWaitTimer = 0f;
        stunTimer = 0f;
        arrestTimer = 0f;
        aimCooldownTimer = 0f;
        knockbackTimer = 0f;
        knockbackVelocity = Vector2.zero;
        EnterState(State.Patrol);
    }

    protected override void HandleGameRestart()
    {
        isPlayerHidden = false;
        base.HandleGameRestart();
        RestoreAliveState();
        currentWaypointIndex = 0;
        waypointWaitTimer = 0f;
        stunTimer = 0f;
        arrestTimer = 0f;
        aimCooldownTimer = 0f;
        knockbackTimer = 0f;
        knockbackVelocity = Vector2.zero;
        EnterState(State.Patrol);
    }

    private void EvaluateJump()
    {
        if (!canJump || !isGrounded || jumpCooldownTimer > 0f || knockbackTimer > 0f)
        {
            shouldJump = false;
            return;
        }

        if (currentState != State.Chase && currentState != State.Arrest)
        {
            shouldJump = false;
            return;
        }

        float dir = facingRight ? 1f : -1f;
        Vector2 origin = (Vector2)transform.position;

        // 1. Wall ahead
        RaycastHit2D wallHit = Physics2D.Raycast(
            origin + Vector2.up * 0.5f,
            Vector2.right * dir,
            obstacleDetectDistance, jumpObstacleLayer);
        bool wallAhead = wallHit.collider != null && npcCol != null && wallHit.collider != npcCol;

        // 2. Gap ahead - scan forward to find ground
        bool gapAhead = false;
        if (!wallAhead)
        {
            gapAhead = FindLandingSpot(Vector2.right * dir, out Vector2 landing);
            if (gapAhead)
            {
                cachedJumpForce = jumpForce;
                cachedHorizBoost = 0f;
                CalculateJumpParameters(landing, out cachedJumpForce, out cachedHorizBoost);
            }
        }

        // 3. Player above
        bool playerAbove = false;
        if (playerTransform != null)
        {
            float heightDiff = playerTransform.position.y - transform.position.y;
            float horizDist = Mathf.Abs(playerTransform.position.x - transform.position.x);
            playerAbove = heightDiff > playerAboveThreshold && heightDiff < playerAboveMaxHeight && horizDist < chaseRange;
            if (playerAbove)
            {
                cachedJumpForce = jumpForce + heightDiff * 2f;
                cachedHorizBoost = 0f;
            }
        }

        if (wallAhead)
        {
            cachedJumpForce = jumpForce;
            cachedHorizBoost = 0f;
        }

        shouldJump = wallAhead || gapAhead || playerAbove;
    }

    private bool FindLandingSpot(Vector2 moveDir, out Vector2 landingPoint)
    {
        landingPoint = Vector2.zero;
        jumpScanHits ??= new RaycastHit2D[3];
        bool hadGround = false;
        bool foundGap = false;

        for (int i = 1; i <= gapScanMaxSteps; i++)
        {
            Vector2 probeX = (Vector2)transform.position + moveDir * gapScanStep * i;
            Vector2 probeOrigin = probeX + Vector2.up * gapDetectHeight;
            RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, gapDetectHeight * 2f + 1f, groundLayer);

            if (hit.collider != null)
            {
                if (!hadGround)
                    hadGround = true;
                else if (foundGap)
                {
                    landingPoint = hit.point;
                    return true;
                }
            }
            else
            {
                if (hadGround)
                    foundGap = true;
                else if (i > 1)
                {
                    foundGap = true;
                }
            }
        }

        return foundGap;
    }

    private void CalculateJumpParameters(Vector2 landingPoint, out float outJumpForce, out float outHorizBoost)
    {
        float dx = Mathf.Abs(landingPoint.x - transform.position.x);
        float dy = landingPoint.y - transform.position.y;

        outJumpForce = jumpForce;

        if (dy > 0.5f)
            outJumpForce += dy * 2f;

        outHorizBoost = dx > gapDetectDistance ? dx * 1.5f : 0f;
        outHorizBoost = Mathf.Clamp(outHorizBoost, 0f, maxJumpHorizontalBoost);
    }

    private void ExecuteJump()
    {
        if (!shouldJump) return;

        jumpCooldownTimer = jumpCooldown;
        shouldJump = false;

        if (UseRigidbodyMovement)
        {
            cachedHorizBoost = Mathf.Clamp(cachedHorizBoost, 0f, maxJumpHorizontalBoost);
            npcRig.linearVelocity = new Vector2(npcRig.linearVelocity.x, 0f);
            npcRig.AddForce(Vector2.up * cachedJumpForce, ForceMode2D.Impulse);

            if (cachedHorizBoost > 0f)
            {
                float dir = facingRight ? 1f : -1f;
                npcRig.linearVelocity += new Vector2(dir * cachedHorizBoost, 0f);
            }
        }
        else
        {
            transformVerticalVelocity = cachedJumpForce;
            isGrounded = false;
        }
    }

    private void DiagnosticLog()
    {
        Vector3 npcPos = transform.position;
        bool hasWaypoints = waypoints != null && waypoints.Length > 0;
        string pathStr = hasWaypoints ? $"wp=[{currentWaypointIndex}/{waypoints.Length}]" : "wander";
        string posStr = playerTransform != null
            ? $"dist={Vector2.Distance(npcPos, playerTransform.position):F1}, npcPos=({npcPos.x:F1},{npcPos.y:F1}), playerPos=({playerTransform.position.x:F1},{playerTransform.position.y:F1})"
            : (S_Player.Instance != null ? "playerTransform null but S_Player.Instance != null" : "playerTransform & Instance both null");

        // Debug.Log($"[{npcName}] state={currentState} ground={isGrounded} {pathStr} {posStr}");
    }

    private void EnsureFacingIndicator()
    {
        if (facingIndicatorLine != null)
            return;

        Transform existing = transform.Find("FacingIndicator");
        GameObject indicatorObject = existing != null ? existing.gameObject : new GameObject("FacingIndicator");
        indicatorObject.transform.SetParent(transform, false);

        facingIndicatorLine = indicatorObject.GetComponent<LineRenderer>();
        if (facingIndicatorLine == null)
            facingIndicatorLine = indicatorObject.AddComponent<LineRenderer>();

        facingIndicatorLine.useWorldSpace = false;
        facingIndicatorLine.loop = false;
        facingIndicatorLine.positionCount = 4;
        facingIndicatorLine.textureMode = LineTextureMode.Stretch;
        facingIndicatorLine.alignment = LineAlignment.View;
        facingIndicatorLine.startWidth = facingIndicatorLineWidth;
        facingIndicatorLine.endWidth = facingIndicatorLineWidth;
        facingIndicatorLine.startColor = facingIndicatorColor;
        facingIndicatorLine.endColor = facingIndicatorColor;
        facingIndicatorLine.material = new Material(Shader.Find("Sprites/Default"));

        if (npcSprite != null)
        {
            facingIndicatorLine.sortingLayerID = npcSprite.sortingLayerID;
            facingIndicatorLine.sortingOrder = npcSprite.sortingOrder + 1;
        }
    }

    private void UpdateFacingIndicator()
    {
        EnsureFacingIndicator();
        if (facingIndicatorLine == null)
            return;

        bool shouldShow = showFacingIndicator && isActive && currentState != State.Disabled;
        facingIndicatorLine.enabled = shouldShow;
        if (!shouldShow)
            return;

        float dir = facingRight ? 1f : -1f;
        Vector2 basePos = new Vector2(Mathf.Abs(facingIndicatorOffset.x) * dir, facingIndicatorOffset.y);
        float size = Mathf.Max(0.01f, facingIndicatorSize);

        facingIndicatorLine.startWidth = facingIndicatorLineWidth;
        facingIndicatorLine.endWidth = facingIndicatorLineWidth;
        facingIndicatorLine.startColor = facingIndicatorColor;
        facingIndicatorLine.endColor = facingIndicatorColor;

        facingIndicatorLine.SetPosition(0, basePos + new Vector2(-dir * size, size * 0.6f));
        facingIndicatorLine.SetPosition(1, basePos + new Vector2(dir * size, 0f));
        facingIndicatorLine.SetPosition(2, basePos + new Vector2(-dir * size, -size * 0.6f));
        facingIndicatorLine.SetPosition(3, basePos + new Vector2(-dir * size * 0.35f, 0f));
    }

    private void OnDrawGizmosSelected()
    {
        // Draw ranges in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, arrestRange);

        DrawVisionGizmos();

        // Draw waypoint path
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
                    int next = (i + 1) % waypoints.Length;
                    if (waypoints[next] != null)
                        Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
                }
            }
        }
    }

    private void DrawVisionGizmos()
    {
        if (!drawVisionGizmos)
            return;

        Vector2 origin = GetVisionOrigin();
        float dir = GetFacingDirectionX();
        Vector2 forward = Vector2.right * dir;
        Vector2 end = origin + forward * Mathf.Max(0f, detectionViewDistance);

        Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(origin, 0.08f);

        Vector2 arrowBack = end - forward * 0.45f;
        Gizmos.DrawLine(end, arrowBack + Vector2.up * 0.18f);
        Gizmos.DrawLine(end, arrowBack + Vector2.down * 0.18f);

        Transform playerBody = playerTransform;
        if (playerBody == null && S_Player.Instance != null)
            playerBody = S_Player.Instance.GetBodyTransform();

        if (playerBody == null)
            return;

        bool canSee = Application.isPlaying
            ? CanSeePlayer(detectionViewDistance)
            : CanSeePointForGizmos(origin, playerBody.position, detectionViewDistance);

        Gizmos.color = canSee
            ? new Color(0.2f, 1f, 0.25f, 0.9f)
            : new Color(1f, 0.25f, 0.2f, 0.75f);
        Gizmos.DrawLine(origin, playerBody.position);
    }

    private bool CanSeePointForGizmos(Vector2 origin, Vector2 target, float maxViewDistance)
    {
        if (isPlayerHidden)
            return false;

        Vector2 toTarget = target - origin;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return true;

        if (maxViewDistance > 0f && toTarget.sqrMagnitude > maxViewDistance * maxViewDistance)
            return false;

        if (Mathf.Abs(toTarget.x) > 0.01f)
        {
            bool targetOnRight = toTarget.x > 0f;
            if (targetOnRight != (GetFacingDirectionX() > 0f))
                return false;
        }

        RaycastHit2D[] hits = Physics2D.LinecastAll(origin, target);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            if (hitCollider == npcCol)
                continue;

            if (hitCollider.GetComponentInParent<S_Player>() != null)
                continue;

            if (hitCollider.isTrigger && hitCollider.GetComponentInParent<S_HideSpot>() == null)
                continue;

            if (hitCollider.GetComponentInParent<S_HideSpot>() != null)
                return false;
        }

        return true;
    }

    private void PlaykSfx()
    {
        AudioClip clip = GetRandomBreakClip();
        if (clip == null)
            return;

        S_GameEvent.PlaySFX(clip);
    }

    private AudioClip GetRandomBreakClip()
    {
        if (allAudio == null || allAudio.Length == 0)
            return null;

        int validClipCount = 0;
        for (int i = 0; i < allAudio.Length; i++)
        {
            if (allAudio[i] != null)
                validClipCount++;
        }

        if (validClipCount == 0)
            return null;

        int randomIndex = Random.Range(0, validClipCount);
        for (int i = 0; i < allAudio.Length; i++)
        {
            AudioClip clip = allAudio[i];
            if (clip == null)
                continue;

            if (randomIndex == 0)
                return clip;

            randomIndex--;
        }

        return null;
    }
}
