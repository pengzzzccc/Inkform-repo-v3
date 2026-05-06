using UnityEngine;

/// <summary>
/// Enemy guard with full state machine: Patrol, Chase, Aim, Attack, Arrest, Stunned.
/// Aim: stop → wait → fire one shot → return to Chase immediately.
/// Attack: keep distance only (no firing). Firing happens only in Aim.
/// Arrest: timed chase (arrestDuration seconds), game over if player paralyzed within arrestRange.
/// </summary>
public class S_NPCEnemy : S_NPCbase
{
    [Header("State Machine")]
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float loseRange = 12f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float arrestRange = 1.5f;
    [SerializeField] private float stunDuration = 3f;

    [Header("Aim (Sniper Shot)")]
    [SerializeField] private float attackWindupTime = 0.5f;
    [SerializeField] private float aimCooldownTime = 2f;

    [Header("Arrest (Timed Chase)")]
    [SerializeField] private float arrestDuration = 3f;

    [Header("State Colors")]
    [SerializeField] private Color aimColor = new Color(1f, 0.65f, 0f, 1f);
    [SerializeField] private Color arrestColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

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

    private enum State
    {
        Patrol,
        Chase,
        Aim,
        Attack,
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

    private float waypointWaitTimer = 0f;

    private Transform playerTransform;

    private float diagnosticLogTimer = 0f;
    private const float DIAG_LOG_INTERVAL = 2f;

    private bool projectileHitPlayer = false;

    public bool IsStunned => currentState == State.Stunned;

    protected override void Awake()
    {
        base.Awake();
        if (waypoints == null || waypoints.Length == 0)
        {
            waypoints = new Transform[] { transform };
        }

        // Init cached references
        ValidatePlayerReference();

    }

    private void Update()
    {
        if (!isActive || currentState == State.Disabled)
            return;

        // Robust player reference — handles scene reload / execution order issues
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

        UpdateStateMachine();
        ExecuteMovement();
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

        // Always refresh to body transform — the root GameObject doesn't move
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
            case State.Attack:
                UpdateAttackTransitions();
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
        if (projectileHitPlayer)
        {
            EnterState(State.Arrest);
            return;
        }

        if (S_SuspicionSystem.PlayerHidden)
        {
            return;
        }

        float dist = DistanceToPlayer();
        if (dist <= chaseRange)
        {
            EnterState(State.Chase);
        }
    }

    private void UpdateChaseTransitions()
    {
        // Projectile hit while returning to Chase → Arrest
        if (projectileHitPlayer)
        {
            EnterState(State.Arrest);
            return;
        }

        if (S_SuspicionSystem.PlayerHidden)
        {
            EnterState(State.Patrol);
            return;
        }

        float dist = DistanceToPlayer();

        if (dist > loseRange)
        {
            EnterState(State.Patrol);
            return;
        }

        // Aim: stop, wind up 0.5s, fire one shot (only if cooldown elapsed)
        if (dist <= attackRange && aimCooldownTimer <= 0f)
        {
            EnterState(State.Aim);
            return;
        }

        // Attack: player too close — back off (no firing)
        if (dist < attackRange * 0.6f)
        {
            EnterState(State.Attack);
        }
    }

    private void UpdateAimTransitions()
    {
        if (S_SuspicionSystem.PlayerHidden)
        {
            EnterState(State.Patrol);
            return;
        }

        float dist = DistanceToPlayer();

        if (dist > loseRange)
        {
            EnterState(State.Patrol);
            return;
        }

        // If projectile hit player → Arrest
        if (projectileHitPlayer)
        {
            EnterState(State.Arrest);
            return;
        }

        // After firing (fireTimer elapses → shot fired in ExecuteAim), return to Chase
        // ExecuteAim sets fireTimer back to fireRate after firing; we detect "has fired" by
        // checking if we've already fired this Aim cycle.
        // The transition back to Chase happens inside ExecuteAim after the shot.
    }

