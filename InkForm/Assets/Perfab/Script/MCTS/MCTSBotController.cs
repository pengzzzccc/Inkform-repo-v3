using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MCTS-based automatic player controller for level testing.
/// Attach to the player's body GameObject (the one with Rigidbody2D and Collider2D).
///
/// Does NOT modify S_Player, combat system, enemy AI, animation, or NPC system.
/// Directly controls the player's Rigidbody2D to apply bot decisions.
///
/// Dependencies (auto-found if not assigned):
/// - S_Player.Instance (for physics constants and form state)
/// - S_coleve.Instance (for ground detection)
/// - S_SectionGoal (for goal position)
/// - LevelTestMetrics (optional, for reporting)
/// </summary>
public class MCTSBotController : MonoBehaviour
{
    // ═══════════════════════════════════════════
    //  Inspector Parameters
    // ═══════════════════════════════════════════

    [Header("MCTS Search")]
    [SerializeField] private int iterationsPerDecision = 200;
    [SerializeField] private float decisionInterval = 0.15f;
    [SerializeField] private int maxSimulationDepth = 30;
    [SerializeField] private float explorationConstant = 1.41f;

    [Header("Physics (auto-filled from S_Player if left at 0)")]
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float jumpForce = 0f;
    [SerializeField] private float gravityScale = 0f;
    [SerializeField] private float sprintForce = 20f;

    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float wallCheckDistance = 0.8f;
    [SerializeField] private float hazardCheckRadius = 0.5f;
    [SerializeField] private float goalCheckRadius = 2f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private LayerMask hazardLayer = 0;
    [SerializeField] private string lavaTag = "lava";

    [Header("Stuck Detection")]
    [SerializeField] private float stuckThreshold = 3.0f;
    [SerializeField] private float stuckPositionEpsilon = 0.1f;
    [SerializeField] private float maxTestDuration = 300f;

    [Header("Bot Control")]
    [SerializeField] private bool botEnabled = true;
    [SerializeField] private bool useAvgRewardSelection = false;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoRayLength = 2f;

    // ═══════════════════════════════════════════
    //  Runtime State
    // ═══════════════════════════════════════════

    private Rigidbody2D rb;
    private S_Player player;
    private LevelTestMetrics metrics;

    // Goal
    private Vector2 goalPosition;
    private bool goalFound;

    // Decision timing
    private float decisionTimer;
    private BotAction currentAction;
    private BotAction previousAction;
    private int repeatActionCount;

    // Stuck detection (real-world)
    private Vector2 lastRealPosition;
    private float realStuckTimer;

    // Death tracking
    private bool isDead;

    // Timeout tracking
    private float testStartTime;
    private bool testTimedOut;

    // All possible actions
    private static readonly BotAction[] AllActions = (BotAction[])System.Enum.GetValues(typeof(BotAction));

    // Last MCTS tree root (for debug)
    private MCTSNode lastRoot;
    private string lastDecisionLog = "";

    // Environment queries cache (updated per decision)
    private bool envWallLeft, envWallRight;
    private bool envGroundAheadLeft, envGroundAheadRight;
    private bool envHazardLeft, envHazardRight, envHazardBelow;
    private bool envGoalNearby;

