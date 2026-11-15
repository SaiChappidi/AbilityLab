# Project debugging run

Report: Ahri’s orb damages a stationary enemy repeatedly while passing through it. It should hit each enemy once going outward and once returning, regardless of simulation step size. Reproduce the issue and investigate what is causing it.

Mode: test (source changes not authorized)

Result: Bug reproduced — no repair attempted

Reproduced: True; source repaired: False; task verified: False.

Executed checks: 10; passed: 4; valid probes: 1; invalid/error probes: 0.

Elapsed: 84.5 seconds (not measured labor saved).

The frozen reproduction and separate witness both failed before any source edit. Gameplay and protected source hashes remained unchanged. Choose Attempt repair to continue from this evidence.

## Specification review

The report explicitly requires one hit per enemy on each leg, independent of step size. Original Cast supplies 100 + 0.5*AP damage; zero AP and MR therefore justify 100 damage per leg without changing balance. Speed 11 and range 8 place the 0.5-second checkpoint after outward contact and before return contact; 1.6 seconds covers both legs. Initial conditions are established before casting. The trace reproduces excess damage and step dependence. Source clears hit history on every collision instead of recording the victim, allowing repeated overlap hits. These checks witness the reported case, not all enemies or all step sizes.

## Before

Ahri orb hits once per enemy per leg across simulation step sizes: 4/10
FAIL normal outward single hit: observed 1100 eq expected 1700 @ normalOutward
FAIL normal return single hit: observed 500 eq expected 1000 @ normalComplete
PASS off-path control unharmed: observed 1800 eq expected 1800 @ normalComplete
PASS normal target stationary: observed 3 eq expected 3 @ normalComplete
PASS normal flight finished: observed 0 eq expected 0 @ normalComplete
FAIL fine outward single hit: observed 0 eq expected 1700 @ fineOutward
FAIL fine return single hit: observed 0 eq expected -100 @ fineComplete
FAIL outward damage independent of step size: observed 0 eq expected 1100 @ fineOutward
FAIL total damage independent of step size: observed 0 eq expected 500 @ fineComplete
PASS fine flight finished: observed 0 eq expected 0 @ fineComplete

## After



## Timeline

0.0s / Investigating
2.6s / Investigating
64.2s / Reproducing
64.6s / Reproduced
64.7s / Reviewing specification
84.5s / Bug reproduced — no repair attempted


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
