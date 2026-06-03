using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// In-game developer command console (Minecraft-style). Press Tab to toggle a panel in the
/// bottom-left corner, type commands to drive gameplay/config. Provides fuzzy command search
/// (showing each command's usage/syntax) and Tab completion. Built on IMGUI (like
/// S_PerformanceMonitor) so it needs no scene EventSystem and captures text natively.
///
/// Commands prefer firing S_GameEvent (matching the project's decoupled event style); where no
/// event exists they call the relevant singleton. Mount on the ManagerRoot prefab as a sibling
/// of the other manager tools so it persists across scenes.
/// </summary>
[DisallowMultipleComponent]
public class S_CommandConsole : MonoBehaviour
{
    private const string InputLockId = "Console";
    private const string InputControlName = "ConsoleInputField";
    private const int MaxSuggestions = 6;
    private const int MaxOutputHistory = 200;

    public static S_CommandConsole Instance { get; private set; }

    private readonly Dictionary<string, S_ConsoleCommand> commandsByKey = new Dictionary<string, S_ConsoleCommand>(StringComparer.OrdinalIgnoreCase);
    private readonly List<S_ConsoleCommand> commands = new List<S_ConsoleCommand>();
    private readonly List<string> output = new List<string>();
    private readonly List<string> history = new List<string>();
    private readonly List<S_ConsoleCommand> suggestions = new List<S_ConsoleCommand>(MaxSuggestions);

    private bool isOpen;
    private bool justOpened;
    private bool focusRequested;
    private string input = string.Empty;
    private int historyIndex = -1;
    private Vector2 outputScroll;
    private bool scrollToBottom;

