using System.Collections.Generic;
using UnityEngine;

public class S_NPCWaveSpawner : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Wave Settings")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField, Min(1)] private int npcsPerSide = 2;
    [SerializeField, Min(1)] private int maxAliveNpcs = 20;

    [Header("Spawn Position")]
    [SerializeField] private float sideOffset = 2f;
    [SerializeField] private float groundDetectDistance = 10f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float spawnYFallback = 0f;

    [Header("Cleanup")]
    [SerializeField] private bool cleanupDistantNpcs = true;
    [SerializeField] private float cleanupDistance = 30f;
    [SerializeField] private float cleanupCheckInterval = 5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float spawnTimer;
    private float cleanupTimer;
    private readonly List<GameObject> aliveNpcs = new List<GameObject>();
    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        spawnTimer = spawnInterval;
        cleanupTimer = cleanupCheckInterval;
    }

    private void Update()
    {
        if (npcPrefab == null || targetCamera == null)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnWave();
        }

        if (cleanupDistantNpcs)
        {
            cleanupTimer -= Time.deltaTime;
            if (cleanupTimer <= 0f)
            {
                cleanupTimer = cleanupCheckInterval;
                CleanupDistant();
            }
        }
    }

    private void SpawnWave()
    {
        CleanDeadReferences();
        int slotsRemaining = maxAliveNpcs - aliveNpcs.Count;
        if (slotsRemaining <= 0)
            return;

        Vector3 camPos = targetCamera.transform.position;
        float camHalfWidth = GetCameraHalfWidth();
        float leftEdge = camPos.x - camHalfWidth - sideOffset;
        float rightEdge = camPos.x + camHalfWidth + sideOffset;

        int perSide = Mathf.Min(npcsPerSide, slotsRemaining / 2);
        if (perSide <= 0 && slotsRemaining > 0)
            perSide = 1;

        SpawnAtSide(leftEdge, -1f, perSide);
        SpawnAtSide(rightEdge, 1f, perSide);
    }

    private void SpawnAtSide(float xPos, float direction, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float jitterX = Random.Range(-0.5f, 0.5f);
            Vector2 spawnOrigin = new Vector2(xPos + jitterX, targetCamera.transform.position.y);

            RaycastHit2D hit = Physics2D.Raycast(spawnOrigin, Vector2.down, groundDetectDistance, groundLayer);
            Vector3 spawnPos;
            if (hit.collider != null)
                spawnPos = new Vector3(hit.point.x, hit.point.y + 0.5f, 0f);
            else
                spawnPos = new Vector3(spawnOrigin.x, spawnYFallback, 0f);

            GameObject npc = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
            if (direction < 0f)
            {
                Vector3 scale = npc.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                npc.transform.localScale = scale;
            }

            aliveNpcs.Add(npc);

            if (debugLogs)
                Debug.Log($"[WaveSpawner] Spawned NPC at {spawnPos}, facing {(direction > 0f ? "right" : "left")}");
        }
    }

    private void CleanupDistant()
    {
        if (targetCamera == null)
            return;

        CleanDeadReferences();
        Vector3 camPos = targetCamera.transform.position;

        for (int i = aliveNpcs.Count - 1; i >= 0; i--)
        {
            GameObject npc = aliveNpcs[i];
            if (npc == null)
            {
                aliveNpcs.RemoveAt(i);
                continue;
            }

            float dist = Mathf.Abs(npc.transform.position.x - camPos.x);
            if (dist > cleanupDistance)
            {
                if (debugLogs)
                    Debug.Log($"[WaveSpawner] Destroying distant NPC: {npc.name} (dist={dist:F1})");
                Destroy(npc);
                aliveNpcs.RemoveAt(i);
            }
        }
    }

    private void CleanDeadReferences()
    {
        aliveNpcs.RemoveAll(n => n == null);
    }

    private float GetCameraHalfWidth()
    {
        if (targetCamera.orthographic)
            return targetCamera.orthographicSize * targetCamera.aspect;
        return targetCamera.orthographicSize * targetCamera.aspect;
    }

    public int AliveCount
    {
        get
        {
            CleanDeadReferences();
            return aliveNpcs.Count;
        }
    }

    public void ClearAll()
    {
        for (int i = aliveNpcs.Count - 1; i >= 0; i--)
        {
            if (aliveNpcs[i] != null)
                Destroy(aliveNpcs[i]);
        }
        aliveNpcs.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        float halfW = cam.orthographicSize * cam.aspect;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        float leftX = camPos.x - halfW - sideOffset;
        float rightX = camPos.x + halfW + sideOffset;
        Gizmos.DrawLine(new Vector3(leftX, camPos.y - 5f, 0f), new Vector3(leftX, camPos.y + 5f, 0f));
        Gizmos.DrawLine(new Vector3(rightX, camPos.y - 5f, 0f), new Vector3(rightX, camPos.y + 5f, 0f));

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(camPos, cleanupDistance);
    }
}