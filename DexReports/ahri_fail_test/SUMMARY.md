# Project debugging run

Report: Ahri’s orb damages a stationary enemy repeatedly while passing through it. It should hit each enemy once going outward and once returning, regardless of simulation step size. Investigate and fix this.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 98.6 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires one hit per enemy on each leg, independent of timestep. Original source sets orb damage to 100 + 0.5*AP: with AP 0 and MR 40, one outward hit deals 100*100/140 = 71.42857 health; one return hit directly removes 100 health. These preserve existing per-hit balance. Speed 11 and outward range 8 support the selected outward and return checkpoints at both timesteps. No post-cast state is manufactured. The trace reproduces excess damage, including timestep-dependent outward damage. Retained cumulative checks prevent a passing return delta or death from concealing earlier excess damage. Off-path checks provide controls. This witness covers the supplied stationary-target cases, not all enemies or all timesteps.

## Before

Ahri orb hits once per enemy per leg across timesteps: 8/12
FAIL Fine outward single hit: observed 1014.286 eq expected 1728.571 @ fine_outward
FAIL Fine return single true-damage hit: observed 0 eq expected 914.2855 @ fine_return
PASS Fine no later damage: observed 0 eq expected 0 @ fine_finished
PASS Fine off-path control: observed 1800 eq expected 1800 @ fine_finished
PASS Fine orb completed: observed 0 eq expected 0 @ fine_finished
FAIL Coarse outward single hit: observed 1657.143 eq expected 1728.571 @ coarse_outward
PASS Coarse return single true-damage hit: observed 1557.143 eq expected 1557.143 @ coarse_return
FAIL Coarse no later damage: observed 1457.143 eq expected 1557.143 @ coarse_finished
PASS Coarse off-path control: observed 1800 eq expected 1800 @ coarse_finished
PASS Coarse orb completed: observed 0 eq expected 0 @ coarse_finished
PASS Target remains stationary: observed -2 eq expected -2 @ coarse_finished
PASS Fine target remains stationary: observed -2 eq expected -2 @ fine_finished

## After

Ahri orb hits once per enemy per leg across timesteps: 12/12
PASS Fine outward single hit: observed 1728.571 eq expected 1728.571 @ fine_outward
PASS Fine return single true-damage hit: observed 1628.571 eq expected 1628.571 @ fine_return
PASS Fine no later damage: observed 1628.571 eq expected 1628.571 @ fine_finished
PASS Fine off-path control: observed 1800 eq expected 1800 @ fine_finished
PASS Fine orb completed: observed 0 eq expected 0 @ fine_finished
PASS Coarse outward single hit: observed 1728.571 eq expected 1728.571 @ coarse_outward
PASS Coarse return single true-damage hit: observed 1628.571 eq expected 1628.571 @ coarse_return
PASS Coarse no later damage: observed 1628.571 eq expected 1628.571 @ coarse_finished
PASS Coarse off-path control: observed 1800 eq expected 1800 @ coarse_finished
PASS Coarse orb completed: observed 0 eq expected 0 @ coarse_finished
PASS Target remains stationary: observed -2 eq expected -2 @ coarse_finished
PASS Fine target remains stationary: observed -2 eq expected -2 @ fine_finished

## Timeline

0.0s / Investigating
0.2s / Investigating
53.9s / Reproducing
54.3s / Reproduced
54.3s / Reviewing specification
79.5s / Repairing
88.4s / Compiling
94.8s / Verifying
98.6s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
