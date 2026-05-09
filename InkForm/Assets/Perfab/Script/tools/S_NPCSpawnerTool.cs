using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class S_NPCSpawnerTool : MonoBehaviour
{
    [Header("NPC Prefab")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField, Min(0)] private int spawnCount = 5;
    [SerializeField] private bool spawnOnStart = false;
    [SerializeField] private bool clearBeforeGenerate = true;

    [Header("Spawn Placement")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool randomizeSpawnPoint = false;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(8f, 0f);
    [SerializeField] private Vector2 spawnPointJitter = Vector2.zero;

    [Header("Naming")]
    [SerializeField] private string spawnedNamePrefix = "NPC";

    [SerializeField, HideInInspector] private List<GameObject> spawnedNpcs = new List<GameObject>();

    public int SpawnedCount
    {
        get
        {
            CleanSpawnedList();
            return spawnedNpcs.Count;
        }
    }

    private void Start()
    {
        if (spawnOnStart)
            Generate();
    }

    [ContextMenu("Apply Spawn Count")]
    public void ApplySpawnCount()
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning($"{nameof(S_NPCSpawnerTool)} needs an NPC prefab before spawning.", this);
            return;
        }

        CleanSpawnedList();

        while (spawnedNpcs.Count > spawnCount)
        {
            GameObject npc = spawnedNpcs[spawnedNpcs.Count - 1];
            spawnedNpcs.RemoveAt(spawnedNpcs.Count - 1);
            DestroySpawnedNpc(npc);
        }

        while (spawnedNpcs.Count < spawnCount)
            SpawnOne(spawnedNpcs.Count);

        MarkDirtyIfNeeded();
    }

    [ContextMenu("Generate NPCs")]
    public void Generate()
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning($"{nameof(S_NPCSpawnerTool)} needs an NPC prefab before spawning.", this);
            return;
        }

        if (clearBeforeGenerate)
            ClearSpawned();

        while (spawnedNpcs.Count < spawnCount)
            SpawnOne(spawnedNpcs.Count);

        MarkDirtyIfNeeded();
    }

    [ContextMenu("Clear Spawned NPCs")]
    public void ClearSpawned()
    {
        CleanSpawnedList();

        for (int i = spawnedNpcs.Count - 1; i >= 0; i--)
            DestroySpawnedNpc(spawnedNpcs[i]);

        spawnedNpcs.Clear();
        MarkDirtyIfNeeded();
    }

    private void SpawnOne(int index)
    {
        Transform parent = spawnParent != null ? spawnParent : transform;
        GetSpawnPose(index, out Vector3 position, out Quaternion rotation);

        GameObject npc = InstantiateNpc(parent, position, rotation);
        if (npc == null) return;

        string prefix = string.IsNullOrWhiteSpace(spawnedNamePrefix) ? npcPrefab.name : spawnedNamePrefix;
        npc.name = $"{prefix}_{index + 1:00}";
        spawnedNpcs.Add(npc);
    }

    private void GetSpawnPose(int index, out Vector3 position, out Quaternion rotation)
    {
        Transform point = GetSpawnPoint(index);
        if (point != null)
        {
            position = point.position + GetJitterOffset();
            rotation = point.rotation;
            return;
        }

        position = transform.position + GetAreaOffset();
        rotation = transform.rotation;
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        if (randomizeSpawnPoint)
            return spawnPoints[Random.Range(0, spawnPoints.Length)];

        return spawnPoints[index % spawnPoints.Length];
    }

    private Vector3 GetAreaOffset()
    {
        return new Vector3(
            Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
            0f);
    }

    private Vector3 GetJitterOffset()
    {
        return new Vector3(
            Random.Range(-spawnPointJitter.x * 0.5f, spawnPointJitter.x * 0.5f),
            Random.Range(-spawnPointJitter.y * 0.5f, spawnPointJitter.y * 0.5f),
            0f);
    }

    private GameObject InstantiateNpc(Transform parent, Vector3 position, Quaternion rotation)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject editorNpc = PrefabUtility.InstantiatePrefab(npcPrefab, parent) as GameObject;
            if (editorNpc != null)
            {
                Undo.RegisterCreatedObjectUndo(editorNpc, "Spawn NPC");
                editorNpc.transform.SetPositionAndRotation(position, rotation);
                return editorNpc;
            }
        }
#endif

        return Instantiate(npcPrefab, position, rotation, parent);
    }

    private void DestroySpawnedNpc(GameObject npc)
    {
        if (npc == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.DestroyObjectImmediate(npc);
            return;
        }
#endif

        Destroy(npc);
    }

    private void CleanSpawnedList()
    {
        spawnedNpcs.RemoveAll(npc => npc == null);
    }

    private void MarkDirtyIfNeeded()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
#endif
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(S_NPCSpawnerTool))]
public class S_NPCSpawnerToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        S_NPCSpawnerTool spawner = (S_NPCSpawnerTool)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawner Tools", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Count"))
                spawner.ApplySpawnCount();

            if (GUILayout.Button("Generate"))
                spawner.Generate();

            if (GUILayout.Button("Clear"))
                spawner.ClearSpawned();
        }

        EditorGUILayout.HelpBox($"Current spawned: {spawner.SpawnedCount}", MessageType.Info);
    }
}
#endif
