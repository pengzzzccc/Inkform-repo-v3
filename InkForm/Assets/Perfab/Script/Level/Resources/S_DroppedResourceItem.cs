using UnityEngine;

public class S_DroppedResourceItem : MonoBehaviour
{
    private const string DefaultResourceId = "block_fragment";

    [Header("Resource")]
    [SerializeField] private string resourceId = DefaultResourceId;
    [SerializeField, Min(1)] private int resourceAmount = 1;

    [Header("Pickup")]
    [SerializeField, Min(0f)] private float pickupDelay = 0.25f;
    [SerializeField, Min(0f)] private float lifetime = 12f;
    [SerializeField, Min(0.01f)] private float pickupRadius = 0.25f;

    [Header("Attraction")]
    [SerializeField, Min(0f)] private float magnetStartBuffer = 0.35f;
    [SerializeField, Min(0f)] private float attractRadius = 2.5f;
    [SerializeField, Min(0f)] private float attractDelayInRange = 0.35f;
    [SerializeField, Min(0f)] private float attractSpeed = 8f;
    [SerializeField, Min(0f)] private float attractAcceleration = 20f;

    [Header("Motion")]
    [SerializeField] private Vector2 initialVelocity = new Vector2(0f, 3.5f);
    [SerializeField, Min(0f)] private float popDuration = 0.45f;
    [SerializeField] private float popGravity = -7f;
    [SerializeField, Min(0f)] private float horizontalDamping = 4f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float bobFrequency = 5f;
    [SerializeField] private float rotationSpeed = 90f;

    private Vector2 velocity;
    private Vector3 settledPosition;
    private float age;
    private bool settled;
    private bool collected;
    private Transform attractTarget;
    private float inRangeTimer;
    private float currentAttractSpeed;
    private bool attracting;

    private void Awake()
    {
        EnsurePickupComponents();
        ResetMotion(initialVelocity);
    }

    private void OnEnable()
    {
        collected = false;
        ResetMotion(initialVelocity);
    }

    public void Initialize(string newResourceId, int newResourceAmount, Vector2 launchVelocity, float newPickupDelay, float newLifetime)
    {
        resourceId = NormalizeResourceId(newResourceId);
        resourceAmount = Mathf.Max(1, newResourceAmount);
        pickupDelay = Mathf.Max(0f, newPickupDelay);
        lifetime = Mathf.Max(0f, newLifetime);

        ResetMotion(launchVelocity);
    }

    private void Update()
    {
        if (collected)
            return;

        float deltaTime = Time.deltaTime;
        age += deltaTime;

        if (lifetime > 0f && age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (attracting)
            UpdateAttraction(deltaTime);
        else
        {
            if (settled)
                UpdateHover();
            else
                UpdatePopMotion(deltaTime);

            UpdateAttractDetection(deltaTime);
        }

        transform.Rotate(Vector3.forward, rotationSpeed * deltaTime, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void ResetMotion(Vector2 launchVelocity)
    {
        age = 0f;
        settled = false;
        collected = false;
        attracting = false;
        attractTarget = null;
        inRangeTimer = 0f;
        currentAttractSpeed = 0f;
        velocity = launchVelocity;
        initialVelocity = launchVelocity;
        settledPosition = transform.position;
    }

    private void UpdatePopMotion(float deltaTime)
    {
        transform.position += (Vector3)(velocity * deltaTime);
        velocity.y += popGravity * deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, 0f, horizontalDamping * deltaTime);

        if (age < popDuration)
            return;

        settled = true;
        settledPosition = transform.position;
    }

    private void UpdateHover()
    {
        float bob = Mathf.Sin((age - popDuration) * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(settledPosition.x, settledPosition.y + bob, settledPosition.z);
    }

    private void UpdateAttractDetection(float deltaTime)
    {
        if (age < pickupDelay + magnetStartBuffer)
            return;

        Transform target = ResolveAttractTarget();
        if (target == null)
        {
            ResetAttractTimer();
            return;
        }

        float radiusSqr = attractRadius * attractRadius;
        float distanceSqr = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;

        if (distanceSqr > radiusSqr)
        {
            ResetAttractTimer();
            return;
        }

        attractTarget = target;
        inRangeTimer += deltaTime;

        if (inRangeTimer >= attractDelayInRange)
            BeginAttraction(target);
    }

    private void BeginAttraction(Transform target)
    {
        attracting = true;
        attractTarget = target;
        currentAttractSpeed = Mathf.Max(0.01f, attractSpeed);
    }

    private void UpdateAttraction(float deltaTime)
    {
        if (attractTarget == null)
            attractTarget = ResolveAttractTarget();

        if (attractTarget == null)
            return;

        currentAttractSpeed += attractAcceleration * deltaTime;

        Vector3 targetPosition = attractTarget.position;
        targetPosition.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentAttractSpeed * deltaTime);

        if (Vector2.Distance(transform.position, targetPosition) <= pickupRadius)
            Collect();
    }

    private Transform ResolveAttractTarget()
    {
        if (!S_PlayerLookup.TryGetActive(out IPlayerActor player))
            return null;

        return player.BodyTransform;
    }

    private void ResetAttractTimer()
    {
        attractTarget = null;
        inRangeTimer = 0f;
    }

    private void TryCollect(Collider2D other)
    {
        if (collected || age < pickupDelay)
            return;

        if (!S_PlayerLookup.IsPlayer(other))
            return;

        Collect();
    }

    private void Collect()
    {
        if (collected)
            return;

        collected = true;
        S_DropResourceCounter.Add(resourceId, resourceAmount);
        Destroy(gameObject);
    }

    private void EnsurePickupComponents()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D itemCollider in colliders)
        {
            itemCollider.isTrigger = true;
        }

        Collider2D rootCollider = GetComponent<Collider2D>();
        if (rootCollider == null)
        {
            CircleCollider2D pickupCollider = gameObject.AddComponent<CircleCollider2D>();
            pickupCollider.radius = pickupRadius;
            pickupCollider.isTrigger = true;
        }

        Rigidbody2D itemRigidbody = GetComponent<Rigidbody2D>();
        if (itemRigidbody == null)
            itemRigidbody = gameObject.AddComponent<Rigidbody2D>();

        itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
        itemRigidbody.gravityScale = 0f;
        itemRigidbody.simulated = true;
    }

    private static string NormalizeResourceId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultResourceId : value.Trim();
    }
}
