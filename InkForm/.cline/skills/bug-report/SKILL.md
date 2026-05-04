---
name: bug-report
description: "Creates a structured bug report from a description, or analyzes code to identify potential bugs. Use when the user says 'bug report', 'report a bug', 'found a bug', 'analyze for bugs', 'file a bug', or wants to document a defect with full reproduction steps, severity assessment, and technical context. Also supports verify and close modes for bug lifecycle management."
---

# Bug Report

Adapted from Claude Code Game Studios (MIT License).

This skill creates structured bug reports or analyzes code for potential bugs. Every bug report includes full reproduction steps, severity assessment, classification, and technical context.

## Modes

- **Description mode** (default): Generate a structured bug report from user description
- **Analyze mode** (`analyze [path]`): Read code and identify potential bugs
- **Verify mode** (`verify [BUG-ID]`): Confirm a fix resolved the bug
- **Close mode** (`close [BUG-ID]`): Mark a verified bug as closed

## Description Mode

1. Parse the user's description for: what broke, when, how to reproduce, expected behavior
2. Search the codebase for related files to add context
3. Draft the bug report:

```markdown
# Bug Report

## Summary
**Title**: [Concise, descriptive title]
**ID**: BUG-[NNNN]
**Severity**: [S1-Critical / S2-Major / S3-Minor / S4-Trivial]
**Priority**: [P1-Immediate / P2-Next Sprint / P3-Backlog / P4-Wishlist]
**Status**: Open
**Reported**: [Date]

## Classification
- **Category**: [Gameplay / UI / Audio / Visual / Performance / Crash / Network]
- **System**: [Which game system is affected]
- **Frequency**: [Always / Often >50% / Sometimes 10-50% / Rare <10%]
- **Regression**: [Yes / No / Unknown]

## Environment
- **Build**: [Version or commit hash]
- **Platform**: [OS, hardware if relevant]
- **Scene/Level**: [Where in the game]
- **Game State**: [Relevant state]

## Reproduction Steps
**Preconditions**: [Required state before starting]

1. [Exact step 1]
2. [Exact step 2]
3. [Exact step 3]

**Expected Result**: [What should happen]
**Actual Result**: [What actually happens]

## Technical Context
- **Likely affected files**: [List from codebase search]
- **Related systems**: [What else might be involved]
- **Possible root cause**: [If identifiable]

## Evidence
- **Logs**: [Relevant log output]
- **Visual**: [Description of visual evidence]

## Related Issues
- [Links to related bugs or design documents]
```

## Analyze Mode

1. Read the target file(s)
2. Identify potential bugs: null references, off-by-one errors, race conditions, unhandled edge cases, resource leaks, incorrect state transitions
3. For each potential bug, generate a report with likely trigger scenario and recommended fix

## Verify Mode

1. Read the bug report file
2. Check if the root cause code path still exists or has been changed
3. Run related tests if available
4. Produce verdict: VERIFIED FIXED / STILL PRESENT / CANNOT VERIFY

## Close Mode

1. Confirm status is "Verified Fixed"
2. Append closure record with date, resolution, fix commit, and regression test info
3. Update status to Closed

## Output

Ask before writing to `production/qa/bugs/BUG-[NNNN].md`.

After filing, suggest:
- Run bug-triage to prioritize alongside existing open bugs
- If S1 or S2: consider emergency fix workflow
- After a fix: run `verify [BUG-ID]` before closing
