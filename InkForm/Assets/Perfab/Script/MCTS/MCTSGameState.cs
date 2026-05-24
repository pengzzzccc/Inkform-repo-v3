using UnityEngine;

/// <summary>
/// Lightweight game state snapshot for MCTS simulation.
/// Does NOT copy the entire Unity scene — only records the minimal data
/// needed to evaluate actions and compute rewards.
/// </summary>
public class MCTSGameState
{
    // ── Player state ──
    public Vector2 position;
    public Vector2 velocity;
    public bool isGrounded;
    public bool facingRight;
    public bool isFluidForm;

    // ── Goal tracking ──
    public Vector2 goalPosition;
    public float distanceToGoal;

    // ── Terminal flags ──
    public bool isDead;
    public bool reachedGoal;

    // ── Simulation clock ──
    public float simulatedTime;
    public float stuckTime;
    public Vector2 lastPositionForStuck;
    public float stuckCheckTimer;

    // ── Physics constants (captured once from scene) ──
    public float moveSpeed;
    public float jumpForce;
    public float gravityScale;
    public float sprintForce;

    // ── Environment queries (canned results from Physics2D at snapshot time) ──
    public bool hasWallLeft;
    public bool hasWallRight;
    public bool hasGroundAheadLeft;
    public bool hasGroundAheadRight;
    public bool hasHazardLeft;
    public bool hasHazardRight;
    public bool hasHazardBelow;
    public bool hasGoalNearby;

    /// <summary>
    /// Creates a deep copy for tree expansion / rollout branching.
    /// </summary>
    public MCTSGameState Clone()
    {
        return new MCTSGameState
        {
            position = position,
            velocity = velocity,
            isGrounded = isGrounded,
            facingRight = facingRight,
            isFluidForm = isFluidForm,
            goalPosition = goalPosition,
            distanceToGoal = distanceToGoal,
            isDead = isDead,
            reachedGoal = reachedGoal,
            simulatedTime = simulatedTime,
            stuckTime = stuckTime,
            lastPositionForStuck = lastPositionForStuck,
            stuckCheckTimer = stuckCheckTimer,
            moveSpeed = moveSpeed,
            jumpForce = jumpForce,
            gravityScale = gravityScale,
            sprintForce = sprintForce,
            hasWallLeft = hasWallLeft,
            hasWallRight = hasWallRight,
            hasGroundAheadLeft = hasGroundAheadLeft,
            hasGroundAheadRight = hasGroundAheadRight,
            hasHazardLeft = hasHazardLeft,
            hasHazardRight = hasHazardRight,
            hasHazardBelow = hasHazardBelow,
            hasGoalNearby = hasGoalNearby,
        };
    }

