# Current verification evidence

Actual local Unity 6000.6.9f1 results, 14 November 2026. The delivered source is repaired. Live requests used the installed native Codex CLI and saved ChatGPT sign-in; no API key or simulated model response was used.

## Recorded end-to-end live runs

- **Root / blink — 29.9 seconds:** [run summary](../../20261114-223655-cdd302/SUMMARY.md), [actual diff](../../20261114-223655-cdd302/diff-1.txt), [224 assertions](../../20261114-223655-cdd302/verification.txt), [before replay](../../20261114-223655-cdd302/before.gif), [after replay](../../20261114-223655-cdd302/after.gif). Thirteen actual frames per GIF. Investigation found 16 failures in 43 generated cases and reduced six actions to `root -> cast`.
- **Mel nova — 28.7 seconds:** [run summary](../../20261114-224648-dcb048/SUMMARY.md), [actual diff](../../20261114-224648-dcb048/diff-1.txt), [224 assertions](../../20261114-224648-dcb048/verification.txt), [before replay](../../20261114-224648-dcb048/before.gif), [after replay](../../20261114-224648-dcb048/after.gif). Fifteen actual frames per GIF. Investigation found nine failures in 43 generated cases and reduced six actions to `nova -> cast`.

Both runs contain real CLI JSONL events, model JSON, input schemas, source snapshots, arena proof JSON, PNG frame sequences and timelines. In each, the before replay failed and the after replay passed. The combined protected-oracle/replay-checker SHA-256 before and after was identical: `51dc4030e087202d7ccde4f2bb2c2d21ca9938984f05c1e80dde4c5db8c3fdd3`. Elapsed time is the automated live interval, excluding the explicitly labeled fixture reset and this later GIF encoding. It is not a measurement of labor saved or a controlled comparison.

Mel's earlier successful expanded run, [33.8 seconds](../../20261114-222751-106121/SUMMARY.md), is retained too. Historical 104-assertion recordings in `codex-demo-backup` cover the previous, smaller version.

## Independent and integration checks

- [Current oracle](current-oracle.txt): **224/224**; includes resource preservation, control/shield combinations, normal/expiry controls, Mel kit rules, nova truth-table and timing boundaries.
- [Arena assertions](../arena-smoke.txt): **38/38**, exercising actual arena movement, collision, damage, mana, cooldown, shield, crowd-control, minion, turret, death, respawn, reset and Mel ability methods in Play.
- [Actual input checks](../input-checks.txt): **13/13** through temporary Input System devices, including left-click selection and continuing attacks after release.
- [Complete scene replays](replays/results.txt): **4/4** for root/blink, root/dash, nova/missile and shield/charm, with expiry controls.
- [Expression/source guards](../workbench-guards.txt): **17/17**, including nova identifier allowlists and injection rejection.
- [Recovery checks](../recovery-checks.txt): **3/3** for a missing native executable, compilation timeout release and stale-source rejection. Live quota exhaustion, interrupted login, and an actual multi-attempt model correction remain untested.
- [Always-block nova mutant](always-block-mutation.txt): rejected at **209/224**. [Never-destroy nova fixture](never-block-mutation.txt): rejected at **217/224**. Assertions were not weakened to make them pass.
- [Visual lifecycle](../visual-lifecycle.txt): repeated Play sessions, no missing sprites and all four portraits loaded. [Visual QA](VISUAL_QA.md) includes reviewed frames, fix pass and honest presentation scores.
- [Final Console](final-console.json): inspect `groundTruth`, not the collector's historical cumulative counts. Earlier compilation/sprite failures remain in history. Unity's pre-existing AI Assistant may log `NoSubscription` as an informational log; Dex's signed-in Codex workflow does not use that service.

`pre-fix` deliberately retains the failed Solar Snare integration replay (the damage tick was frame-dependent) and the incorrect shared portrait binding capture. They were repaired and checked again. Failed timestamped workbench runs are also retained; they are never presented as successes.

The finite 43-case exploration and action-deletion reducer cover supported simplified rule families. They do not discover arbitrary game bugs, guarantee live-League parity, or replace human code review. Read the [architecture and limitations](../../../DEX_README.md) and [short demo script](../../../DEMO_SCRIPT.md).
