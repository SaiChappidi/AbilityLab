# Dex project debugger

The **Project Debugger** tab accepts an ordinary gameplay bug report for this Unity project. The older, deliberately constrained encounter demonstration remains under **Guided Fixtures**. General mode has no encounter-id enum, report keyword routing, reference repair expression, or fixture-specific host handler.

## Architecture

The signed-in native Codex CLI produces structured requests and source-edit proposals. A trusted Unity Editor host supplies a small tool broker:

1. **Inspect:** literal source search and line-range reads from an isolated snapshot, plus active-scene components, package configuration, current Console context, compiler feedback and available regression descriptions.
2. **Reproduce:** compose JSON actions that drive actual arena methods and the same gameplay tick used by playable Update. Run the scenario before applying a source patch. Invalid tools, exceptions and timeouts do not count as reproduced assertion failures.
3. **Review and freeze:** a separate Codex context reviews whether the assertions follow from the report, initial conditions and immutable original source. It receives no candidate repair. Both the proposed reproduction and the separate witness are frozen and hashed before repair. A fresh before-witness run must also fail.
4. **Repair:** propose ordinary multi-line C# changes across gameplay files, including new C# files. The host stages exact, unique source replacements in the working copy, captures the actual diff, checks live-file hashes and applies the candidate.
5. **Compile and iterate:** Unity compilation/domain-reload continuation runs from saved state. Real compiler or test failures go back to the agent for up to three repair proposals. Compilation failure is never treated as a passing test.
6. **Verify:** the frozen reproduction and witness must pass in fresh owned Play sessions; the protected 224-assertion oracle, 38 actual arena checks and saved general scenarios must pass too. Protected-source and frozen-test hashes must remain unchanged.

Source discovery uses explicit host read/search requests because native CLI shell-tool availability varies. The model's native tools remain read-only. It receives no write-capable shell, Unity invocation, network operation or verifier-edit tool.

## Running it

Open **Dex > Agent Workbench > Project Debugger**, stop Play, and describe the observed and expected behavior. Press **Investigate → Reproduce → Repair → Verify**. The existing ChatGPT-signed-in Codex executable is used; no API key is introduced.

Useful reports identify an observable mismatch and any known steps, without specifying a solution. The host does not require choosing a champion pair or known scenario. An ambiguous request can end as **Needs developer review**; an unsuccessful investigation or bounded repair loop ends **Unresolved**. Only the recorded task and its regression gates can be **Verified**.

The dashboard separately displays reproduction, repair and verification status. Tabs show source investigation, frozen steps/seed, actual diffs, expected versus observed values, protected test outcomes, real activity and elapsed wall time.

**Replay frozen test** re-executes the saved actions against the current source. It is a test replay, not a claim that the current source still equals the original before/after run. **Roll back this run's source changes** restores that run's original source only if its live-file manifest still matches. This can reintroduce an intentionally injected evaluation bug; the button says so.

## Reusable test actions

Scenarios name initial player/opponent champions and a random seed. Actions support reset, spawn, place, initial numeric state, movement, attack/stop orders, casts, crowd control, damage, fixed-step time advancement and named observations. Assertions compare observed health, mana, cooldowns, position, control/shield state, projectile/zone/effect counts and other recorded values with literal expectations or earlier checkpoints.

The protected adapter calls real arena Cast, Hit, ResetWorld and TickSimulation methods. The old 1D Combat.ResolveCast simulator is not used as the new reproduction engine. Simulation time advances deterministically; Unity rendering/destruction still happens in ordinary Editor frames. Input polling and automatic bot/wave behavior are disabled during owned tests. Actual mouse/keyboard handling remains a separate integration concern.

Agent-written tests are declarative JSON, not arbitrary executable C# tests. This supports new action sequences without granting test scripts filesystem or Editor access. A detailed API/schema is saved in each working copy's TEST_API.md.

## Write boundary and source preservation

The model runs read-only. The host's patch writer accepts relative gameplay C# paths under Assets; Editor, test, diagnostics and verification paths, traversal, absolute paths and case-variant protected names are rejected. Patches cannot edit packages, settings, credentials, arbitrary assets or the frozen JSON assertions. Candidate code containing direct Editor, filesystem, network, process, reflection or native-interop access is rejected.

The working copy and baseline preserve the current source, including existing user modifications. Before application, the host compares the live gameplay inventory and content hashes. An external change stops the transaction rather than overwriting it. Failed multi-edit validation does not partially stage a proposal. Rollback also checks current hashes and refuses to overwrite external edits.

This is a constrained host-tool boundary, not a secure execution environment for deliberately malicious arbitrary C#. Candidate gameplay C# is eventually compiled/executed by the active Unity Editor. The working copy isolates proposed source edits; it is not a separately licensed/cloned Unity Editor, OS container or copy of Library. Restricted-API scanning and integrity hashes are additional defenses, not a proof of C# sandboxing.

## Recovery and evidence

Every run has a DexReports/general-* directory containing state checkpoints, source baseline/working copy, inspection results, proposed/frozen scenarios, separate specification review, before/after actual observations and screenshots, Codex JSONL events, ordinary source patches, compiler feedback, protected regression output and checker hashes.

Compilation continuation is saved on disk and resumed after a script reload. A reload/restart during another asynchronous phase is marked interrupted without claiming success. Completed proposals remain reviewable; rollback can restore the run's source after external-change checks. Cancellation stops the model and owned test session; failed applied repairs attempt rollback and recompilation. If recovery cannot safely restore source, the run explicitly needs developer review.

Automatic tests require this project's Disable Domain Reload setting and an idle Editor. User-owned Play sessions are not taken over. CLI requests have a 180-second timeout, there are three reproduction probes and up to seven inspection turns per probe, scenario execution budgets, a 120-second compile watchdog, and a bounded three-attempt repair loop. Raw failed attempts are retained.

Successfully checked witness scenarios are saved as future regressions. At most twenty saved scenarios are run automatically in the current prototype; this limit prevents an unbounded test loop and is reported as a limitation.

## What verification means

The specification reviewer uses a separate context with no candidate patch, but it is still a model and can share mistakes with the repair model. Frozen failures, independent host assertions, fresh execution and protected regressions provide evidence; they do not independently human-certify an arbitrary intended behavior. A clear report and source-supported expectations are essential. No claim covers all possible game states.

Scope remains this sandbox and its exposed operations: gameplay C# bugs reachable through the action API. Asset import issues, arbitrary visual layout judgments, animation/physics systems outside this arena, package installation, networking and general Unity-project repair are not established. Intentionally introduced evaluation fixtures live separately in AgentScripts/GeneralEvaluation.cs and are never a production route from a report to a fix.

Run durations are elapsed wall time, not measured developer labor saved. A controlled comparison with a human baseline is still needed for man-hour claims.
