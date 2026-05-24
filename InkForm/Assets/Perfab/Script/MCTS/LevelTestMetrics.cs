using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Records metrics during MCTS-driven level testing.
/// Attach alongside MCTSBotController to collect and output
/// a readable report at test end.
/// </summary>
public class LevelTestMetrics : MonoBehaviour
{
    // ── Core metrics ──
    public bool ReachedGoal { get; private set; }
    public int DeathCount { get; private set; }
    public int StuckCount { get; private set; }
    public int TotalDecisions { get; private set; }
    public float AverageReward { get; private set; }
    public float FinalDistanceToGoal { get; private set; }
    public float TestDuration { get; private set; }

    // ── Action frequency ──
    private Dictionary<BotAction, int> actionFrequency = new Dictionary<BotAction, int>();

    // ── Reward tracking ──
    private float rewardSum;
    private int rewardSamples;

    // ── Timer ──
    private float testStartTime;
    private bool testRunning;

    void Start()
    {
        ResetMetrics();
    }

    /// <summary>
    /// Resets all metrics to initial state.
    /// </summary>
    public void ResetMetrics()
    {
        ReachedGoal = false;
        DeathCount = 0;
        StuckCount = 0;
        TotalDecisions = 0;
        AverageReward = 0f;
        FinalDistanceToGoal = 0f;
        TestDuration = 0f;
        rewardSum = 0f;
        rewardSamples = 0;
        actionFrequency.Clear();

        foreach (BotAction action in System.Enum.GetValues(typeof(BotAction)))
        {
            actionFrequency[action] = 0;
        }

        testStartTime = Time.realtimeSinceStartup;
        testRunning = true;
    }

    /// <summary>
    /// Records a decision made by the MCTS bot.
    /// </summary>
    public void RecordDecision(BotAction action, float reward)
    {
        if (!testRunning) return;

        TotalDecisions++;
        rewardSum += reward;
        rewardSamples++;
        AverageReward = rewardSum / rewardSamples;

        if (actionFrequency.ContainsKey(action))
            actionFrequency[action]++;
        else
            actionFrequency[action] = 1;
    }

    /// <summary>
    /// Records the player reaching the goal.
    /// </summary>
    public void RecordGoalReached()
    {
        ReachedGoal = true;
    }

    /// <summary>
    /// Records a death event.
    /// </summary>
    public void RecordDeath()
    {
        DeathCount++;
    }

    /// <summary>
    /// Records a stuck event (bot unable to make progress).
    /// </summary>
    public void RecordStuck()
    {
        StuckCount++;
    }

    /// <summary>
    /// Updates the final distance to goal (called periodically).
    /// </summary>
    public void UpdateFinalDistance(float distance)
    {
        FinalDistanceToGoal = distance;
    }

    /// <summary>
    /// Ends the test and computes final metrics.
    /// </summary>
    public void EndTest()
    {
        if (!testRunning) return;
        testRunning = false;
        TestDuration = Time.realtimeSinceStartup - testStartTime;
    }

    /// <summary>
    /// Generates a human-readable report of the test results.
    /// </summary>
    public string GenerateReport()
    {
        if (testRunning)
            EndTest();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════╗");
        sb.AppendLine("║         LEVEL TEST REPORT — MCTS Bot        ║");
        sb.AppendLine("╠══════════════════════════════════════════════╣");
        sb.AppendLine($"║  Reached Goal:       {(ReachedGoal ? "YES ✓" : "NO  ✗"),-23}║");
        sb.AppendLine($"║  Test Duration:      {TestDuration,10:F1}s{"            "}║");
        sb.AppendLine($"║  Total Decisions:    {TotalDecisions,10}{"            "}║");
        sb.AppendLine($"║  Death Count:        {DeathCount,10}{"            "}║");
        sb.AppendLine($"║  Stuck Count:        {StuckCount,10}{"            "}║");
        sb.AppendLine($"║  Average Reward:     {AverageReward,10:F2}{"            "}║");
        sb.AppendLine($"║  Final Dist to Goal: {FinalDistanceToGoal,10:F2}{"            "}║");
        sb.AppendLine("╠══════════════════════════════════════════════╣");
        sb.AppendLine("║  ACTION FREQUENCY                           ║");
        sb.AppendLine("╠══════════════════════════════════════════════╣");

        // Sort by frequency descending
        List<KeyValuePair<BotAction, int>> sorted = new List<KeyValuePair<BotAction, int>>(actionFrequency);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        foreach (var kvp in sorted)
        {
            if (kvp.Value == 0) continue;
            float pct = TotalDecisions > 0 ? (float)kvp.Value / TotalDecisions * 100f : 0f;
            sb.AppendLine($"║  {kvp.Key,-20} {kvp.Value,5} ({pct,5:F1}%){"            "}║");
        }

        sb.AppendLine("╠══════════════════════════════════════════════╣");
        sb.AppendLine($"║  Verdict: {(ReachedGoal ? "PASS ✓ — Level is completable" : "CONCERN — Goal not reached within test duration"),-36}║");
        sb.AppendLine("╚══════════════════════════════════════════════╝");

        return sb.ToString();
    }

    /// <summary>
    /// Logs the report to Unity console.
    /// </summary>
    public void LogReport()
    {
        string report = GenerateReport();
        Debug.Log(report);
    }

    /// <summary>
    /// Returns a quick one-line summary for in-scene display.
    /// </summary>
    public string GetQuickSummary()
    {
        return $"Goal:{(ReachedGoal ? "✓" : "✗")} Deaths:{DeathCount} Stuck:{StuckCount} " +
               $"Decisions:{TotalDecisions} AvgR:{AverageReward:F1} Dist:{FinalDistanceToGoal:F1}";
    }
}