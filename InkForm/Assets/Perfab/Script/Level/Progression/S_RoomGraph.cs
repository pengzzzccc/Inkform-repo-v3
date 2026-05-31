using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Facility topology: room nodes, scene refs, adjacency, and flags.
/// Single source of truth for room navigation. The progression controller reads this.
/// Adjacency is treated as undirected (author once per pair).
/// </summary>
[CreateAssetMenu(fileName = "RoomGraph", menuName = "InkForm/Room Graph")]
public class S_RoomGraph : ScriptableObject
{
    [Serializable]
    public class RoomNode
    {
        public RoomId id;
        public S_SceneReference scene = new S_SceneReference();
        public RoomId[] adjacentRooms;
        [Tooltip("Reachable directly from TR when leaving training (ComR, BF).")]
        public bool isFacilityEntry;
        [Tooltip("Entering this room is the 'factory clear' ending (For).")]
        public bool isForEnding;
    }

    [SerializeField] private RoomNode[] rooms;

    public RoomNode GetNode(RoomId id)
    {
        if (rooms == null)
            return null;

        foreach (RoomNode node in rooms)
        {
            if (node != null && node.id == id)
                return node;
        }
        return null;
    }

    public string GetSceneKey(RoomId id)
    {
        RoomNode node = GetNode(id);
        return node != null && node.scene != null ? node.scene.RuntimeKey : string.Empty;
    }

    /// <summary>Undirected adjacency check: true if either node lists the other.</summary>
    public bool AreAdjacent(RoomId a, RoomId b)
    {
        if (a == b)
            return false;

        return ListsContains(GetNode(a), b) || ListsContains(GetNode(b), a);
    }

    public bool IsForEnding(RoomId id)
    {
        RoomNode node = GetNode(id);
        return node != null && node.isForEnding;
    }

    /// <summary>Rooms reachable directly from TR (random first facility room source).</summary>
    public List<RoomId> GetFirstFacilityRooms()
    {
        List<RoomId> result = new List<RoomId>();
        if (rooms == null)
            return result;

        foreach (RoomNode node in rooms)
        {
            if (node != null && node.isFacilityEntry)
                result.Add(node.id);
        }
        return result;
    }

    /// <summary>Reverse map: active scene key -> room id, for tracking current room.</summary>
    public bool TryResolve(string sceneKey, out RoomId id)
    {
        id = RoomId.None;
        if (rooms == null || string.IsNullOrWhiteSpace(sceneKey))
            return false;

        foreach (RoomNode node in rooms)
        {
            if (node != null && node.scene != null && node.scene.Matches(sceneKey))
            {
                id = node.id;
                return true;
            }
        }
        return false;
    }

    private static bool ListsContains(RoomNode node, RoomId target)
    {
        if (node == null || node.adjacentRooms == null)
            return false;

        foreach (RoomId adj in node.adjacentRooms)
        {
            if (adj == target)
                return true;
        }
        return false;
    }
}
