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
    private int fixedTrainingIndex;
    private int completedRandomTrainingRooms;
    private int randomTrainingTargetCount;
    private readonly List<int> drawnRandomTrainingIndices = new List<int>();

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
        S_GameEvent.OnStoryTrigger += HandleStoryTrigger;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        S_GameEvent.OnRunStartRequested -= StartNewRun;
        S_GameEvent.OnLevelCompleted -= HandleLevelCompleted;
        S_GameEvent.OnRoomEnterRequested -= HandleRoomEnterRequested;
        S_GameEvent.OnEndingRequested -= EnterEnding;
        S_GameEvent.OnReturnToStartMenuRequested -= ReturnToStartMenu;
        S_GameEvent.OnStoryTrigger -= HandleStoryTrigger;
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

        if (runFlowConfig.FixedTrainingCount > 0)
        {
            phase = RunPhase.FixedTraining;
            fixedTrainingIndex = 0;
            LoadLevelEntry(runFlowConfig.GetFixedTrainingLevel(fixedTrainingIndex));
            return;
        }

        BeginRandomTrainingOrFacility();
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

    private void HandleLevelCompleted(S_LevelCompletionReason reason)
    {
        switch (phase)
        {
            case RunPhase.FixedTraining:
                AdvanceFixedTraining();
                break;
            case RunPhase.RandomTraining:
                AdvanceRandomTraining();
                break;
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

    private void AdvanceFixedTraining()
    {
        fixedTrainingIndex++;
        if (fixedTrainingIndex < runFlowConfig.FixedTrainingCount)
        {
            LoadLevelEntry(runFlowConfig.GetFixedTrainingLevel(fixedTrainingIndex));
            return;
        }

        BeginRandomTrainingOrFacility();
    }

    private void BeginRandomTrainingOrFacility()
    {
        if (runFlowConfig.RandomTrainingPoolCount <= 0)
        {
            EnterFacility();
            return;
        }

        randomTrainingTargetCount = runFlowConfig.GetRandomTrainingTargetCount();
        if (randomTrainingTargetCount <= 0)
        {
            EnterFacility();
            return;
        }

        phase = RunPhase.RandomTraining;
        completedRandomTrainingRooms = 0;
        drawnRandomTrainingIndices.Clear();
        LoadLevelEntry(runFlowConfig.GetRandomTrainingLevel(DrawRandomTrainingIndex()));
    }

    private void AdvanceRandomTraining()
    {
        completedRandomTrainingRooms++;
        if (completedRandomTrainingRooms >= randomTrainingTargetCount
            || drawnRandomTrainingIndices.Count >= runFlowConfig.RandomTrainingPoolCount)
        {
            EnterFacility();
            return;
        }

        LoadLevelEntry(runFlowConfig.GetRandomTrainingLevel(DrawRandomTrainingIndex()));
    }

    private int DrawRandomTrainingIndex()
    {
        List<int> available = new List<int>();
        for (int i = 0; i < runFlowConfig.RandomTrainingPoolCount; i++)
        {
            if (!drawnRandomTrainingIndices.Contains(i))
                available.Add(i);
        }

        if (available.Count == 0)
            return -1;

        int picked = available[Random.Range(0, available.Count)];
        drawnRandomTrainingIndices.Add(picked);
        return picked;
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

        S_GameEvent.FacilityEntered();
        LoadRoom(entries[Random.Range(0, entries.Count)]);
    }

    private void HandleRoomEnterRequested(RoomId targetRoom)
    {
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

        if (currentRoom != RoomId.None && !roomGraph.AreAdjacent(currentRoom, targetRoom))
        {
            Debug.LogWarning($"[RunFlow] Room {targetRoom} is not adjacent to {currentRoom}; request ignored.");
            return;
        }

        if (roomGraph.IsForEnding(targetRoom))
        {
            EnterEnding();
            return;
        }

        LoadRoom(targetRoom);
    }

    private void LoadRoom(RoomId roomId)
    {
        string sceneKey = runFlowConfig.RoomGraph != null ? runFlowConfig.RoomGraph.GetSceneKey(roomId) : string.Empty;
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogError($"[RunFlow] Room {roomId} has no scene assigned.");
            return;
        }

        currentRoom = roomId;
        S_GameEvent.SceneLoadRequested(sceneKey);
    }

    private void EnterEnding()
    {
        if (phase == RunPhase.Ending)
            return;

        phase = RunPhase.Ending;
        currentRoom = RoomId.None;

        if (runFlowConfig == null || string.IsNullOrWhiteSpace(runFlowConfig.EndingSceneKey))
        {
            Debug.LogError("[RunFlow] Ending scene is not configured.");
            return;
        }

        S_GameEvent.SceneLoadRequested(runFlowConfig.EndingSceneKey);
    }

    private void HandleStoryTrigger(string triggerID)
    {
        if (phase != RunPhase.Facility
            || runFlowConfig == null
            || string.IsNullOrWhiteSpace(runFlowConfig.DrRoomEndingStoryId)
            || triggerID != runFlowConfig.DrRoomEndingStoryId)
        {
            return;
        }

        EnterEnding();
    }

    private void LoadLevelEntry(S_LevelSceneEntry entry)
    {
        if (entry == null || !entry.HasScene)
        {
            Debug.LogError("[RunFlow] Tried to load an empty level entry.");
            return;
        }

        currentRoom = RoomId.None;
        S_GameEvent.SceneLoadRequested(entry.SceneKey);
    }

    private void ResetRunState()
    {
        phase = RunPhase.NotStarted;
        currentRoom = RoomId.None;
        fixedTrainingIndex = 0;
        completedRandomTrainingRooms = 0;
        randomTrainingTargetCount = 0;
        drawnRandomTrainingIndices.Clear();
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
}