    // ═══════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
    }

    void Start()
    {
        // Find player reference
        player = S_Player.Instance;
        if (player == null)
        {
            Debug.LogError("[MCTSBotController] S_Player.Instance not found! Disabling bot.");
            botEnabled = false;
            return;
        }

        // Auto-fill physics constants from S_Player
        if (moveSpeed <= 0f) moveSpeed = player.GetMoveSpeed();
        if (jumpForce <= 0f) jumpForce = 10f; // Default from S_Player serialized field
        if (gravityScale <= 0f) gravityScale = 4f; // Default solidGravityScale
        if (rb == null) rb = player.GetRigidbody();

        // Find metrics component
        metrics = GetComponent<LevelTestMetrics>();
        if (metrics == null) metrics = GetComponentInParent<LevelTestMetrics>();

        // Find goal
        RefreshGoalPosition();

        // Subscribe to events
        S_GameEvent.OnPlayerDied += HandlePlayerDied;
        S_GameEvent.OnSectionEnd += HandleSectionEnd;

        // Initialize
        lastRealPosition = rb != null ? rb.position : Vector2.zero;
        decisionTimer = 0f;
        testStartTime = Time.realtimeSinceStartup;
        testTimedOut = false;
    }

    void OnDestroy()
    {
        S_GameEvent.OnPlayerDied -= HandlePlayerDied;
        S_GameEvent.OnSectionEnd -= HandleSectionEnd;
    }

    void FixedUpdate()
    {
        if (!botEnabled || isDead) return;
        if (player == null || rb == null) return;

        if (!goalFound)
        {
            RefreshGoalPosition();
            if (!goalFound) return;
        }

        // Check test timeout
        if (Time.realtimeSinceStartup - testStartTime > maxTestDuration)
        {
            if (!testTimedOut)
            {
                testTimedOut = true;
                if (metrics != null) metrics.EndTest();
                if (metrics != null) metrics.LogReport();
                botEnabled = false;
                Debug.Log("[MCTSBotController] Test duration exceeded. Stopping bot and generating report.");
            }
            return;
        }

        // Decision timer
        decisionTimer += Time.fixedDeltaTime;
        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            MakeDecision();
        }

        // Apply current action to real physics
        ApplyActionToPhysics(currentAction);

        // Update stuck detection
        UpdateRealStuckDetection();

        // Update metrics
        if (metrics != null)
        {
            float dist = Vector2.Distance(rb.position, goalPosition);
            metrics.UpdateFinalDistance(dist);
        }
    }

    // ═══════════════════════════════════════════
    //  MCTS Decision Pipeline
    // ═══════════════════════════════════════════

    private void MakeDecision()
    {
        // 1. Snapshot the real game state
        MCTSGameState rootState = SnapshotGameState();

        // 2. Run MCTS search
        MCTSNode root = new MCTSNode(rootState);
        root.InitializeUntriedActions(AllActions);

        for (int i = 0; i < iterationsPerDecision; i++)
        {
            MCTSNode node = root;

            // Selection
            while (!node.IsTerminal && node.IsFullyExpanded)
            {
                MCTSNode next = node.SelectChildUCT(explorationConstant);
                if (next == null) break;
                node = next;
            }

            // Expansion
            if (!node.IsTerminal && node.HasUntriedActions)
            {
                BotAction action = node.PickUntriedAction();
                MCTSGameState childState = node.State.Clone();
                childState.SimulateStep(action, Time.fixedDeltaTime, groundCheckDist: groundCheckDistance,
                    wallCheckDist: wallCheckDistance, hazardCheckRadius: hazardCheckRadius,
                    goalCheckRadius: goalCheckRadius);
                MCTSNode child = node.AddChild(action, childState);
                child.InitializeUntriedActions(AllActions);
                node = child;
            }

            // Simulation (Rollout)
            MCTSGameState rolloutState = node.State.Clone();
            float rolloutReward = 0f;
            float prevDist = rolloutState.distanceToGoal;

            for (int step = 0; step < maxSimulationDepth; step++)
            {
                if (rolloutState.isDead || rolloutState.reachedGoal) break;

                BotAction randomAction = AllActions[Random.Range(0, AllActions.Length)];
                rolloutState.SimulateStep(randomAction, Time.fixedDeltaTime, groundCheckDistance,
                    wallCheckDistance, hazardCheckRadius, goalCheckRadius);

                rolloutReward += rolloutState.ComputeReward(prevDist);
                prevDist = rolloutState.distanceToGoal;
            }

            // Backpropagation
            node.Backpropagate(rolloutReward);
        }

        // 3. Select best action
        MCTSNode bestChild;
        if (useAvgRewardSelection)
            bestChild = root.SelectBestChildByAvgReward();
        else
            bestChild = root.SelectBestChild();

        BotAction chosenAction = bestChild != null ? bestChild.ActionFromParent : BotAction.Idle;

        // 4. Anti-jitter: penalize repeated same action oscillation
        if (chosenAction == previousAction)
        {
            repeatActionCount++;
            if (repeatActionCount > 10 && (chosenAction == BotAction.MoveLeft || chosenAction == BotAction.MoveRight))
            {
                // Force a jump to break out of stuck pattern
                chosenAction = chosenAction == BotAction.MoveLeft ? BotAction.MoveLeftJump : BotAction.MoveRightJump;
                repeatActionCount = 0;
            }
        }
        else
        {
            repeatActionCount = 0;
        }

        previousAction = currentAction;
        currentAction = chosenAction;

        // 5. Record metrics
        if (metrics != null)
        {
            float reward = bestChild != null ? bestChild.AverageReward : 0f;
            metrics.RecordDecision(chosenAction, reward);
        }

        // 6. Debug logging
        lastRoot = root;
        if (debugMode)
        {
            lastDecisionLog = BuildDecisionLog(root, chosenAction);
            Debug.Log(lastDecisionLog);
        }
    }

    // ═══════════════════════════════════════════
    //  State Snapshot & Environment Queries
    // ═══════════════════════════════════════════

    private MCTSGameState SnapshotGameState()
    {
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;

        // Ground detection
        bool grounded = false;
        if (S_coleve.Instance != null)
            grounded = S_coleve.Instance.getPlayerOnGround();

        // Fallback ground check via raycast
        if (!grounded)
        {
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, groundCheckDistance, groundLayer);
            grounded = hit.collider != null;
        }

        // Environment queries
        QueryEnvironment(pos);

        // Distance to goal
        float dist = Vector2.Distance(pos, goalPosition);

        return new MCTSGameState
        {
            position = pos,
            velocity = vel,
            isGrounded = grounded,
            facingRight = player.GetFaceRight(),
            isFluidForm = player.getForm(),
            goalPosition = goalPosition,
            distanceToGoal = dist,
            isDead = false,
            reachedGoal = dist < goalCheckRadius,
            simulatedTime = 0f,
            stuckTime = 0f,
            lastPositionForStuck = pos,
            stuckCheckTimer = 0f,
            moveSpeed = moveSpeed,
            jumpForce = jumpForce,
            gravityScale = gravityScale,
            sprintForce = sprintForce,
            hasWallLeft = envWallLeft,
            hasWallRight = envWallRight,
            hasGroundAheadLeft = envGroundAheadLeft,
            hasGroundAheadRight = envGroundAheadRight,
            hasHazardLeft = envHazardLeft,
            hasHazardRight = envHazardRight,
            hasHazardBelow = envHazardBelow,
            hasGoalNearby = envGoalNearby,
        };
    }

    private void QueryEnvironment(Vector2 pos)
    {
        // Wall detection (left and right)
        envWallLeft = Physics2D.Raycast(pos, Vector2.left, wallCheckDistance, groundLayer).collider != null;
        envWallRight = Physics2D.Raycast(pos, Vector2.right, wallCheckDistance, groundLayer).collider != null;

        // Ground ahead (diagonal down-left and down-right)
        envGroundAheadLeft = Physics2D.Raycast(pos + new Vector2(-0.5f, 0), Vector2.down, groundCheckDistance, groundLayer).collider != null;
        envGroundAheadRight = Physics2D.Raycast(pos + new Vector2(0.5f, 0), Vector2.down, groundCheckDistance, groundLayer).collider != null;

        // Hazard detection (lava tagged objects)
        envHazardLeft = CheckHazardInDirection(pos, Vector2.left, wallCheckDistance);
        envHazardRight = CheckHazardInDirection(pos, Vector2.right, wallCheckDistance);
        envHazardBelow = CheckHazardInDirection(pos, Vector2.down, groundCheckDistance);

        // Goal proximity
        envGoalNearby = Vector2.Distance(pos, goalPosition) < goalCheckRadius * 2f;
    }

    private bool CheckHazardInDirection(Vector2 pos, Vector2 dir, float dist)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, dir, dist);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag(lavaTag))
                return true;

            // Also check hazard layer
            if (hazardLayer != 0 && hit.collider != null && ((1 << hit.collider.gameObject.layer) & hazardLayer) != 0)
                return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════
    //  Action Application
    // ═══════════════════════════════════════════

    private void ApplyActionToPhysics(BotAction action)
    {
        if (rb == null) return;

        Vector2 vel = rb.linearVelocity;
        bool grounded = S_coleve.Instance != null && S_coleve.Instance.getPlayerOnGround();

        switch (action)
        {
            case BotAction.Idle:
                vel.x = 0f;
                break;

            case BotAction.MoveLeft:
                vel.x = -moveSpeed;
                break;

            case BotAction.MoveRight:
                vel.x = moveSpeed;
                break;

            case BotAction.Jump:
                vel.x = 0f;
                if (grounded)
                    rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                break;

            case BotAction.MoveLeftJump:
                vel.x = -moveSpeed;
                if (grounded)
                    rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                break;

            case BotAction.MoveRightJump:
                vel.x = moveSpeed;
                if (grounded)
                    rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                break;

            case BotAction.SprintLeft:
                rb.AddForce(new Vector2(-sprintForce, 0), ForceMode2D.Impulse);
                break;

            case BotAction.SprintRight:
                rb.AddForce(new Vector2(sprintForce, 0), ForceMode2D.Impulse);
                break;

            case BotAction.GripUp:
                vel.x = 0f;
                vel.y = moveSpeed;
                break;

            case BotAction.GripDown:
                vel.x = 0f;
                vel.y = -moveSpeed;
                break;

            case BotAction.GripLeft:
                vel.x = -moveSpeed;
                break;

            case BotAction.GripRight:
                vel.x = moveSpeed;
                break;

            case BotAction.Hide:
                vel = Vector2.zero;
                break;
        }

        // Only set velocity for non-impulse actions
        switch (action)
        {
            case BotAction.Idle:
            case BotAction.MoveLeft:
            case BotAction.MoveRight:
            case BotAction.GripUp:
            case BotAction.GripDown:
            case BotAction.GripLeft:
            case BotAction.GripRight:
            case BotAction.Hide:
                rb.linearVelocity = vel;
                break;
                // Jump and Sprint use AddForce, don't overwrite velocity
        }
    }

    // ═══════════════════════════════════════════
    //  Goal Detection
    // ═══════════════════════════════════════════

    private void RefreshGoalPosition()
    {
        // Try to find S_SectionGoal with End type
        S_SectionGoal[] goals = FindObjectsByType<S_SectionGoal>(FindObjectsSortMode.None);
        foreach (var g in goals)
        {
            // SectionGoal with triggerType End is the goal
            // We access via reflection-free approach: check the transform position
            // Since triggerType is private, we look for the last SectionGoal in the scene
            // or use the one closest to the player's forward direction
            goalPosition = g.transform.position;
            goalFound = true;
        }

        // Fallback: try to find objects tagged "Goal" or "Exit"
        if (!goalFound)
        {
            GameObject goalObj = GameObject.FindGameObjectWithTag("Goal");
            if (goalObj == null) goalObj = GameObject.FindGameObjectWithTag("Exit");
            if (goalObj != null)
            {
                goalPosition = goalObj.transform.position;
                goalFound = true;
            }
        }

        if (!goalFound)
        {
            // Last resort: look for S_ExitGate
            S_ExitGate exitGate = FindAnyObjectByType<S_ExitGate>();
            if (exitGate != null)
            {
                goalPosition = exitGate.transform.position;
                goalFound = true;
            }
        }

        if (debugMode && goalFound)
            Debug.Log($"[MCTSBotController] Goal found at {goalPosition}");
    }

    // ═══════════════════════════════════════════
    //  Stuck Detection
    // ═══════════════════════════════════════════

    private void UpdateRealStuckDetection()
    {
        if (rb == null) return;

        Vector2 currentPos = rb.position;
        float moved = Vector2.Distance(currentPos, lastRealPosition);

        if (moved < stuckPositionEpsilon)
        {
            realStuckTimer += Time.fixedDeltaTime;
            if (realStuckTimer >= stuckThreshold)
            {
                realStuckTimer = 0f;
                if (metrics != null) metrics.RecordStuck();

                if (debugMode)
                    Debug.LogWarning("[MCTSBotController] Bot appears stuck! Forcing random action.");

                // Force a random action to break out
                BotAction[] escapeActions = { BotAction.MoveLeftJump, BotAction.MoveRightJump, BotAction.SprintLeft, BotAction.SprintRight };
                currentAction = escapeActions[Random.Range(0, escapeActions.Length)];
            }
        }
        else
        {
            realStuckTimer = 0f;
        }

        lastRealPosition = currentPos;
    }

    // ═══════════════════════════════════════════
    //  Event Handlers
    // ═══════════════════════════════════════════

    private void HandlePlayerDied()
    {
        isDead = true;
        if (metrics != null) metrics.RecordDeath();

        if (debugMode)
            Debug.Log("[MCTSBotController] Player died. Waiting for respawn...");

        // Reset after respawn (GameManager teleports player on PlayerDied)
        Invoke(nameof(ResetAfterDeath), 1.0f);
    }

    private void ResetAfterDeath()
    {
        isDead = false;
        lastRealPosition = rb != null ? rb.position : Vector2.zero;
        realStuckTimer = 0f;
        currentAction = BotAction.Idle;
        decisionTimer = 0f;

        if (debugMode)
            Debug.Log("[MCTSBotController] Resumed after death.");
    }

    private void HandleSectionEnd(int sectionIndex)
    {
        if (debugMode)
            Debug.Log($"[MCTSBotController] Section {sectionIndex} end reached!");

        if (metrics != null)
        {
            metrics.RecordGoalReached();
            metrics.LogReport();
        }

        // Optionally disable bot after reaching goal
        // botEnabled = false;
    }

    // ═══════════════════════════════════════════
    //  Debug: Decision Log
    // ═══════════════════════════════════════════

    private string BuildDecisionLog(MCTSNode root, BotAction chosen)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ MCTS Decision ═══");
        sb.AppendLine($"Chosen: {chosen} | Iterations: {iterationsPerDecision}");

        if (root.Children != null && root.Children.Count > 0)
        {
            // Sort children by visit count
            var sorted = new List<MCTSNode>(root.Children);
            sorted.Sort((a, b) => b.VisitCount.CompareTo(a.VisitCount));

            sb.AppendLine("Top actions:");
            int showCount = Mathf.Min(5, sorted.Count);
            for (int i = 0; i < showCount; i++)
            {
                var child = sorted[i];
                sb.AppendLine($"  {i + 1}. {child.ActionFromParent,-16} " +
                              $"visits={child.VisitCount,4} avgR={child.AverageReward,8:F2}");
            }
        }

        sb.AppendLine($"Pos: {rb.position:F1} → Goal: {goalPosition:F1} " +
                      $"Dist: {Vector2.Distance(rb.position, goalPosition):F1}");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════
    //  Unity Inspector / Gizmos
    // ═══════════════════════════════════════════

    void OnGUI()
    {
        if (!debugMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 300));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<size=14><b>MCTS Bot Controller</b></size>");
        GUILayout.Label($"Enabled: {botEnabled}  Action: {currentAction}");
        GUILayout.Label($"Goal Found: {goalFound}  Goal: {goalPosition}");

        if (rb != null)
        {
            float dist = Vector2.Distance(rb.position, goalPosition);
            GUILayout.Label($"Pos: {rb.position:F1}  Dist→Goal: {dist:F1}");
            GUILayout.Label($"Vel: {rb.linearVelocity:F1}  Grounded: {(S_coleve.Instance != null ? S_coleve.Instance.getPlayerOnGround().ToString() : "?")}");
        }

        if (metrics != null)
        {
            GUILayout.Label(metrics.GetQuickSummary());
        }

        GUILayout.Label($"Repeat: {repeatActionCount}  Stuck: {realStuckTimer:F1}s");

        if (lastRoot != null && lastRoot.Children != null)
        {
            GUILayout.Label("── Top Children ──");
            var sorted = new List<MCTSNode>(lastRoot.Children);
            sorted.Sort((a, b) => b.VisitCount.CompareTo(a.VisitCount));
            int show = Mathf.Min(5, sorted.Count);
            for (int i = 0; i < show; i++)
            {
                var c = sorted[i];
                GUILayout.Label($"  {c.ActionFromParent,-14} v={c.VisitCount} avg={c.AverageReward:F1}");
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || rb == null) return;

        Vector3 pos = rb.position;

        // Ground check rays
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + Vector3.down * groundCheckDistance);
        Gizmos.DrawLine(pos + new Vector3(-0.5f, 0), pos + new Vector3(-0.5f, 0) + Vector3.down * groundCheckDistance);
        Gizmos.DrawLine(pos + new Vector3(0.5f, 0), pos + new Vector3(0.5f, 0) + Vector3.down * groundCheckDistance);

        // Wall check rays
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + Vector3.left * wallCheckDistance);
        Gizmos.DrawLine(pos, pos + Vector3.right * wallCheckDistance);

        // Hazard check rays
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + Vector3.left * wallCheckDistance);
        Gizmos.DrawLine(pos, pos + Vector3.right * wallCheckDistance);
        Gizmos.DrawLine(pos, pos + Vector3.down * groundCheckDistance);

        // Goal direction
        if (goalFound)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, goalPosition);
            Gizmos.DrawWireSphere(goalPosition, goalCheckRadius);
        }

        // Current action direction indicator
        Gizmos.color = Color.cyan;
        Vector3 actionDir = GetActionDirection(currentAction);
        Gizmos.DrawRay(pos, actionDir * gizmoRayLength);
    }

    private Vector3 GetActionDirection(BotAction action)
    {
        switch (action)
        {
            case BotAction.MoveLeft: return Vector3.left;
            case BotAction.MoveRight: return Vector3.right;
            case BotAction.Jump: return Vector3.up;
            case BotAction.MoveLeftJump: return new Vector3(-1, 1, 0).normalized;
            case BotAction.MoveRightJump: return new Vector3(1, 1, 0).normalized;
            case BotAction.SprintLeft: return Vector3.left * 2;
            case BotAction.SprintRight: return Vector3.right * 2;
            case BotAction.GripUp: return Vector3.up;
            case BotAction.GripDown: return Vector3.down;
            case BotAction.GripLeft: return Vector3.left;
            case BotAction.GripRight: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    // ═══════════════════════════════════════════
    //  Public API (for external control / testing)
    // ═══════════════════════════════════════════

    /// <summary>Enable or disable the bot at runtime.</summary>
    public void SetBotEnabled(bool enabled)
    {
        botEnabled = enabled;
        if (!enabled) currentAction = BotAction.Idle;
    }

    /// <summary>Get the currently selected action.</summary>
    public BotAction GetCurrentAction() => currentAction;

    /// <summary>Force a specific goal position (overrides auto-detection).</summary>
    public void SetGoalPosition(Vector2 position)
    {
        goalPosition = position;
        goalFound = true;
    }
}