# Project debugging run

Report: Ahri’s orb damages a stationary enemy repeatedly while passing through it. It should hit each enemy once going outward and once returning, regardless of simulation step size. Reproduce the issue and investigate what is causing it.

Mode: repair (repair authorized)

Result: Repair verified

Reproduced: True; source repaired: True; task verified: True.

Executed checks: 13; passed: 9; valid probes: 1; invalid/error probes: 0.

Elapsed: 109.6 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires one hit per enemy per direction, independent of step size. Original source sets orb damage to 100 + 0.5*AP: with AP 0 and MR 100, one outward magic hit deals 50 and one returning true-damage hit deals 100. These preserve existing per-hit balance, yielding 150 total damage. All setup occurs before casting. The trace reproduces excess damage: fine steps deal 450 outward and 1650 total; coarse steps deal 300 total. StepShots clears the hit set on collision without adding the victim, permitting repeated overlap hits. Retained damage and off-path controls directly witness the report; equality across step sizes alone would also accept equally broken results. These checks cover the supplied stationary-target scenario, not all enemies or step sizes.

## Before

Ahri orb hits once per direction across simulation step sizes: 9/13
PASS Fine cast launches one orb: observed 1 eq expected 1 @ fineCast
FAIL Fine outward hits once: observed 1350 eq expected 1750 @ fineOutward
FAIL Fine full flight hits once each direction: observed 150 eq expected 1650 @ fineComplete
PASS Fine off-path control unharmed: observed 1800 eq expected 1800 @ fineComplete
PASS Fine target stays stationary: observed 0 eq expected 0 @ fineComplete
PASS Fine orb completes return: observed 0 eq expected 0 @ fineComplete
PASS Coarse cast launches one orb: observed 1 eq expected 1 @ coarseCast
PASS Coarse outward hits once: observed 1750 eq expected 1750 @ coarseOutward
FAIL Coarse full flight hits once each direction: observed 1500 eq expected 1650 @ coarseComplete
PASS Coarse off-path control unharmed: observed 1800 eq expected 1800 @ coarseComplete
PASS Coarse target stays stationary: observed 0 eq expected 0 @ coarseComplete
PASS Coarse orb completes return: observed 0 eq expected 0 @ coarseComplete
FAIL Completed damage independent of step size: observed 1500 eq expected 150 @ coarseComplete

## After

Ahri orb hits once per direction across simulation step sizes: 13/13
PASS Fine cast launches one orb: observed 1 eq expected 1 @ fineCast
PASS Fine outward hits once: observed 1750 eq expected 1750 @ fineOutward
PASS Fine full flight hits once each direction: observed 1650 eq expected 1650 @ fineComplete
PASS Fine off-path control unharmed: observed 1800 eq expected 1800 @ fineComplete
PASS Fine target stays stationary: observed 0 eq expected 0 @ fineComplete
PASS Fine orb completes return: observed 0 eq expected 0 @ fineComplete
PASS Coarse cast launches one orb: observed 1 eq expected 1 @ coarseCast
PASS Coarse outward hits once: observed 1750 eq expected 1750 @ coarseOutward
PASS Coarse full flight hits once each direction: observed 1650 eq expected 1650 @ coarseComplete
PASS Coarse off-path control unharmed: observed 1800 eq expected 1800 @ coarseComplete
PASS Coarse target stays stationary: observed 0 eq expected 0 @ coarseComplete
PASS Coarse orb completes return: observed 0 eq expected 0 @ coarseComplete
PASS Completed damage independent of step size: observed 1650 eq expected 1650 @ coarseComplete

## Timeline

0.0s / Investigating
0.1s / Investigating
65.3s / Reproducing
65.8s / Reproduced
65.8s / Reviewing specification
87.1s / Repairing
98.2s / Compiling
104.2s / Verifying
109.6s / Repair verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