    private GUIStyle panelStyle;
    private GUIStyle inputStyle;
    private GUIStyle outputStyle;
    private GUIStyle suggestionStyle;
    private bool stylesReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            S_ManagerRoot.DestroyDuplicate(this);
            return;
        }
        Instance = this;
        RegisterBuiltInCommands();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        if (isOpen)
            Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Tab toggles: open when closed; close when open with an empty input; when open with
        // typed text, OnGUI handles Tab as command/argument completion instead.
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (!isOpen)
                Open();
            else if (string.IsNullOrEmpty(input))
                Close();
        }
    }

    /// <summary>Register a command. Other systems may add their own commands at runtime.</summary>
    public void Register(S_ConsoleCommand command)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.Name))
            return;

        if (commandsByKey.ContainsKey(command.Name))
            return;

        commands.Add(command);
        commandsByKey[command.Name] = command;
        if (command.Aliases != null)
        {
            foreach (string alias in command.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias) && !commandsByKey.ContainsKey(alias))
                    commandsByKey[alias] = command;
            }
        }
    }

    private void Open()
    {
        isOpen = true;
        justOpened = true;
        focusRequested = true;
        input = string.Empty;
        historyIndex = -1;
        S_GameEvent.PushGameplayInputLock(InputLockId);
    }

    private void Close()
    {
        isOpen = false;
        S_GameEvent.PopGameplayInputLock(InputLockId);
        GUIUtility.keyboardControl = 0;
    }

    private void OnGUI()
    {
        if (!isOpen)
            return;

        EnsureStyles();
        HandleKeys();
        RebuildSuggestions();

        const float margin = 12f;
        float width = Mathf.Clamp(Screen.width * 0.45f, 420f, 820f);
        float height = Mathf.Clamp(Screen.height * 0.4f, 240f, 520f);
        float lineH = inputStyle.lineHeight + 8f;

        GUILayout.BeginArea(new Rect(margin, Screen.height - margin - height, width, height), panelStyle);

        // Output / scrollback: word-wrapped and scrollable so it fits the panel completely.
        if (scrollToBottom)
        {
            outputScroll.y = float.MaxValue;
            scrollToBottom = false;
        }
        outputScroll = GUILayout.BeginScrollView(outputScroll, GUILayout.ExpandHeight(true));
        for (int i = 0; i < output.Count; i++)
            GUILayout.Label(output[i], outputStyle);
        GUILayout.EndScrollView();

        // Fuzzy suggestions (command + usage + description)
        for (int i = 0; i < suggestions.Count; i++)
        {
            S_ConsoleCommand c = suggestions[i];
            string prefix = i == 0 ? "<b>></b> " : "   ";
            GUILayout.Label($"{prefix}<b>{c.Usage}</b>  —  {c.Description}", suggestionStyle);
        }

        // Input line
        GUI.SetNextControlName(InputControlName);
        input = GUILayout.TextField(input ?? string.Empty, inputStyle, GUILayout.Height(lineH));

        GUILayout.EndArea();

        if (focusRequested)
        {
            GUI.FocusControl(InputControlName);
            focusRequested = false;
        }

        justOpened = false;
    }

    private void HandleKeys()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;

        switch (e.keyCode)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                Submit();
                e.Use();
                break;

            case KeyCode.Escape:
                Close();
                e.Use();
                break;

            case KeyCode.Tab:
                if (!justOpened)
                    Complete();
                e.Use();
                break;

            case KeyCode.UpArrow:
                CycleHistory(1);
                e.Use();
                break;

            case KeyCode.DownArrow:
                CycleHistory(-1);
                e.Use();
                break;
        }
    }

    private void Submit()
    {
        string line = (input ?? string.Empty).Trim();
        input = string.Empty;
        historyIndex = -1;

        if (line.Length == 0)
            return;

        if (history.Count == 0 || history[history.Count - 1] != line)
            history.Add(line);

        AppendOutput("> " + line);

        string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string name = tokens[0];
        if (!commandsByKey.TryGetValue(name, out S_ConsoleCommand command))
        {
            AppendOutput($"Unknown command '{name}'. Type 'help'.");
            return;
        }

        string[] args = new string[tokens.Length - 1];
        Array.Copy(tokens, 1, args, 0, args.Length);

        try
        {
            string result = command.Handler != null ? command.Handler(args) : null;
            if (!string.IsNullOrEmpty(result))
                AppendOutput(result);
        }
        catch (Exception ex)
        {
            AppendOutput($"Error: {ex.Message}");
        }
    }

    private void Complete()
    {
        string text = input ?? string.Empty;
        int firstSpace = text.IndexOf(' ');

        // Completing an argument: use the resolved command's ArgCompleter.
        if (firstSpace >= 0)
        {
            string cmdToken = text.Substring(0, firstSpace);
            if (commandsByKey.TryGetValue(cmdToken, out S_ConsoleCommand cmd) && cmd.ArgCompleter != null)
            {
                int lastSpace = text.LastIndexOf(' ');
                string prefixPart = text.Substring(0, lastSpace + 1);
                string argPartial = text.Substring(lastSpace + 1);
                IEnumerable<string> candidates = cmd.ArgCompleter(argPartial);
                if (candidates != null)
                {
                    foreach (string cand in candidates)
                    {
                        if (string.IsNullOrEmpty(cand))
                            continue;
                        if (argPartial.Length == 0 || cand.StartsWith(argPartial, StringComparison.OrdinalIgnoreCase))
                        {
                            input = prefixPart + cand;
                            focusRequested = true;
                            return;
                        }
                    }
                }
            }
            return;
        }

        // Completing the command name (top suggestion); add a trailing space when it takes args.
        if (suggestions.Count == 0)
            return;
        string completed = suggestions[0].Name;
        if (!string.IsNullOrEmpty(suggestions[0].Usage) && suggestions[0].Usage.Contains("<"))
            completed += " ";
        input = completed;
        focusRequested = true;
    }

    private void CycleHistory(int direction)
    {
        if (history.Count == 0)
            return;

        if (historyIndex < 0)
            historyIndex = history.Count;

        historyIndex = Mathf.Clamp(historyIndex - direction, 0, history.Count);
        input = historyIndex >= history.Count ? string.Empty : history[historyIndex];
    }

    private void RebuildSuggestions()
    {
        suggestions.Clear();

        string token = (input ?? string.Empty).Trim();
        int space = token.IndexOf(' ');
        if (space >= 0)
            token = token.Substring(0, space);

        if (token.Length == 0)
            return;

        // Score: prefix(3) > substring(2) > subsequence(1) > none(0); break ties by name length.
        List<(S_ConsoleCommand cmd, int score)> scored = new List<(S_ConsoleCommand, int)>();
        foreach (S_ConsoleCommand c in commands)
        {
            int score = ScoreMatch(c.Name, token);
            if (c.Aliases != null)
            {
                foreach (string alias in c.Aliases)
                    score = Mathf.Max(score, ScoreMatch(alias, token));
            }
            if (score > 0)
                scored.Add((c, score));
        }

        scored.Sort((a, b) =>
        {
            if (a.score != b.score) return b.score.CompareTo(a.score);
            return a.cmd.Name.Length.CompareTo(b.cmd.Name.Length);
        });

        for (int i = 0; i < scored.Count && i < MaxSuggestions; i++)
            suggestions.Add(scored[i].cmd);
    }

    private static int ScoreMatch(string candidate, string token)
    {
        if (string.IsNullOrEmpty(candidate))
            return 0;

        string c = candidate.ToLowerInvariant();
        string t = token.ToLowerInvariant();

        if (c.StartsWith(t, StringComparison.Ordinal))
            return 3;
        if (c.Contains(t))
            return 2;
        return IsSubsequence(t, c) ? 1 : 0;
    }

    private static bool IsSubsequence(string needle, string haystack)
    {
        int n = 0;
        for (int i = 0; i < haystack.Length && n < needle.Length; i++)
        {
            if (haystack[i] == needle[n])
                n++;
        }
        return n == needle.Length;
    }

    private void AppendOutput(string line)
    {
        output.Add(line);
        if (output.Count > MaxOutputHistory)
            output.RemoveRange(0, output.Count - MaxOutputHistory);
        scrollToBottom = true;
    }

    private void EnsureStyles()
    {
        if (stylesReady)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeColorTexture(new Color(0.02f, 0.03f, 0.05f, 0.92f));
        panelStyle.padding = new RectOffset(8, 8, 8, 8);

        outputStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 13 };
        outputStyle.normal.textColor = new Color(0.82f, 0.88f, 0.95f, 1f);

        suggestionStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 13 };
        suggestionStyle.normal.textColor = new Color(0.65f, 0.85f, 1f, 1f);

        inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 15 };

        stylesReady = true;
    }

    private static Texture2D MakeColorTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;
        return tex;
    }

    // ──────────────────────────────────────────────
    //  Built-in commands
    // ──────────────────────────────────────────────

    private void RegisterBuiltInCommands()
    {
        Register(new S_ConsoleCommand("help", "help [command]", "List commands or show one command's usage.", CmdHelp));

        Register(new S_ConsoleCommand("respawn", "respawn", "Respawn at the last checkpoint.",
            _ => { S_GameEvent.RespawnRequested(); return "Respawn requested."; }));

        Register(new S_ConsoleCommand("kill", "kill", "Kill the player.",
            _ => { S_GameEvent.PlayerDied(); return "Player killed."; }, new[] { "die" }));

        Register(new S_ConsoleCommand("scene", "scene <key>", "Load a scene by name/path key.",
            args =>
            {
                if (args.Length < 1) return "Usage: scene <key>";
                S_GameEvent.SceneLoadRequested(args[0]);
                return $"Loading scene '{args[0]}'...";
            }));

        Register(new S_ConsoleCommand("room", "room <RoomId>", "Request a facility room transition.",
            args =>
            {
                if (args.Length < 1) return "Usage: room <RoomId> (" + string.Join("/", Enum.GetNames(typeof(RoomId))) + ")";
                if (!Enum.TryParse(args[0], true, out RoomId room))
                    return $"Unknown RoomId '{args[0]}'.";
                S_GameEvent.RoomEnterRequested(room);
                return $"Room transition -> {room}.";
            },
            argCompleter: _ => Enum.GetNames(typeof(RoomId))));

        Register(new S_ConsoleCommand("ending", "ending", "Trigger the ending.",
            _ => { S_GameEvent.EndingRequested(); return "Ending requested."; }));

        Register(new S_ConsoleCommand("runstart", "runstart", "Fire the run-start event.",
            _ => { S_GameEvent.RunStartRequested(); return "Run start requested."; }));

        Register(new S_ConsoleCommand("menu", "menu", "Return to the start menu.",
            _ => { S_GameEvent.ReturnToStartMenuRequested(); return "Returning to start menu."; }));

        Register(new S_ConsoleCommand("unlock", "unlock <skill>", "Unlock a skill in the skill tree.",
            args =>
            {
                if (args.Length < 1) return "Usage: unlock <skill>";
                if (S_SkillTree.Instance == null) return "No SkillTree in scene.";
                bool ok = S_SkillTree.Instance.TryUnlock(args[0]);
                return ok ? $"Unlocked '{args[0]}'." : $"Could not unlock '{args[0]}' (see Console log).";
            },
            argCompleter: _ => S_SkillTree.Instance != null ? S_SkillTree.Instance.GetAllSkillNames() : null));

        Register(new S_ConsoleCommand("unlockall", "unlockall", "Unlock every skill.",
            _ =>
            {
                if (S_SkillTree.Instance == null) return "No SkillTree in scene.";
                int count = 0;
                foreach (string name in S_SkillTree.Instance.GetAllSkillNames())
                    if (S_SkillTree.Instance.TryUnlock(name)) count++;
                return $"Unlocked {count} skill(s).";
            }));

        Register(new S_ConsoleCommand("skill", "skill <name>", "Activate an unlocked skill.",
            args =>
            {
                if (args.Length < 1) return "Usage: skill <name>";
                if (S_SkillTree.Instance == null) return "No SkillTree in scene.";
                S_SkillTree.Instance.ActivateSkill(args[0]);
                return $"Activated '{args[0]}'.";
            },
            argCompleter: _ => S_SkillTree.Instance != null ? S_SkillTree.Instance.GetAllSkillNames() : null));

        Register(new S_ConsoleCommand("energy", "energy [amount]", "Refill energy, or set it to amount.",
            args =>
            {
                S_PlayerEnergy energy = S_Player.Instance != null ? S_Player.Instance.Energy : null;
                if (energy == null) return "No player energy available.";
                if (args.Length == 0)
                {
                    energy.ResetEnergy();
                    return "Energy refilled.";
                }
                if (!TryParseFloat(args[0], out float amount)) return "Usage: energy [amount]";
                energy.SetEnergy(amount);
                return $"Energy set to {amount}.";
            }));

        Register(new S_ConsoleCommand("suspicion", "suspicion <amount|reset>", "Change or reset suspicion.",
            args =>
            {
                if (args.Length < 1) return "Usage: suspicion <amount|reset>";
                if (string.Equals(args[0], "reset", StringComparison.OrdinalIgnoreCase))
                {
                    S_GameEvent.SuspicionResetRequested();
                    return "Suspicion reset.";
                }
                if (!TryParseFloat(args[0], out float amount)) return "Usage: suspicion <amount|reset>";
                S_GameEvent.SuspicionChangeRequested(amount);
                return $"Suspicion changed by {amount}.";
            }));

        Register(new S_ConsoleCommand("tp", "tp <x> <y>", "Teleport the player.",
            args =>
            {
                if (args.Length < 2 || !TryParseFloat(args[0], out float x) || !TryParseFloat(args[1], out float y))
                    return "Usage: tp <x> <y>";
                if (S_Player.Instance == null) return "No player in scene.";
                S_Player.Instance.Teleport(new Vector2(x, y));
                return $"Teleported to ({x}, {y}).";
            }));

        Register(new S_ConsoleCommand("timescale", "timescale <value>", "Set Time.timeScale (debug).",
            args =>
            {
                if (args.Length < 1 || !TryParseFloat(args[0], out float v)) return "Usage: timescale <value>";
                Time.timeScale = Mathf.Max(0f, v);
                return $"Time scale = {Time.timeScale}.";
            }));

        Register(new S_ConsoleCommand("stopbgm", "stopbgm", "Stop background music.",
            _ => { S_GameEvent.StopBgmRequested(); return "BGM stopped."; }));

        Register(new S_ConsoleCommand("bgmvol", "bgmvol <0-1>", "Set BGM volume.",
            args =>
            {
                if (args.Length < 1 || !TryParseFloat(args[0], out float v)) return "Usage: bgmvol <0-1>";
                S_GameEvent.BgmVolumeChangeRequested(Mathf.Clamp01(v));
                return $"BGM volume = {Mathf.Clamp01(v)}.";
            }));

        Register(new S_ConsoleCommand("sfxvol", "sfxvol <0-1>", "Set SFX volume.",
            args =>
            {
                if (args.Length < 1 || !TryParseFloat(args[0], out float v)) return "Usage: sfxvol <0-1>";
                S_GameEvent.SfxVolumeChangeRequested(Mathf.Clamp01(v));
                return $"SFX volume = {Mathf.Clamp01(v)}.";
            }));

        Register(new S_ConsoleCommand("story", "story <id>", "Fire a story trigger.",
            args =>
            {
                if (args.Length < 1) return "Usage: story <id>";
                S_GameEvent.StoryTrigger(args[0]);
                return $"Story trigger '{args[0]}' fired.";
            }));

        Register(new S_ConsoleCommand("clear", "clear", "Clear the console output.",
            _ => { output.Clear(); return null; }, new[] { "cls" }));
    }

    private string CmdHelp(string[] args)
    {
        if (args.Length >= 1)
        {
            if (commandsByKey.TryGetValue(args[0], out S_ConsoleCommand c))
                return $"{c.Usage}  —  {c.Description}";
            return $"Unknown command '{args[0]}'.";
        }

        StringBuilder sb = new StringBuilder("Commands: ");
        for (int i = 0; i < commands.Count; i++)
        {
            sb.Append(commands[i].Name);
            if (i < commands.Count - 1) sb.Append(", ");
        }
        return sb.ToString();
    }

    private static bool TryParseFloat(string s, out float value)
    {
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
