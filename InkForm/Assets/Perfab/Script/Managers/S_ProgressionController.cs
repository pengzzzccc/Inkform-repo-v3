using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the run flow above S_GameManager (the dumb scene loader).
/// Phases: fixed tutorials (linear) -> random training (shuffle bag, N rooms) -> facility (room graph) -> ending.
/// Persistent child of ManagerRoot.prefab; run state survives scene loads.
/// </summary>
public class S_ProgressionController : MonoBehaviour
{
    public enum ProgressionPhase { FixedTutorial, RandomTraining, Facility, Ending }

    public static S_ProgressionController Instance { get; private set; }

    [Header("Graph & Scenes")]
    [SerializeField] private S_RoomGraph roomGraph;
    [SerializeField] private S_SceneReference endScene = new S_SceneReference("Assets/Scenes/For_game/END.unity");

    [Header("Random Training Pool")]
    [SerializeField] private S_SceneReference[] trainingRoomPool;
    [SerializeField, Min(0)] private int minTrainRooms = 2;
    [SerializeField, Min(0)] private int maxTrainRooms = 4;

    [Header("Dr.R Ending")]
    [Tooltip("Story trigger id that ends the game from Dr.R's room, e.g. 'Dialogue_End_DrR'.")]
    [SerializeField] private string drRoomEndingStoryId = "Dialogue_End_DrR";

    // Run state (survives scene loads).
    private ProgressionPhase phase = ProgressionPhase.FixedTutorial;
    private RoomId currentRoom = RoomId.None;
    private int completedTrainingRooms;
    private int trainingTargetN;
    private readonly List<int> drawnTrainingIndices = new List<int>();

