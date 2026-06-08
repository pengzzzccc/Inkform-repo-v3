using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the authored level flow: fixed training, random training, facility graph, and ending.
/// Scene loading itself stays in S_GameManager.
/// </summary>
public class S_RunFlowController : MonoBehaviour
{
    public enum RunPhase
    {
        NotStarted,
        FixedTraining,
        RandomTraining,
        Facility,
        Ending
    }

    public static S_RunFlowController Instance { get; private set; }

    [SerializeField] private S_RunFlowConfig runFlowConfig;

    private RunPhase phase = RunPhase.NotStarted;
    private RoomId currentRoom = RoomId.None;
    private bool hasPendingRoomEntry;
    private S_RoomTransitionRequest pendingRoomEntry;
    private Coroutine doorEntryRoutine;

    public RunPhase Phase => phase;
    public RoomId CurrentRoom => currentRoom;
    public S_RunFlowConfig Config => runFlowConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }

        Instance = this;
        SyncCurrentRoomFromActiveScene();
    }

    private void OnEnable()
    {
        S_GameEvent.OnRunStartRequested += StartNewRun;
        S_GameEvent.OnLevelCompleted += HandleLevelCompleted;
        S_GameEvent.OnRoomEnterRequested += HandleRoomEnterRequested;
        S_GameEvent.OnEndingRequested += EnterEnding;
        S_GameEvent.OnReturnToStartMenuRequested += ReturnToStartMenu;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        S_GameEvent.OnRunStartRequested -= StartNewRun;
        S_GameEvent.OnLevelCompleted -= HandleLevelCompleted;
        S_GameEvent.OnRoomEnterRequested -= HandleRoomEnterRequested;
        S_GameEvent.OnEndingRequested -= EnterEnding;
        S_GameEvent.OnReturnToStartMenuRequested -= ReturnToStartMenu;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartNewRun()
    {
        if (!ValidateConfig())
            return;

        ResetRunState();
        S_GameEvent.SuspicionResetRequested();
        EnterFacility();
    }

    public void ReturnToStartMenu()
    {
        if (runFlowConfig == null || string.IsNullOrWhiteSpace(runFlowConfig.StartMenuSceneKey))
        {
            Debug.LogWarning("[RunFlow] Start menu scene is not configured.");
            return;
        }

        ResetRunState();
        S_GameEvent.SceneLoadRequested(runFlowConfig.StartMenuSceneKey);
    }

    public bool TryConsumeInkPodSpawnForCurrentScene()
    {
        if (phase != RunPhase.Facility
            || !hasPendingRoomEntry
            || pendingRoomEntry.EntryMode != S_RoomEntryMode.InkPod)
        {
            return false;
        }

        if (pendingRoomEntry.TargetRoom != RoomId.None
            && currentRoom != RoomId.None
            && pendingRoomEntry.TargetRoom != currentRoom)
        {
            return false;
        }

        ClearPendingRoomEntry();
        return true;
    }

    private void HandleLevelCompleted(S_LevelCompletionReason reason)
    {
        switch (phase)
        {
            case RunPhase.Facility:
                Debug.LogWarning($"[RunFlow] Ignored level completion '{reason}' during facility phase. Use RoomExit or EndingTrigger for facility navigation.");
                break;
            case RunPhase.Ending:
                break;
            case RunPhase.NotStarted:
            default:
                Debug.LogWarning($"[RunFlow] Level completed while no run is active. Reason: {reason}.");
                break;
        }
    }

    private void EnterFacility()
    {
        phase = RunPhase.Facility;

        List<RoomId> entries = runFlowConfig.GetFacilityEntryRooms();
        if (entries.Count == 0)
        {
            Debug.LogError("[RunFlow] RoomGraph has no facility entry rooms.");
            return;
        }

        RoomId entryRoom = entries[Random.Range(0, entries.Count)];
        S_GameEvent.FacilityEntered();
        LoadRoom(entryRoom, S_RoomEntryMode.InkPod, RoomId.TR);
    }

    private void HandleRoomEnterRequested(S_RoomTransitionRequest request)
    {
        RoomId targetRoom = request.TargetRoom;
        if (phase != RunPhase.Facility)
        {
            Debug.LogWarning($"[RunFlow] Ignored room request '{targetRoom}' because the run is in phase {phase}.");
            return;
        }

        if (targetRoom == RoomId.None)
        {
            Debug.LogWarning("[RunFlow] Ignored room request with RoomId.None.");
            return;
        }

        S_RoomGraph roomGraph = runFlowConfig.RoomGraph;
        if (roomGraph == null)
        {
            Debug.LogError("[RunFlow] RoomGraph is not configured.");
            return;
        }

        RoomId sourceRoom = request.SourceRoom != RoomId.None ? request.SourceRoom : currentRoom;
        if (sourceRoom != RoomId.None && !roomGraph.AreAdjacent(sourceRoom, targetRoom))
        {
            Debug.LogWarning($"[RunFlow] Room {targetRoom} is not adjacent to {sourceRoom}; request ignored.");
            return;
        }

        if (roomGraph.IsForEnding(targetRoom))
        {
            EnterEnding();
            return;
        }

        LoadRoom(targetRoom, request.EntryMode, sourceRoom);
    }

    private void LoadRoom(RoomId roomId, S_RoomEntryMode entryMode = S_RoomEntryMode.Door, RoomId sourceRoom = RoomId.None)
    {
        string sceneKey = runFlowConfig.RoomGraph != null ? runFlowConfig.RoomGraph.GetSceneKey(roomId) : string.Empty;
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogError($"[RunFlow] Room {roomId} has no scene assigned.");
            return;
        }

        SetPendingRoomEntry(new S_RoomTransitionRequest(roomId, entryMode, sourceRoom));
        currentRoom = roomId;
        S_GameEvent.SceneLoadRequested(sceneKey);
    }

    private void EnterEnding()
    {
        if (phase == RunPhase.Ending)
            return;

        phase = RunPhase.Ending;
        currentRoom = RoomId.None;
        ClearPendingRoomEntry();

        if (runFlowConfig == null || string.IsNullOrWhiteSpace(runFlowConfig.EndingSceneKey))
        {
            Debug.LogError("[RunFlow] Ending scene is not configured.");
            return;
        }

        S_GameEvent.SceneLoadRequested(runFlowConfig.EndingSceneKey);
    }

    private void ResetRunState()
    {
        phase = RunPhase.NotStarted;
        currentRoom = RoomId.None;
        ClearPendingRoomEntry();
    }

    private bool ValidateConfig()
    {
        if (runFlowConfig != null)
            return true;

        Debug.LogError("[RunFlow] Missing RunFlowConfig on S_RunFlowController.");
        return false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncCurrentRoomFromActiveScene();
        ApplyPendingRoomEntryForScene(scene);
    }

    private void SyncCurrentRoomFromActiveScene()
    {
        if (runFlowConfig == null || runFlowConfig.RoomGraph == null)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        string sceneKey = !string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.path : activeScene.name;
        if (runFlowConfig.RoomGraph.TryResolve(sceneKey, out RoomId roomId))
        {
            currentRoom = roomId;
            if (phase == RunPhase.NotStarted)
                phase = RunPhase.Facility;
        }
    }

    private void SetPendingRoomEntry(S_RoomTransitionRequest request)
    {
        hasPendingRoomEntry = true;
        pendingRoomEntry = request;
    }

    private void ClearPendingRoomEntry()
    {
        hasPendingRoomEntry = false;
        pendingRoomEntry = default;

        if (doorEntryRoutine != null)
        {
            StopCoroutine(doorEntryRoutine);
            doorEntryRoutine = null;
        }
    }

    private void ApplyPendingRoomEntryForScene(Scene scene)
    {
        if (!hasPendingRoomEntry || pendingRoomEntry.EntryMode != S_RoomEntryMode.Door)
            return;

        if (pendingRoomEntry.TargetRoom != RoomId.None
            && currentRoom != RoomId.None
            && pendingRoomEntry.TargetRoom != currentRoom)
        {
            return;
        }

        if (doorEntryRoutine != null)
            StopCoroutine(doorEntryRoutine);

        doorEntryRoutine = StartCoroutine(ApplyDoorEntryAfterSceneLoad(pendingRoomEntry, scene));
    }

    private IEnumerator ApplyDoorEntryAfterSceneLoad(S_RoomTransitionRequest request, Scene scene)
    {
        yield return null;

        IPlayerActor player = null;
        for (int i = 0; i < 120 && !S_PlayerLookup.TryGetActive(out player); i++)
            yield return null;

        if (player == null)
        {
            Debug.LogError($"[RunFlow] Door entry from {request.SourceRoom} to {request.TargetRoom} could not find an active player.");
            ClearPendingRoomEntry();
            yield break;
        }

        if (!TryFindArrivalDoor(scene, request.SourceRoom, out S_RoomExit arrivalDoor))
        {
            Debug.LogError($"[RunFlow] Door entry into {request.TargetRoom} could not find a reverse door targeting {request.SourceRoom} in scene '{scene.name}'.");
            ClearPendingRoomEntry();
            yield break;
        }

        player.Teleport(arrivalDoor.GetArrivalPosition());
        if (S_Player.Instance != null)
            S_Player.Instance.SetFacingRight(arrivalDoor.ArrivalFacingRight);

        ClearPendingRoomEntry();
    }

    private static bool TryFindArrivalDoor(Scene scene, RoomId sourceRoom, out S_RoomExit arrivalDoor)
    {
        arrivalDoor = null;
        if (sourceRoom == RoomId.None)
            return false;

        S_RoomExit[] exits = FindObjectsByType<S_RoomExit>(FindObjectsSortMode.None);
        for (int i = 0; i < exits.Length; i++)
        {
            S_RoomExit exit = exits[i];
            if (exit == null || exit.gameObject.scene != scene)
                continue;

            if (exit.TargetRoom != sourceRoom)
                continue;

            arrivalDoor = exit;
            return true;
        }

        return false;
    }
}
