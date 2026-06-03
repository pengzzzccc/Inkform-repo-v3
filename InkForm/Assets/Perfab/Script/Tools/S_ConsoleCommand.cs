using System;
using System.Collections.Generic;

/// <summary>
/// A single command entry for the in-game S_CommandConsole.
/// Plain data: name + aliases for lookup, Usage/Description for the fuzzy-search help,
/// a Handler that runs the command and returns feedback text, and an optional ArgCompleter
/// that supplies completion candidates for the current (partial) argument token.
/// </summary>
public sealed class S_ConsoleCommand
{
    public string Name;
    public string[] Aliases;
    public string Usage;
    public string Description;
    public Func<string[], string> Handler;
    public Func<string, IEnumerable<string>> ArgCompleter;

    public S_ConsoleCommand(
        string name,
        string usage,
        string description,
        Func<string[], string> handler,
        string[] aliases = null,
        Func<string, IEnumerable<string>> argCompleter = null)
    {
        Name = name;
        Usage = usage;
        Description = description;
        Handler = handler;
        Aliases = aliases ?? Array.Empty<string>();
        ArgCompleter = argCompleter;
    }

    public bool Matches(string token)
    {
        if (string.Equals(Name, token, StringComparison.OrdinalIgnoreCase))
            return true;

        for (int i = 0; i < Aliases.Length; i++)
        {
            if (string.Equals(Aliases[i], token, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
