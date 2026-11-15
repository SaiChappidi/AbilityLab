# Dex — ability regression workbench

Dex helps an engineer turn an ability-interaction bug report into a checked source repair. Its product is the development workflow: **report → investigate → reproduce → repair → compile → independently verify → replay**. The playable Unity chamber provides observable integration evidence.

The DevHacks kickoff deck's Prompt 02 asks for an agentic workflow and a small project it can modify, showing a request becoming a verified change. Its judging guidance asks for a concrete bottleneck and evidence. The mentor deck emphasizes protected tests, deterministic replay and measuring efficiency rather than assuming it. Dex focuses on the repetitive setup, regression exploration and evidence collection around interaction bugs. It does not claim to replace a general game engineer.

## Start

1. Open this existing project in **Unity 6000.6.9f1**, allow import/compilation, and open `Assets/Scenes/SampleScene.unity`.
2. Open **Dex → Agent Workbench**. The installed native Codex CLI is discovered automatically. Connection details are under **Connection & developer tools**.
3. Choose an encounter. **DEMO: INJECT + RUN** explicitly introduces its known regression, compiles the fixture, then starts the real agent automatically. **RUN AUTOMATED REPAIR** uses the current source and your engineering request without injecting a bug.
4. Leave automatic evidence enabled. The workbench selects and runs reproductions, explores a generated scenario corpus, reduces a failing sequence, captures the broken arena, requests the repair, edits real C#, compiles/reloads, executes the protected oracle, and captures the corrected arena.
5. A live run reaches **Passed** only after the compiled oracle and the actual after-repair replay pass. Inspect Overview, Source diff, Verification and Activity, or open the run evidence folder.

The project is delivered repaired. A run against already-correct rules reports that no failure was reproduced and avoids requesting an unnecessary repair. The clearly labeled offline reference button remains available for deterministic developer recovery; it is never represented as an AI run.

## Four supported encounters

- **Binding / Arcane Shift:** Morgana's root must block a new Ezreal blink and preserve resources; expiry restores mobility.
- **Solar Snare / Spirit Rush:** Mel's root must block a new Ahri dash. This exercises the same mobility rule through another kit.
- **Rebuttal / hostile missile:** Mel's active nova destroys an incoming hostile missile, prevents its root and retaliates. A missile after expiry must get through.
- **Black Shield / Charm:** an active magic shield prevents Charm; after expiry it must not grant immunity.

The model can choose these encounters and the related normal, stun and expiry scenarios from a natural-language report. It cannot invent arbitrary executable tests or silently claim support for a new bug family. A custom report still needs to fit the supported rule families.

## Agent autonomy and trust boundary

