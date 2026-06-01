using UnityEngine;
using UnityEngine.SceneManagement;

public class S_SceneCheckpointTracker : MonoBehaviour
{
    private const string TrackerName = "SceneCheckpointTracker";

    private static bool sceneHookRegistered;

    private Scene trackedScene;
    private Vector2 spawnPosition;
    private bool hasSpawnPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        sceneHookRegistered = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!sceneHookRegistered)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookRegistered = true;
        }

        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponentInChildren<S_SceneCheckpointTracker>(true) != null)
                return;
        }

        GameObject trackerObject = new GameObject(TrackerName);
        SceneManager.MoveGameObjectToScene(trackerObject, scene);

        S_SceneCheckpointTracker tracker = trackerObject.AddComponent<S_SceneCheckpointTracker>();
        tracker.trackedScene = scene;
    }

    private void Awake()
    {
        if (!trackedScene.IsValid())
            trackedScene = gameObject.scene;
    }

    private void Start()
    {
        CacheDefaultSpawnPosition();
    }

    private void OnEnable()
    {
        S_GameEvent.OnSpawnPointChanged += HandleSpawnPointChanged;
        S_GameEvent.OnRespawnRequested += HandleRespawnRequested;
    }

    private void OnDisable()
    {
        S_GameEvent.OnSpawnPointChanged -= HandleSpawnPointChanged;
        S_GameEvent.OnRespawnRequested -= HandleRespawnRequested;
    }

    private void HandleSpawnPointChanged(Transform checkpoint)
    {
        if (checkpoint == null || checkpoint.gameObject.scene != trackedScene)
            return;

        spawnPosition = checkpoint.position;
        hasSpawnPosition = true;
    }

    private void HandleRespawnRequested()
    {
        ReloadTrackedScene();
    }

    private void CacheDefaultSpawnPosition()
    {
        if (hasSpawnPosition)
            return;

        if (!S_PlayerLookup.TryGetActive(out IPlayerActor player) || !IsPlayerInTrackedScene(player))
            return;

        spawnPosition = player.Rigidbody != null
            ? player.Rigidbody.position
            : (Vector2)player.BodyTransform.position;
        hasSpawnPosition = true;
    }

    private bool IsPlayerInTrackedScene(IPlayerActor player)
    {
        if (player == null || player.Rigidbody == null)
            return false;

        return player.Rigidbody.gameObject.scene == trackedScene;
    }

    private void ReloadTrackedScene()
    {
        if (!trackedScene.IsValid() || string.IsNullOrWhiteSpace(trackedScene.name))
            return;

        string sceneKey = !string.IsNullOrWhiteSpace(trackedScene.path)
            ? trackedScene.path
            : trackedScene.name;

        if (S_GameManager.Instance != null)
            S_GameEvent.SceneLoadRequested(sceneKey);
        else
            SceneManager.LoadScene(sceneKey);
    }
}
