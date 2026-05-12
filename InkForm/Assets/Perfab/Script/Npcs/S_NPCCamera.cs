using UnityEngine;

/// <summary>
/// Surveillance drone NPC. Follows a patrol route and triggers alerts
/// when the player enters its detection cone.
/// Used in Ch1 (Escape Sequence) and Ch2 (Nurserie restricted zones).
/// </summary>
public class S_NPCCamera : S_NPCbase
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waypointWaitTime = 1f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float detectionAngle = 60f;
    [SerializeField] private float detectionCooldown = 2f;
    [SerializeField] private int suspicionOnDetect = 30;

    [Header("Visual")]
    [SerializeField] private Color idleLight = Color.green;
    [SerializeField] private Color alertLight = Color.red;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float cooldownTimer = 0f;
    private bool playerDetected = false;

    private void Update()
    {
        if (!isActive) return;
        UpdateCooldown();
        UpdateDetection();
        UpdatePatrol();
    }

    private void FixedUpdate()
    {
        if (!isActive) return;
        ExecuteMovement();
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void UpdateDetection()
    {
        if (S_SuspicionSystem.PlayerHidden)
        {
            playerDetected = false;
            return;
        }

        if (cooldownTimer > 0f) return;
        if (S_Player.Instance == null) return;

        float distance = DistanceToPlayer();
        if (distance > detectionRange)
        {
            if (playerDetected)
            {
                playerDetected = false;
                // TODO: Change light colour back to idle light
            }
            return;
        }

        Transform playerBody = S_Player.Instance.GetBodyTransform();
        if (playerBody == null) return;

        Vector2 directionToPlayer = (playerBody.position - transform.position).normalized;
        float angle = Vector2.Angle(facingRight ? Vector2.right : Vector2.left, directionToPlayer);

        if (angle <= detectionAngle * 0.5f)
        {
            if (!playerDetected)
            {
                playerDetected = true;
                OnPlayerDetected();
            }
        }
        else if (playerDetected)
        {
            playerDetected = false;
            // TODO: Change light colour back to idle light
        }
    }

    private void OnPlayerDetected()
    {
        cooldownTimer = detectionCooldown;
        // TODO: Change light colour to alert light
        S_GameEvent.SuspicionChanged(suspicionOnDetect);
        S_GameEvent.AlertTriggered(transform);
    }

    private void UpdatePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                AdvanceWaypoint();
            }
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        if (target != null && Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            isWaiting = true;
            waitTimer = waypointWaitTime;
        }
    }

    private void ExecuteMovement()
    {
        if (waypoints == null || waypoints.Length == 0 || isWaiting) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        npcRig.linearVelocity = new Vector2(direction.x * patrolSpeed, npcRig.linearVelocity.y);
        FlipSprite(direction.x);
    }

    private void AdvanceWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
            currentWaypointIndex = 0;
    }

    private void OnDrawGizmosSelected()
    {
        // Detection cone
        Gizmos.color = playerDetected ? Color.red : Color.yellow;
        Vector3 center = transform.position;
        float halfAngle = detectionAngle * 0.5f * Mathf.Deg2Rad;
        float facingSign = facingRight ? 1f : -1f;
        float baseAngle = facingRight ? 0f : Mathf.PI;

        Vector3 leftDir = new Vector3(Mathf.Cos(baseAngle - halfAngle), Mathf.Sin(baseAngle - halfAngle), 0);
        Vector3 rightDir = new Vector3(Mathf.Cos(baseAngle + halfAngle), Mathf.Sin(baseAngle + halfAngle), 0);
        Vector3 forward = new Vector3(facingSign, 0, 0);

        Gizmos.DrawLine(center, center + leftDir * detectionRange);
        Gizmos.DrawLine(center, center + rightDir * detectionRange);
        Gizmos.DrawLine(center + leftDir * detectionRange, center + rightDir * detectionRange);

        // Waypoints
        if (waypoints == null || waypoints.Length == 0) return;
        Gizmos.color = Color.magenta;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
        }
        if (waypoints.Length > 1)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}