    private void UpdateAttackTransitions()
    {
        // Projectile hit while backing off → Arrest
        if (projectileHitPlayer)
        {
            EnterState(State.Arrest);
            return;
        }

        if (S_SuspicionSystem.PlayerHidden)
        {
            EnterState(State.Patrol);
            return;
        }

        float dist = DistanceToPlayer();

        if (dist > loseRange)
        {
            EnterState(State.Patrol);
            return;
        }

        // When distance is safe again, go back to Chase
        if (dist >= attackRange * 0.9f)
        {
            EnterState(State.Chase);
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
        if (S_SuspicionSystem.PlayerHidden)
        {
            EnterState(State.Patrol);
            return;
        }

        float dist = DistanceToPlayer();

        if (dist > loseRange)
        {
            EnterState(State.Patrol);
            return;
        }

        // Timer runs in ExecuteArrest
        if (arrestTimer <= 0f)
        {
            // Out of time — go back to Chase
            EnterState(State.Chase);
            return;
        }

        // If player is paralyzed and within arrest range → success
        if (S_Player.Instance != null && S_Player.Instance.IsParalyzed && dist <= arrestRange)
        {
            TriggerArrest();
        }
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
                projectileHitPlayer = false;
                break;
            case State.Attack:
                fireTimer = 0f;
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

        npcSprite.color = currentState switch
        {
            State.Aim => aimColor,
            State.Arrest => arrestColor,
            _ => defaultColor,
        };
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
            case State.Attack:
                ExecuteAttack();
                break;
            case State.Arrest:
                ExecuteArrest();
                break;
            case State.Stunned:
                // No movement, just stay still
                break;
        }
    }

    private void ExecutePatrol()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

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
        transform.Translate(moveDir * (patrolSpeed * Time.deltaTime));
        FlipSprite(moveDir.x);
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
            transform.Translate(moveDir * (chaseSpeed * Time.deltaTime));
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
        if (fireTimer <= 0f && !projectileHitPlayer)
        {
            FireProjectile(toPlayer.normalized);
            // After firing, go back to Chase immediately
            EnterState(State.Chase);
        }
    }

    private void ExecuteAttack()
    {
        if (playerTransform == null)
            return;

        // Face player and back off — no firing
        Vector2 toPlayer = playerTransform.position - transform.position;
        FlipSprite(toPlayer.x);

        float dist = toPlayer.magnitude;
        if (dist < attackRange * 0.9f)
        {
            Vector2 backDir = -toPlayer.normalized;
            transform.Translate(backDir * (chaseSpeed * 0.5f * Time.deltaTime));
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
            // Contact — arrest successful
            TriggerArrest();
            return;
        }

        Vector2 moveDir = toPlayer.normalized;
        transform.Translate(moveDir * (chaseSpeed * Time.deltaTime));
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
    /// Called by S_EMProjectile when the projectile hits the player.
    /// Sets flag that drives Aim → Arrest transition.
    /// </summary>
    public void OnProjectileHitPlayer()
    {
        projectileHitPlayer = true;
    }

    private void TriggerArrest()
    {
        S_GameEvent.ArrestTriggered();
        currentState = State.Disabled;
    }

    /// <summary>
    /// Called externally (e.g., by Sprint) to stun this enemy.
    /// </summary>
    public void Stun()
    {
        if (currentState == State.Disabled || currentState == State.Arrest)
            return;

        EnterState(State.Stunned);
    }

    protected override void HandleGameStart()
    {
        base.HandleGameStart();
        currentState = State.Patrol;
        currentWaypointIndex = 0;
        waypointWaitTimer = 0f;
        fireTimer = 0f;
        stunTimer = 0f;
        arrestTimer = 0f;
        aimCooldownTimer = 0f;
        projectileHitPlayer = false;
        UpdateStateColor();
    }

    protected override void HandleGameRestart()
    {
        S_SuspicionSystem.PlayerHidden = false;
        base.HandleGameRestart();
        currentState = State.Patrol;
        currentWaypointIndex = 0;
        waypointWaitTimer = 0f;
        fireTimer = 0f;
        stunTimer = 0f;
        arrestTimer = 0f;
        aimCooldownTimer = 0f;
        projectileHitPlayer = false;
        UpdateStateColor();
    }

    private void DiagnosticLog()
    {
        Vector3 npcPos = transform.position;
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        string posStr = playerTransform != null
            ? $"dist={Vector2.Distance(npcPos, playerTransform.position):F1}, npcPos=({npcPos.x:F1},{npcPos.y:F1}), playerPos=({playerPos.x:F1},{playerPos.y:F1})"
            : (S_Player.Instance != null ? "playerTransform null but S_Player.Instance != null" : "playerTransform & Instance both null");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw ranges in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, arrestRange);

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
}