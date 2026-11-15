# Recorded successful Codex demo

This is actual local evidence from the signed-in Codex CLI integration, not an offline reference repair presented as AI.

1. Open `before.gif`: the real arena permits Ezreal to blink while Morgana's projectile-applied root is active. Mana/cooldown are spent and the post-expiry attempt fails. `before.txt` captures the observed outcome.
2. Open `successful-run/run.txt`: the report is **Ezreal can blink while rooted by Morgana**. Codex selects the reproduction scenarios; the workbench executes them and returns actual feedback. The source change is in `successful-run/diff-1.txt` with before/after snapshots next to it.
3. Open `workbench.png`: the live run completed in 16.3 seconds of recorded wall time, including Unity compilation/reload and 104/104 independent checks. This is not a measured comparison against manual work.
4. Open `after.gif`: the same arena scenario blocks rooted E without consuming mana/cooldown and permits E after expiry. `after.txt` captures the observed outcome.

Each GIF contains 24 actual Game view frames recorded over six seconds at four frames per second. The PNG frame sequences are retained. They are replay recordings, not a continuous video of the model's entire run. For the agent portion, use the timestamped progress transcript, JSONL events, source diff and verification evidence.

`successful-run/codex-investigation` and `successful-run/codex-repair-1` contain the exact schemas, prompt payloads, saved model responses, raw process events and invocation metadata. Codex was logged in using ChatGPT; no API key was used. Its tools ran with a read-only sandbox and no approval prompts. The host applied only the validated expression proposal to the real source. The model's snapshot contains no verifier.

The independent verifier passed 104/104 assertions. `successful-run/verifier-before.sha256` and `verifier-after.sha256` are identical. The current focused fixture scored 85/104 before repair. A first separate live run (`../20261114-205722-d090f7`) also reached 104/104 in 17.7 seconds; the copied repeat is `../20261114-210129-fdf7c5`.

For a fresh live demo, stop Play, open Dex > Agent Workbench, check Codex sign-in, reset the developer fixture, run Codex investigation + repair, inspect the actual diff/result, and click Replay interaction in arena. If a network or subscription limit prevents a fresh run, clearly introduce these files as a previously recorded successful run.

Scope: constrained rule synthesis and predefined reproductions for a simplified Unity combat sandbox. This does not establish production reliability, live-League accuracy, or time savings. Remote quota exhaustion and multi-attempt AI recovery have not been tested.
