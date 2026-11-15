# Project debugging run

Report: Ahri's Orb of Deception damages the same stationary enemy repeatedly while passing through it. It should damage that enemy once going out and once coming back, regardless of simulation step size. Reproduce this with the actual projectile and fix the hit accounting.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 118.1 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires one hit per enemy per travel leg, independent of step size. Original Cast creates an actual orb with 100 + 0.5*AP damage; AP is zero. Combat.Damage reduces the outward hit to 50 at 100 MR, while StepShots applies 100 directly on return. These are existing per-hit balances, not new tuning. Speed 11 and range 8 support the selected outbound and completion checkpoints. All state setup precedes casting. The trace reproduces excess damage at both step sizes. The witness checks exact per-leg damage and total damage, so equal but incorrect damage or a missing return hit cannot pass. Passing these checks establishes only this scenario's behavior.

## Before

Ahri orb hits each stationary target once per travel leg across step sizes: 12/17
PASS Fine cast creates actual projectile: observed 1 eq expected 1 @ fine_cast
FAIL Fine outward hits once: observed 700 eq expected 1750 @ fine_outbound
PASS Fine gap has no extra hit: observed 700 eq expected 700 @ fine_gap
FAIL Fine return hits once for true damage: observed 0 eq expected 600 @ fine_returned
PASS Fine projectile completes: observed 0 eq expected 0 @ fine_returned
PASS Fine damage stops: observed 0 eq expected 0 @ fine_finished
PASS Fine off-path control unharmed: observed 1800 eq expected 1800 @ fine_finished
PASS Fine target stays stationary: observed 4 eq expected 4 @ fine_finished
PASS Coarse cast creates actual projectile: observed 1 eq expected 1 @ coarse_cast
FAIL Coarse outward hits once: observed 1700 eq expected 1750 @ coarse_outbound
PASS Coarse gap has no extra hit: observed 1700 eq expected 1700 @ coarse_gap
FAIL Coarse return hits once for true damage: observed 1500 eq expected 1600 @ coarse_returned
PASS Coarse projectile completes: observed 0 eq expected 0 @ coarse_returned
PASS Coarse damage stops: observed 1500 eq expected 1500 @ coarse_finished
PASS Coarse off-path control unharmed: observed 1800 eq expected 1800 @ coarse_finished
PASS Coarse target stays stationary: observed 4 eq expected 4 @ coarse_finished
FAIL Damage independent of tested step size: observed 1500 eq expected 0 @ coarse_finished

## After

Ahri orb hits each stationary target once per travel leg across step sizes: 17/17
PASS Fine cast creates actual projectile: observed 1 eq expected 1 @ fine_cast
PASS Fine outward hits once: observed 1750 eq expected 1750 @ fine_outbound
PASS Fine gap has no extra hit: observed 1750 eq expected 1750 @ fine_gap
PASS Fine return hits once for true damage: observed 1650 eq expected 1650 @ fine_returned
PASS Fine projectile completes: observed 0 eq expected 0 @ fine_returned
PASS Fine damage stops: observed 1650 eq expected 1650 @ fine_finished
PASS Fine off-path control unharmed: observed 1800 eq expected 1800 @ fine_finished
PASS Fine target stays stationary: observed 4 eq expected 4 @ fine_finished
PASS Coarse cast creates actual projectile: observed 1 eq expected 1 @ coarse_cast
PASS Coarse outward hits once: observed 1750 eq expected 1750 @ coarse_outbound
PASS Coarse gap has no extra hit: observed 1750 eq expected 1750 @ coarse_gap
PASS Coarse return hits once for true damage: observed 1650 eq expected 1650 @ coarse_returned
PASS Coarse projectile completes: observed 0 eq expected 0 @ coarse_returned
PASS Coarse damage stops: observed 1650 eq expected 1650 @ coarse_finished
PASS Coarse off-path control unharmed: observed 1800 eq expected 1800 @ coarse_finished
PASS Coarse target stays stationary: observed 4 eq expected 4 @ coarse_finished
PASS Damage independent of tested step size: observed 1650 eq expected 1650 @ coarse_finished

## Timeline

0.0s / Investigating
0.2s / Investigating
67.5s / Reproducing
68.6s / Reproduced
68.6s / Reviewing specification
101.1s / Repairing
108.2s / Compiling
115.4s / Verifying
118.1s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