    public ProgressionPhase Phase => phase;
    public RoomId CurrentRoom => currentRoom;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }
        Instance = this;
        Debug.Log("[Progression] Controller ready.");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        S_GameEvent.OnFixedTutorialsComplete += HandleFixedTutorialsComplete;
        S_GameEvent.OnRoomEnterRequested += HandleRoomEnterRequested;
        S_GameEvent.OnReachEnding += HandleReachEnding;
        S_GameEvent.OnStoryTrigger += HandleStoryTrigger;
        S_GameEvent.OnGameStart += ResetRun;
        S_GameEvent.OnGameRestart += ResetRun;
        S_GameEvent.OnStartFreshGameRequested += ResetRun;
    }

    void OnDisable()
    {
        S_GameEvent.OnFixedTutorialsComplete -= HandleFixedTutorialsComplete;
        S_GameEvent.OnRoomEnterRequested -= HandleRoomEnterRequested;
        S_GameEvent.OnReachEnding -= HandleReachEnding;
        S_GameEvent.OnStoryTrigger -= HandleStoryTrigger;
        S_GameEvent.OnGameStart -= ResetRun;
        S_GameEvent.OnGameRestart -= ResetRun;
        S_GameEvent.OnStartFreshGameRequested -= ResetRun;
    }

    // Set when the last fixed tutorial is cleared, so the exit hands off to training
    // regardless of event subscription order.
    private bool fixedHandoffPending;

    /// <summary>
    /// Called by the last fixed tutorial on goal so the next exit begins random training.
    /// </summary>
    public void RequestFixedHandoff()
    {
        if (phase == ProgressionPhase.FixedTutorial)
        {
            fixedHandoffPending = true;
            Debug.Log("[Progression] Fixed-tutorial handoff armed; next exit starts random training.");
        }
    }

    /// <summary>
    /// Called by S_GameManager.HandleLevelExitRequested.
    /// Returns true if the controller consumed the event (GameManager must not linear-advance).
    /// </summary>
    public bool HandleLevelExit()
    {
        switch (phase)
        {
            case ProgressionPhase.FixedTutorial:
                if (fixedHandoffPending)
                {
                    fixedHandoffPending = false;
                    BeginRandomTraining();
                    return true;
                }
                return false; // legacy linear path for fixed tutorials
            case ProgressionPhase.RandomTraining:
                AdvanceTraining();
                return true;
            case ProgressionPhase.Facility:
            case ProgressionPhase.Ending:
            default:
                // Facility transitions go through RoomEnterRequested, not LevelExit.
                return true;
        }
    }

    public void ResetRun()
    {
        phase = ProgressionPhase.FixedTutorial;
        currentRoom = RoomId.None;
        completedTrainingRooms = 0;
        trainingTargetN = 0;
        drawnTrainingIndices.Clear();
        fixedHandoffPending = false;
    }

    private void HandleFixedTutorialsComplete()
    {
        RequestFixedHandoff();
    }

    private void BeginRandomTraining()
    {
        if (trainingRoomPool == null || trainingRoomPool.Length == 0)
        {
            Debug.LogWarning("[Progression] Training pool empty; handing off to facility directly.");
            HandoffToFacility();
            return;
        }

        phase = ProgressionPhase.RandomTraining;
        completedTrainingRooms = 0;
        drawnTrainingIndices.Clear();

        int maxN = Mathf.Clamp(maxTrainRooms, 0, trainingRoomPool.Length);
        int minN = Mathf.Clamp(minTrainRooms, 0, maxN);
        trainingTargetN = Random.Range(minN, maxN + 1);

        if (trainingTargetN <= 0)
        {
            HandoffToFacility();
            return;
        }

        Debug.Log($"[Progression] Random training started: target {trainingTargetN} room(s) from a pool of {trainingRoomPool.Length}.");
        LoadTrainingScene(DrawNextTrainingIndex());
    }

    private void AdvanceTraining()
    {
        completedTrainingRooms++;
        Debug.Log($"[Progression] Training room cleared ({completedTrainingRooms}/{trainingTargetN}).");

        if (completedTrainingRooms >= trainingTargetN || drawnTrainingIndices.Count >= trainingRoomPool.Length)
        {
            HandoffToFacility();
            return;
        }

        LoadTrainingScene(DrawNextTrainingIndex());
    }

    private int DrawNextTrainingIndex()
    {
        // No-repeat shuffle bag: pick a random unused index.
        List<int> available = new List<int>();
        for (int i = 0; i < trainingRoomPool.Length; i++)
        {
            if (!drawnTrainingIndices.Contains(i))
                available.Add(i);
        }

        if (available.Count == 0)
            return -1;

        int picked = available[Random.Range(0, available.Count)];
        drawnTrainingIndices.Add(picked);
        return picked;
    }

    private void LoadTrainingScene(int index)
    {
        if (index < 0 || index >= trainingRoomPool.Length || trainingRoomPool[index] == null || !trainingRoomPool[index].IsValid)
        {
            Debug.LogWarning($"[Progression] Invalid training scene at index {index}; handing off to facility.");
            HandoffToFacility();
            return;
        }

        S_GameEvent.SceneLoadRequested(trainingRoomPool[index].RuntimeKey);
    }

    private void HandoffToFacility()
    {
        phase = ProgressionPhase.Facility;

        RoomId firstRoom = PickFirstFacilityRoom();
        if (firstRoom == RoomId.None)
        {
            Debug.LogError("[Progression] No facility entry rooms in RoomGraph.");
            return;
        }

        Debug.Log($"[Progression] Entering facility via {firstRoom}.");
        S_GameEvent.FacilityEntered();
        LoadRoom(firstRoom);
    }

    private RoomId PickFirstFacilityRoom()
    {
        if (roomGraph == null)
            return RoomId.None;

        List<RoomId> entries = roomGraph.GetFirstFacilityRooms();
        if (entries.Count == 0)
            return RoomId.None;

        return entries[Random.Range(0, entries.Count)];
    }

    private void HandleRoomEnterRequested(string roomIdName)
    {
        if (phase != ProgressionPhase.Facility)
            return;

        if (!System.Enum.TryParse(roomIdName, out RoomId target) || target == RoomId.None)
        {
            Debug.LogWarning($"[Progression] Unknown room id '{roomIdName}'.");
            return;
        }

        RequestRoom(target);
    }

    public void RequestRoom(RoomId target)
    {
        if (roomGraph == null)
        {
            Debug.LogError("[Progression] No RoomGraph assigned.");
            return;
        }

        // currentRoom may be None right after handoff (scene not yet resolved); allow first move.
        if (currentRoom != RoomId.None && !roomGraph.AreAdjacent(currentRoom, target))
        {
            Debug.LogWarning($"[Progression] {target} is not adjacent to {currentRoom}; ignoring exit.");
            return;
        }

        if (roomGraph.IsForEnding(target))
        {
            HandleReachEnding();
            return;
        }

        LoadRoom(target);
    }

    private void LoadRoom(RoomId id)
    {
        string sceneKey = roomGraph != null ? roomGraph.GetSceneKey(id) : string.Empty;
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogError($"[Progression] Room {id} has no scene assigned in RoomGraph.");
            return;
        }

        currentRoom = id;
        S_GameEvent.SceneLoadRequested(sceneKey);
    }

    private void HandleStoryTrigger(string triggerID)
    {
        if (phase == ProgressionPhase.Facility
            && !string.IsNullOrEmpty(drRoomEndingStoryId)
            && triggerID == drRoomEndingStoryId)
        {
            HandleReachEnding();
        }
    }

    private void HandleReachEnding()
    {
        if (phase == ProgressionPhase.Ending)
            return;

        phase = ProgressionPhase.Ending;

        string endKey = endScene != null && endScene.IsValid ? endScene.RuntimeKey : string.Empty;
        if (string.IsNullOrWhiteSpace(endKey))
        {
            Debug.LogError("[Progression] End scene not configured.");
            return;
        }

        S_GameEvent.SceneLoadRequested(endKey);
    }
}
