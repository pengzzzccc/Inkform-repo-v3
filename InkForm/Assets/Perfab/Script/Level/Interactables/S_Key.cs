using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Collectible key item. Player touches it to collect.
/// Keys persist across deaths (not reset on respawn).
/// Resets automatically when a new scene loads.
/// </summary>
public class S_Key : MonoBehaviour
{
    private static readonly HashSet<S_Key> allKeys = new HashSet<S_Key>();
    private static int collectedCount;
    private static bool sceneHooked;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupClip;

    [Header("Pickup")]
    [SerializeField, Min(0f)] private float pickupDelayAfterSpawn = 0.25f;

    [Header("Dropped Key Motion")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField, Min(0f)] private float popDuration = 0.45f;
    [SerializeField] private float popGravity = -12f;
    [SerializeField, Min(0f)] private float horizontalDamping = 4f;
    [SerializeField, Min(0f)] private float groundProbeDistance = 5f;
    [SerializeField, Min(0f)] private float hoverHeight = 0.45f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float bobFrequency = 5f;

    public static int TotalKeys => allKeys.Count;
    public static int CollectedKeys => collectedCount;

    private bool isCollected;
    private float spawnTime;
    private float effectivePickupDelay;
    private bool droppedMotionActive;
    private bool droppedSettled;
    private float droppedAge;
    private Vector2 droppedVelocity;
    private Vector3 hoverBasePosition;
    private RaycastHit2D[] groundHits;

    private void Awake()
    {
        if (!sceneHooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHooked = true;
        }

        allKeys.Add(this);
        isCollected = false;
        spawnTime = Time.time;
        effectivePickupDelay = pickupDelayAfterSpawn;
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
        effectivePickupDelay = pickupDelayAfterSpawn;
    }

    private void OnDestroy()
    {
        allKeys.Remove(this);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // New level loaded — reset collection state
        // (allKeys will be repopulated by new scene's S_Key Awake calls)
        collectedCount = 0;
    }

    private void Update()
    {
        if (isCollected || !droppedMotionActive)
            return;

        UpdateDroppedMotion(Time.deltaTime);
    }

    public void InitializeDroppedKey(Vector2 launchVelocity, float pickupDelay)
    {
        droppedMotionActive = true;
        droppedSettled = false;
        droppedAge = 0f;
        droppedVelocity = launchVelocity;
        hoverBasePosition = transform.position;
        spawnTime = Time.time;
        effectivePickupDelay = Mathf.Max(0f, pickupDelay);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    private void TryCollect(Collider2D collision)
    {
        if (isCollected || Time.time < spawnTime + effectivePickupDelay)
            return;

        if (!S_PlayerLookup.IsPlayer(collision))
            return;

        Collect();
    }

    private void Collect()
    {
        isCollected = true;
        collectedCount++;

        PlayPickupSfx();
        gameObject.SetActive(false);

        S_GameEvent.KeyCollected();
        S_GameEvent.KeyCountChanged(collectedCount, allKeys.Count);
    }

    private void PlayPickupSfx()
    {
        if (pickupClip == null)
            return;

        S_GameEvent.PlaySFX(pickupClip);
    }

    private void UpdateDroppedMotion(float deltaTime)
    {
        droppedAge += deltaTime;

        if (!droppedSettled)
        {
            transform.position += (Vector3)(droppedVelocity * deltaTime);
            droppedVelocity.y += popGravity * deltaTime;
            droppedVelocity.x = Mathf.MoveTowards(droppedVelocity.x, 0f, horizontalDamping * deltaTime);

            if (droppedAge >= popDuration && TryGetGroundY(transform.position, out float groundY))
            {
                float targetY = groundY + hoverHeight;
                if (transform.position.y <= targetY || droppedVelocity.y <= 0f)
                {
                    hoverBasePosition = new Vector3(transform.position.x, targetY, transform.position.z);
                    transform.position = hoverBasePosition;
                    droppedSettled = true;
                }
            }
            else if (droppedAge >= popDuration * 1.75f)
            {
                hoverBasePosition = transform.position;
                droppedSettled = true;
            }

            return;
        }

        float bob = Mathf.Sin((droppedAge - popDuration) * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(hoverBasePosition.x, hoverBasePosition.y + bob, hoverBasePosition.z);
    }

    private bool TryGetGroundY(Vector3 originPosition, out float groundY)
    {
        groundY = originPosition.y;
        groundHits ??= new RaycastHit2D[8];

        int hitCount = Physics2D.RaycastNonAlloc(originPosition, Vector2.down, groundHits, groundProbeDistance, groundLayer);
        float closestDistance = float.PositiveInfinity;
        bool foundGround = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundHits[i];
            Collider2D hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger)
                continue;

            if (hitCollider.transform == transform || hitCollider.GetComponentInParent<S_Key>() == this)
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            groundY = hit.point.y;
            foundGround = true;
        }

        return foundGround;
    }
}
