using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RunFlowConfig", menuName = "InkForm/Level Flow/Run Flow Config")]
public class S_RunFlowConfig : ScriptableObject
{
    [Header("Menu & Ending")]
    [SerializeField] private S_SceneReference startMenuScene = new S_SceneReference("Assets/Scenes/For_game/Start.unity");
    [SerializeField] private S_SceneReference endingScene = new S_SceneReference("Assets/Scenes/For_game/END.unity");

    [Header("Training Flow")]
    [SerializeField] private S_LevelSceneEntry[] fixedTrainingLevels;
    [SerializeField] private S_LevelSceneEntry[] randomTrainingPool;
    [SerializeField, Min(0)] private int minRandomTrainingRooms = 2;
    [SerializeField, Min(0)] private int maxRandomTrainingRooms = 3;

    [Header("Facility Flow")]
    [SerializeField] private S_RoomGraph roomGraph;
    [SerializeField] private string drRoomEndingStoryId = "Dialogue_End_DrR";

    public string StartMenuSceneKey => startMenuScene != null ? startMenuScene.RuntimeKey : string.Empty;
    public string EndingSceneKey => endingScene != null ? endingScene.RuntimeKey : string.Empty;
    public S_LevelSceneEntry[] FixedTrainingLevels => fixedTrainingLevels;
    public S_LevelSceneEntry[] RandomTrainingPool => randomTrainingPool;
    public int MinRandomTrainingRooms => minRandomTrainingRooms;
    public int MaxRandomTrainingRooms => maxRandomTrainingRooms;
    public S_RoomGraph RoomGraph => roomGraph;
    public string DrRoomEndingStoryId => drRoomEndingStoryId;

    public int FixedTrainingCount => fixedTrainingLevels != null ? fixedTrainingLevels.Length : 0;
    public int RandomTrainingPoolCount => randomTrainingPool != null ? randomTrainingPool.Length : 0;

    public S_LevelSceneEntry GetFixedTrainingLevel(int index)
    {
        return fixedTrainingLevels != null && index >= 0 && index < fixedTrainingLevels.Length
            ? fixedTrainingLevels[index]
            : null;
    }

    public S_LevelSceneEntry GetRandomTrainingLevel(int index)
    {
        return randomTrainingPool != null && index >= 0 && index < randomTrainingPool.Length
            ? randomTrainingPool[index]
            : null;
    }

    public int GetRandomTrainingTargetCount()
    {
        int poolCount = RandomTrainingPoolCount;
        int maxCount = Mathf.Clamp(maxRandomTrainingRooms, 0, poolCount);
        int minCount = Mathf.Clamp(minRandomTrainingRooms, 0, maxCount);
        return Random.Range(minCount, maxCount + 1);
    }

    public bool TryFindEntryForScene(string sceneKey, out S_LevelSceneEntry entry)
    {
        if (TryFindEntryInList(fixedTrainingLevels, sceneKey, out entry))
            return true;

        return TryFindEntryInList(randomTrainingPool, sceneKey, out entry);
    }

    public List<RoomId> GetFacilityEntryRooms()
    {
        return roomGraph != null ? roomGraph.GetFirstFacilityRooms() : new List<RoomId>();
    }

    private static bool TryFindEntryInList(S_LevelSceneEntry[] levels, string sceneKey, out S_LevelSceneEntry entry)
    {
        entry = null;
        if (levels == null || string.IsNullOrWhiteSpace(sceneKey))
            return false;

        foreach (S_LevelSceneEntry candidate in levels)
        {
            if (candidate != null && candidate.MatchesScene(sceneKey))
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }
}
