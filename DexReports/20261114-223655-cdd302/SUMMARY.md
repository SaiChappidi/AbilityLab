# Dex run

Run type: Live Codex CLI

Result: Passed — 224 / 224 independent checks passed

Elapsed: 29.9 seconds. No manual baseline has been measured.

Independent compiled checks passed and the actual arena replay passed. Before/after evidence saved automatically.

## Timeline

0.0s  /  Investigating
6.9s  /  Reproduced
6.9s  /  Capturing before
11.1s  /  Repairing
18.6s  /  Compiling
25.0s  /  Verifying
25.0s  /  Replaying
29.9s  /  Passed

## Exploration

Deterministic exploration: 43 generated timing/matchup cases; 16 failures.
Morgana -> Ezreal at 0.000s: expected cast=False, actual=True
Morgana -> Ezreal at 0.500s: expected cast=False, actual=True
Morgana -> Ezreal at 1.499s: expected cast=False, actual=True
Morgana -> Ezreal at 1.500s: expected cast=False, actual=True
Morgana -> Ezreal at 1.999s: expected cast=False, actual=True
Morgana -> Ahri at 0.000s: expected cast=False, actual=True
Morgana -> Ahri at 0.500s: expected cast=False, actual=True
Morgana -> Ahri at 1.499s: expected cast=False, actual=True
Morgana -> Ahri at 1.500s: expected cast=False, actual=True
Morgana -> Ahri at 1.999s: expected cast=False, actual=True
Mel -> Ezreal at 0.000s: expected cast=False, actual=True
Mel -> Ezreal at 0.500s: expected cast=False, actual=True
Action deletion: 6 original actions -> 2 required actions (root -> cast)
Scope: finite generated simplified scenarios, not arbitrary gameplay discovery.

The agent has read-only tools. The workbench applies validated expressions and verifies them independently. See source diffs, verifier hashes, CLI event transcripts and before/after replay captures in this directory.