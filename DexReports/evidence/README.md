# Verification evidence

**Current expanded scope:** see [v2 evidence](v2/README.md) for Mel, four encounters, 224 protected assertions, 38 arena checks, 17 grammar/source guards, automatic before/after scene verification and new live Codex runs. The notes below describe the earlier 104-assertion version. Some shared diagnostic filenames have since been refreshed; their contents carry the actual current counts.

These files contain actual local Unity Editor results. Earlier offline reference repairs are labeled accordingly. The signed-in Codex CLI now has two successful live investigation/repair runs, preserved in timestamped folders and the sibling `codex-demo-backup` folder. No separate API key was used.

- `broken-fixture.txt`: original source rejected at 79/104; adjacent `.source.txt` is the actual evaluated source.
- `reject-all-mutation.txt`: reject-all mobility rejected at 54/104.
- `missing-ground-mutation.txt`: incomplete mobility repair rejected at 97/104.
- `reference-corrected.txt`: legitimate source passed at 104/104.
- `arena-smoke.txt`: 28/28 integration assertions against actual arena methods in Play mode.
- `input-checks.txt`: 13/13 checks through temporary Input System mouse/keyboard devices and the arena Update method, including left-click selection and repeated auto-attacks after releasing the mouse.
- `workbench-guards.txt`: 13/13 validation and source-preservation checks.
- `recovery-checks.txt`: 3/3 missing-key, timeout and stale-source checks. `missing-key.txt` is the real missing-credential workbench report.
- `replay-before.txt` transcribes the observed pre-fix arena banner; `replay-after.txt` is captured directly from the corrected replay.
- `before-replay.png` / `after-replay.png`: actual Game view captures.
- `workbench-narrow.png` / `workbench-wide.png`: reviewed workbench layouts.
- `final-console.json`: final Console ground truth, including compilation status.
- `missing-sprites-before.json`: actual missing native sprite references observed before the lifecycle repair.
- `visual-lifecycle.txt`: zero missing sprites and three loaded official portraits, including consecutive Play sessions without a script recompile or domain reload between them.
- `official-portraits.png`: rendered arena after the visual repair, with official static champion portrait tokens.
- `visual-fix-console.json`: Console ground truth after the visual/control follow-up.
- `visual-fix-no-new-errors.json`: no new errors after the last pre-fix MissingReferenceException (cursor 69449). The collector's cumulative counts retain historical pre-fix exceptions; current Console ground truth has zero errors.

Timestamped sibling directories preserve actual workbench runs. `20261114-195404-7764dc` records a continuation timeout found during development. `20261114-200806-e9f289` is the final explicit developer reset, rejected at 79/104. `20261114-200827-54bf5b` is the final reference repair: real source diff, compile/reload continuation and 104/104 independent verification, explicitly labeled NOT AI.

The final project contains the repaired source. To reproduce the failure, use the labeled developer reset in Dex > Agent Workbench, then enter Play and press F6. The current reset focuses only on the mobility regression (85/104), preserving the shield rule; the historical two-bug fixture above scored 79/104. The latest real Codex runs are `20261114-205722-d090f7` and `20261114-210129-fdf7c5`, both 104/104. The second run also records identical verifier hashes before and after. `codex-demo-backup` has actual before/after replay GIFs and a copy of the successful run. Remote usage-limit and multi-attempt repair behavior remain untested. See DEX_README.md for setup and limitations.
