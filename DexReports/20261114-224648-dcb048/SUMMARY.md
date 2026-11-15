# Dex run

Run type: Live Codex CLI

Result: Passed — 224 / 224 independent checks passed

Elapsed: 28.7 seconds. No manual baseline has been measured.

Independent compiled checks passed and the actual arena replay passed. Before/after evidence saved automatically.

## Timeline

0.0s  /  Investigating
7.2s  /  Reproduced
7.2s  /  Capturing before
11.9s  /  Repairing
16.7s  /  Compiling
23.0s  /  Verifying
23.0s  /  Replaying
28.7s  /  Passed

## Exploration

Deterministic exploration: 43 generated timing/matchup cases; 9 failures.
Rebuttal vs Morgana at 0.000s: expected hit=False, actual=True
Rebuttal vs Morgana at 0.250s: expected hit=False, actual=True
Rebuttal vs Morgana at 0.749s: expected hit=False, actual=True
Rebuttal vs Ezreal at 0.000s: expected hit=False, actual=True
Rebuttal vs Ezreal at 0.250s: expected hit=False, actual=True
Rebuttal vs Ezreal at 0.749s: expected hit=False, actual=True
Rebuttal vs Ahri at 0.000s: expected hit=False, actual=True
Rebuttal vs Ahri at 0.250s: expected hit=False, actual=True
Rebuttal vs Ahri at 0.749s: expected hit=False, actual=True
Action deletion: 6 original actions -> 2 required actions (nova -> cast)
Scope: finite generated simplified scenarios, not arbitrary gameplay discovery.

The agent has read-only tools. The workbench applies validated expressions and verifies them independently. See source diffs, verifier hashes, CLI event transcripts and before/after replay captures in this directory.