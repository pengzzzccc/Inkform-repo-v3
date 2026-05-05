using UnityEngine;

/// <summary>
/// Enemy guard with full state machine: Patrol, Chase, Attack, Arrest, Stunned.
/// Shoots EM projectiles to paralyze the player before attempting arrest.
/// Loses target when player is hidden (S_HideSpot.PlayerHidden).
/// </summary>
public class S_NPCEnemy : S_NPCbase
{
    [Header("State Machine")]
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float loseRange = 12f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float arrestRange = 1.5f;
    [SerializeField] private float stunDuration = 3f;

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
        Attack,
        Arrest,
        Stunned,
        Disabled
    }

    private State currentState = State.Patrol;

    private int currentWaypointIndex = 0;
    private float fireTimer = 0f;
    private float stunTimer = 0f;

    private float waypointWaitTimer = 0f;

    private Transform playerTransform;

    private float diagnosticLogTimer = 0f;
    private const float DIAG_LOG_INTERVAL = 2f;

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
        Debug.Log("state: " + currentState);
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
        if (S_SuspicionSystem.PlayerHidden)
        {
            // Debug.Log($"[S_NPCEnemy] {npcName}: Patrol — PlayerHidden=true, staying in Patrol");
            return;
        }

        float dist = DistanceToPlayer();
        if (dist <= chaseRange)
        {
            // Debug.Log($"[S_NPCEnemy] {npcName}: TRANSITION Patrol→Chase — dist={dist:F1} <= chaseRange={chaseRange}");
            EnterState(State.Chase);
        }
    }

    private void UpdateChaseTransitions()
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

        if (S_Player.Instance != null && S_Player.Instance.IsParalyzed && dist <= arrestRange)
        {
            EnterState(State.Arrest);
            return;
        }

        if (dist <= attackRange)
        {
            EnterState(State.Attack);
        }
    }

    private void UpdateAttackTransitions()
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

        if (S_Player.Instance != null && S_Player.Instance.IsParalyzed && dist <= arrestRange)
        {
            EnterState(State.Arrest);
            return;
        }

        if (dist > attackRange)
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
        // Arrest is terminal for this encounter — handled in EnterState
    }

    private void EnterState(State newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        switch (newState)
        {
            case State.Patrol:
                fireTimer = 0f;
                break;
            case State.Attack:
                fireTimer = 0f;
                break;
            case State.Stunned:
                stunTimer = stunDuration;
                break;
            case State.Arrest:
                TriggerArrest();
                break;
        }
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

    private void ExecuteAttack()
    {
        if (playerTransform == null)
            return;

        // Face player and stand ground
        Vector2 toPlayer = playerTransform.position - transform.position;
        FlipSprite(toPlayer.x);

        // Keep distance — step back if too close
        float dist = toPlayer.magnitude;
        if (dist < attackRange * 0.6f)
        {
            Vector2 backDir = -toPlayer.normalized;
            transform.Translate(backDir * (chaseSpeed * 0.5f * Time.deltaTime));
        }

        // Fire at rate
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            // Debug.Log($"[S_NPCEnemy] {npcName}: Firing projectile — dir={toPlayer.normalized}, playerPos={playerTransform.position}, npcPos={transform.position}");
            FireProjectile(toPlayer.normalized);
            fireTimer = fireRate;
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
    }

    private void FireProjectile(Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            // Debug.LogWarning($"[S_NPCEnemy] {npcName}: projectilePrefab is null, cannot fire");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        S_EMProjectile em = proj.GetComponent<S_EMProjectile>();
        if (em != null)
        {
            em.Launch(direction);
        }
    }

    private void TriggerArrest()
    {
        // Debug.Log($"[S_NPCEnemy] {npcName}: Player arrested");
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
    }

    private void DiagnosticLog()
    {
        Vector3 npcPos = transform.position;
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        string posStr = playerTransform != null
            ? $"dist={Vector2.Distance(npcPos, playerTransform.position):F1}, npcPos=({npcPos.x:F1},{npcPos.y:F1}), playerPos=({playerPos.x:F1},{playerPos.y:F1})"
            : (S_Player.Instance != null ? "playerTransform null but S_Player.Instance != null" : "playerTransform & Instance both null");
        // Debug.Log($"[S_NPCEnemy] {npcName}: state={currentState}, isActive={isActive}, playerHidden={S_SuspicionSystem.PlayerHidden}, chaseRange={chaseRange}, attackRange={attackRange}, {posStr}");
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