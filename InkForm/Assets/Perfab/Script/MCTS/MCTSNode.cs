using System.Collections.Generic;

/// <summary>
/// Represents a single node in the MCTS search tree.
/// Each node stores a game state snapshot, visit statistics,
/// and the action that led to it from its parent.
/// </summary>
public class MCTSNode
{
    // ── Tree structure ──
    public MCTSNode Parent;
    public List<MCTSNode> Children;
    public BotAction ActionFromParent;

    // ── State ──
    public MCTSGameState State;

    // ── Statistics ──
    public int VisitCount;
    public float TotalReward;

    // ── Expansion tracking ──
    private List<BotAction> untriedActions;

    /// <summary>
    /// Creates a new MCTS node with the given state.
    /// If parent is null, this is the root node.
    /// </summary>
    public MCTSNode(MCTSGameState state, MCTSNode parent = null, BotAction actionFromParent = BotAction.Idle)
    {
        State = state;
        Parent = parent;
        ActionFromParent = actionFromParent;
        Children = new List<MCTSNode>();
        VisitCount = 0;
        TotalReward = 0f;
        untriedActions = new List<BotAction>();
    }

    /// <summary>
    /// Initializes untried actions for expansion.
    /// Should be called once after construction.
    /// </summary>
    public void InitializeUntriedActions(BotAction[] allActions)
    {
        untriedActions = new List<BotAction>(allActions);
    }

    /// <summary>
    /// Whether all possible child actions have been expanded.
    /// </summary>
    public bool IsFullyExpanded => untriedActions != null && untriedActions.Count == 0;

    /// <summary>
    /// Whether this node is a leaf (no children expanded yet).
    /// </summary>
    public bool IsLeaf => Children == null || Children.Count == 0;

    /// <summary>
    /// Whether the state at this node is terminal (dead or reached goal).
    /// </summary>
    public bool IsTerminal => State != null && (State.isDead || State.reachedGoal);

    /// <summary>
    /// Gets the average reward value (Q/N) for this node.
    /// </summary>
    public float AverageReward => VisitCount > 0 ? TotalReward / VisitCount : 0f;

    /// <summary>
    /// Returns true if this node still has untried actions.
    /// </summary>
    public bool HasUntriedActions => untriedActions != null && untriedActions.Count > 0;

    /// <summary>
    /// Picks a random untried action and removes it from the list.
    /// Returns the action to expand.
    /// </summary>
    public BotAction PickUntriedAction()
    {
        if (!HasUntriedActions)
            return BotAction.Idle;

        int idx = UnityEngine.Random.Range(0, untriedActions.Count);
        BotAction action = untriedActions[idx];
        untriedActions.RemoveAt(idx);
        return action;
    }

    /// <summary>
    /// Computes the UCT (Upper Confidence Bound for Trees) value.
    /// UCT = Q/N + explorationConstant * sqrt(ln(N_parent) / N)
    /// Returns float.MaxValue if this node has never been visited
    /// (encouraging exploration of unvisited nodes).
    /// </summary>
    public float GetUCTValue(float explorationConstant)
    {
        if (VisitCount == 0)
            return float.MaxValue;

        float exploitation = TotalReward / VisitCount;
        float exploration = explorationConstant *
            (float)System.Math.Sqrt(System.Math.Log(Parent.VisitCount) / VisitCount);

        return exploitation + exploration;
    }

    /// <summary>
    /// Selects the child with the highest UCT value.
    /// </summary>
    public MCTSNode SelectChildUCT(float explorationConstant)
    {
        if (Children == null || Children.Count == 0)
            return null;

        MCTSNode best = null;
        float bestUCT = float.MinValue;

        for (int i = 0; i < Children.Count; i++)
        {
            float uct = Children[i].GetUCTValue(explorationConstant);
            if (uct > bestUCT)
            {
                bestUCT = uct;
                best = Children[i];
            }
        }

        return best;
    }

    /// <summary>
    /// Selects the best child based on visit count (most explored = most promising).
    /// Used for final action selection after MCTS search.
    /// </summary>
    public MCTSNode SelectBestChild()
    {
        if (Children == null || Children.Count == 0)
            return null;

        MCTSNode best = null;
        int bestVisits = -1;

        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].VisitCount > bestVisits)
            {
                bestVisits = Children[i].VisitCount;
                best = Children[i];
            }
        }

        return best;
    }

    /// <summary>
    /// Selects the best child based on average reward (Q/N).
    /// Alternative strategy for final action selection.
    /// </summary>
    public MCTSNode SelectBestChildByAvgReward()
    {
        if (Children == null || Children.Count == 0)
            return null;

        MCTSNode best = null;
        float bestAvg = float.MinValue;

        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].VisitCount == 0) continue;
            float avg = Children[i].AverageReward;
            if (avg > bestAvg)
            {
                bestAvg = avg;
                best = Children[i];
            }
        }

        return best ?? SelectBestChild();
    }

    /// <summary>
    /// Adds a new child node for the given action and state.
    /// </summary>
    public MCTSNode AddChild(BotAction action, MCTSGameState childState)
    {
        MCTSNode child = new MCTSNode(childState, this, action);
        if (Children == null)
            Children = new List<MCTSNode>();
        Children.Add(child);
        return child;
    }

    /// <summary>
    /// Backpropagates the reward up the tree.
    /// </summary>
    public void Backpropagate(float reward)
    {
        MCTSNode node = this;
        while (node != null)
        {
            node.VisitCount++;
            node.TotalReward += reward;
            node = node.Parent;
        }
    }

    /// <summary>
    /// Returns a debug summary string for logging.
    /// </summary>
    public string GetDebugSummary()
    {
        return $"[{ActionFromParent}] visits={VisitCount} avgR={AverageReward:F2} totalR={TotalReward:F1}";
    }
}