    /// <summary>
    /// Advances the simplified physics model by one simulation step (fixedDeltaTime).
    /// </summary>
    public void SimulateStep(BotAction action, float fixedDeltaTime, float groundCheckDist,
        float wallCheckDist, float hazardCheckRadius, float goalCheckRadius)
    {
        if (isDead || reachedGoal) return;

        simulatedTime += fixedDeltaTime;
        float stepMoveSpeed = moveSpeed;
        float stepJumpForce = jumpForce;
        float stepGravity = gravityScale;
        float stepSprintForce = sprintForce;

        // ── Apply action to velocity ──
        float vx = velocity.x;
        float vy = velocity.y;

        switch (action)
        {
            case BotAction.Idle:
                vx = 0f;
                break;

            case BotAction.MoveLeft:
                vx = -stepMoveSpeed;
                break;

            case BotAction.MoveRight:
                vx = stepMoveSpeed;
                break;

            case BotAction.Jump:
                if (isGrounded)
                {
                    vy = stepJumpForce;
                    isGrounded = false;
                }
                break;

            case BotAction.MoveLeftJump:
                vx = -stepMoveSpeed;
                if (isGrounded)
                {
                    vy = stepJumpForce;
                    isGrounded = false;
                }
                break;

            case BotAction.MoveRightJump:
                vx = stepMoveSpeed;
                if (isGrounded)
                {
                    vy = stepJumpForce;
                    isGrounded = false;
                }
                break;

            case BotAction.SprintLeft:
                vx = -stepSprintForce;
                break;

            case BotAction.SprintRight:
                vx = stepSprintForce;
                break;

            case BotAction.GripUp:
                if (isFluidForm)
                {
                    vx = 0f;
                    vy = stepMoveSpeed;
                }
                break;

            case BotAction.GripDown:
                if (isFluidForm)
                {
                    vx = 0f;
                    vy = -stepMoveSpeed;
                }
                break;

            case BotAction.GripLeft:
                if (isFluidForm)
                {
                    vx = -stepMoveSpeed;
                    vy *= 0.5f;
                }
                break;

            case BotAction.GripRight:
                if (isFluidForm)
                {
                    vx = stepMoveSpeed;
                    vy *= 0.5f;
                }
                break;

            case BotAction.Hide:
                vx = 0f;
                vy = 0f;
                break;
        }

        // ── Apply gravity (unless gripping in fluid form for GripUp/Down/Left/Right) ──
        bool isGripAction = action == BotAction.GripUp || action == BotAction.GripDown ||
                            action == BotAction.GripLeft || action == BotAction.GripRight;
        if (!(isFluidForm && isGripAction))
        {
            vy -= stepGravity * fixedDeltaTime * 9.81f;
        }

        // ── Integrate position ──
        velocity = new Vector2(vx, vy);
        Vector2 newPos = position + velocity * fixedDeltaTime;

        // ── Simplified collision checks ──
        // Ground: if we were grounded and moving down, clamp to ground
        if (isGrounded && vy <= 0f)
        {
            newPos.y = Mathf.Max(newPos.y, position.y - 0.1f);
            velocity.y = 0f;
        }

        // Wall collision (simplified): if moving into a wall, zero horizontal velocity
        if (hasWallLeft && vx < 0f)
        {
            newPos.x = position.x;
            velocity.x = 0f;
        }
        if (hasWallRight && vx > 0f)
        {
            newPos.x = position.x;
            velocity.x = 0f;
        }

        // ── Hazard detection ──
        if (hasHazardBelow && newPos.y < position.y - hazardCheckRadius)
        {
            isDead = true;
        }
        if ((hasHazardLeft && vx < 0f) || (hasHazardRight && vx > 0f))
        {
            isDead = true;
        }

        position = newPos;

        // ── Goal check ──
        float newDist = Vector2.Distance(position, goalPosition);
        if (newDist < goalCheckRadius)
        {
            reachedGoal = true;
        }
        distanceToGoal = newDist;

        // ── Stuck detection ──
        stuckCheckTimer += fixedDeltaTime;
        if (stuckCheckTimer >= 0.5f)
        {
            float moved = Vector2.Distance(position, lastPositionForStuck);
            if (moved < 0.05f)
            {
                stuckTime += stuckCheckTimer;
            }
            else
            {
                stuckTime = 0f;
            }
            lastPositionForStuck = position;
            stuckCheckTimer = 0f;
        }

        // ── Simple ground detection for next step ──
        // In simulation, we consider grounded if position hasn't fallen significantly
        if (isGrounded && vy <= 0f)
        {
            // Stay grounded
        }
        else if (velocity.y < -0.5f && position.y <= lastPositionForStuck.y + 0.01f)
        {
            // Landing heuristic
            isGrounded = true;
            velocity.y = 0f;
        }
        else
        {
            isGrounded = false;
        }
    }

    /// <summary>
    /// Computes the reward for the current state.
    /// </summary>
    public float ComputeReward(float prevDistanceToGoal)
    {
        float reward = 0f;

        // Terminal rewards
        if (reachedGoal) return 100f;
        if (isDead) return -100f;

        // Distance delta reward
        float distDelta = prevDistanceToGoal - distanceToGoal;
        reward += distDelta * 5f;

        // Time penalty
        reward -= 0.5f;

        // Stuck penalty
        if (stuckTime > 2.0f)
        {
            reward -= 10f;
        }

        return reward;
    }
}

/// <summary>
/// All actions the bot can take. Derived from confirmed player abilities.
/// </summary>
public enum BotAction
{
    Idle,
    MoveLeft,
    MoveRight,
    Jump,
    MoveLeftJump,
    MoveRightJump,
    SprintLeft,
    SprintRight,
    GripUp,
    GripDown,
    GripLeft,
    GripRight,
    Hide,
}