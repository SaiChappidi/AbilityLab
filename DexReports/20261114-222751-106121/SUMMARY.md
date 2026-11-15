# Dex run

Run type: Live Codex CLI

Result: Passed — 224 / 224 independent checks passed

Elapsed: 33.8 seconds. No manual baseline has been measured.

Independent compiled checks passed and the actual arena replay passed. Before/after evidence saved automatically.

## Timeline

0.0s  /  Investigating
8.7s  /  Reproduced
8.7s  /  Capturing before
13.4s  /  Repairing
19.6s  /  Compiling
28.4s  /  Verifying
28.4s  /  Replaying
33.8s  /  Passed

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