The workbench uses the installed **Codex CLI**, with saved **ChatGPT sign-in**. It does not need an OpenAI API key, copy tokens or automate login. Run `codex login status` and, if needed, `codex login` in your terminal. Plan limits and network availability still apply. The model field can stay blank to use the CLI default. References: [authentication](https://learn.chatgpt.com/docs/auth), [noninteractive execution](https://learn.chatgpt.com/docs/non-interactive-mode).

Codex runs as a native process with direct argv quoting, no command shell, structured JSON output, a schema, saved JSONL progress, an ephemeral session, ignored user config/rules, **read-only tools**, and approvals set to **never**. API-key environment variables are removed and the preflight requires ChatGPT authentication. There is no paid API fallback and no user-configured MCP integration is loaded.

Its small read-only snapshot contains `Combat.cs`, `CombatRules.cs` and `ChampionKit.cs`. The protected oracle and arena checker are not copied into that snapshot. The model selects scenarios, receives actual visible feedback, and returns three boolean expressions plus an explanation. The trusted host is the only source writer. It validates a complete allowlisted grammar and can replace only the expression bodies of:

- `CanMove(stunned, rooted, charmed, grounded)`
- `BlocksControl(magicShield, physicalDamage)`
- `DestroysProjectile(novaActive, enemyProjectile, insideNova)`

The host preserves surrounding source, rejects stale snapshots, saves the real diff and backups, and tries atomic file replacement. If Windows/Mono rejects replacement, it acquires an exclusive file handle, rechecks the original under that lock, and writes the validated proposal; durable before/after copies support crash recovery. SHA-256 of the protected oracle and replay checker is recorded before and after the run; a changed hash fails the gate. Compilation/domain-reload continuation is persisted in Unity SessionState. Editor updates drive continuation, including an unfocused Editor, rather than relying solely on GUI delay callbacks.

Codex requests have a 180-second timeout, bounded output and cancellation. At most three compiled repair attempts are allowed when verification rejects a proposal. Auth, quota, malformed-output and process failures stop visibly; they do not switch billing methods. Compilation errors, a 120-second reload timeout and a ten-minute overall limit release the run without claiming success. Automatic Play sessions are owned and cleaned up by the checker, including interrupted script reloads. Existing user Play sessions are not used as automated test sessions.

This is constrained rule synthesis with specified intended semantics. It is not a general coding agent discovering the complete rules of League, or a sandbox against an adversarial human who can independently edit the host. The simplified simulator and arena share rule/resource gates; geometry and presentation still need their own integration checks.

## Automated investigation and evidence

The host generates **43 finite timing/matchup scenarios**, covering two root casters, two mobility kits and nova timing against three missile owners. It deletes actions from an observed failing sequence while preserving the failure. In the recorded Mel run, nine generated cases failed and a six-action sequence reduced to **nova → cast**. This is deterministic corpus exploration and local action deletion, not an unrestricted search over game state.

The independent oracle has **224 assertions**: control flag combinations; shield behavior; root/stun/charm/ground timing at just before, exactly at and after expiry for Ezreal and Ahri; mana/cooldown preservation; all nova boolean combinations; nova timing boundaries; Mel barrier, passive, mark, ultimate and retaliation regressions; and normal cooldown/death/slot guards. Expected values are held outside the agent-facing tools.

The actual arena replay is a separate gate. Core-only success does not count as a completed automatic run. Every live automatic run saves:

- The original request, selected tests and actual feedback.
- Generated failures and reduced reproduction steps.
- Codex invocation metadata, schema, model response, stdout event stream and stderr.
- Before/after source snapshots and an applied source diff.
- Compiler continuation, assertion-level results and protected-checker hashes.
- Before/after replay JSON, screenshots and captured PNG frame sequences.
- A stage timeline, elapsed wall time and a concise `SUMMARY.md`.

The PNG recordings can be packaged without new dependencies using `AgentScripts/EncodeAutomation.ps1 -RunDirectory <run-folder>`, which verifies the resulting GIF frame counts. Introduce saved evidence as a recording if a live network/quota problem prevents a fresh run.

## Measuring effort honestly

The automation handles scenario selection/execution, timing sweeps, reproduction reduction, source application, compilation/reload, protected verification, scene setup, replay and evidence packaging. The visible fixtures are intentionally small; a prewritten reference repair is a simpler and faster way to apply an already-known fix. The agent's demonstrated value is the complete request/feedback/checking loop, not proof that an LLM inserts a known expression faster.

Recorded wall time includes actual model/tool/compile/replay time. It is **not measured labor saved**. Under developer tools, an opt-in human baseline timer records a real manual interval, source snapshots and the final core verdict. It is never populated by automated reference repairs. It excludes automatic arena replay and does not calculate a misleading speedup percentage. A controlled comparison by a real engineer is still required before making man-hour claims.

## Arena controls

- Left-click an enemy: pursue and repeatedly auto-attack after releasing the mouse. The pink ring marks the target.
- Right-click ground: cancel the attack order and move. Right-click an enemy also attacks.
- Aim with the cursor and press **Q/W/E/R**, or click an ability icon using the last arena aim point.
- **A:** attack nearest. **S:** stop. **Esc:** matchup and practice menu. **F6:** reset/replay Binding → Arcane Shift.
- The menu selects any of four champions on either side; controls bots, waves, regeneration, sound and illustrative item presets; refills health/mana; resets cooldowns; and launches any supported replay.
- Champion death respawns after seven simulated seconds with fresh resources/control/cooldowns. Turrets stay destroyed; later waves replace minions.

The chamber has original geometric art, circular portrait tokens, colored team outlines, shield/control auras, aim/target feedback, ability icons, health/mana, passive/mark state, a minimap, combat trace and synthesized sound cues. It remains an intentionally small verification arena. Use a reasonably large Game view; the workbench scrolls and reflows at narrow widths.

## Mel: current-kit interpretation

Mel follows the **26.22 kit identity**, including the reworked W that destroys missiles and retaliates rather than reflecting their original effects. The champion overview and patch notes use slightly different Q names; this demo uses the patch-note label **Radiant Volley**. Sources: [official champion overview](https://www.leagueoflegends.com/en-us/champions/mel/) and [official 26.22 notes](https://www.leagueoflegends.com/en-gb/news/game-updates/league-of-legends-patch-26-22-notes/).

- Passive: casts bank bonus attack bolts, capped at nine. Damage applies Overwhelm; low-health marked targets can be executed.
- Q: six aimed missiles burst at a location. A champion hit refunds a little cooldown.
- W: a growing nova destroys hostile missiles and nearby enemy shields, grants a temporary barrier and speed, then retaliates at missile owners. Retaliations are combined and capped per owner in this demo.
- E: a travelling orb roots central targets and leaves a slowing damage field. Its damage pulses on a fixed cadence so passive stacking does not depend on rendered frame rate.
- R: detonate and consume active Overwhelm marks across the arena.

All numeric tuning, geometry, item stats and several ordering details are simplified. The barrier absorbs damage but does not independently grant Black Shield's control immunity. No live-patch parity, complete ally interaction, runes, tenacity, cast buffering, multiplayer or shop simulation is claimed.

## Files and verification

- `Assets/Dex/Runtime`: combat state, three editable rules, kit definitions, Mel arena behavior, replay encounters, HUD, procedural art, feedback and diagnostics.
- `Assets/Dex/Editor`: the dashboard, CLI runner, grammar, visible scenario exploration, protected oracle, automatic arena checker and opt-in manual timer.
- `AgentScripts/QualityPass.cs`: developer probes and explicit mutation helpers. These are not tools available to the repair agent.
- `DexReports/evidence/v2`: the expanded oracle, mutation rejection, scene replays, and visual QA evidence. Earlier evidence is retained as historical scope.
- `DexReports/<timestamp>`: actual independent/developer/live runs. `origin` identifies them; an offline action is never labeled Codex.
- `DexReports/restorepoints/pre-mel-polish.zip`: source checkpoint before this pass. Existing unrelated user changes were retained.

Current checks include **224/224 core**, **38/38 actual arena assertions**, **13/13 Input System checks**, and **4/4 complete encounter replays**. The always-block nova mutant was rejected at **209/224**; the never-destroy fixture at **217/224**. Development failures were fixed rather than hidden by weakening assertions; a prior frame-dependent Solar Snare failure is preserved under `evidence/v2/pre-fix`.

For CLI verification, use `unity status --project-path .`, `unity command script recompile_status`, and `unity command script run --file AgentScripts/QualityPass.cs --entry QualityPass.Core --caller plugin --skill unity-pipeline`. Enter Play for `Evidence.Smoke` and `InputChecks.Run`; `QualityPass.AllReplays` requires an idle Editor and manages its own Play sessions. Wait for compilation/reload completion before starting diagnostics. Restore auto-tick/background overrides after headless testing.

The direct API-only route was replaced with signed-in Codex. Remote quota exhaustion, interrupted authentication and a model needing multiple correction attempts have not been demonstrated. Automatic scene evidence currently requires this project's **Disable Domain Reload** Play setting; manual replay remains available otherwise. SessionState does not survive quitting Unity; inspect saved reports/source before restarting an interrupted run.

Official portrait and ability artwork © Riot Games; see `THIRD_PARTY_ASSETS.md`. This is an unofficial fan prototype and is not endorsed by Riot Games. No Riot source code, extracted models, game client connection or game automation is used. No publication, purchases or unrelated packages were needed.
