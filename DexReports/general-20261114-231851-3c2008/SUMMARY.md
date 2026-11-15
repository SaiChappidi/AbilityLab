# Project debugging run

Report: Ahri's Orb of Deception damages the same stationary enemy repeatedly while passing through it. It should damage that enemy once going out and once coming back, regardless of simulation step size. Reproduce this with the actual projectile and fix the hit accounting.

Result: Needs developer review

Reproduced: True; source repaired: False; task verified: False.

Elapsed: 81.6 seconds (not measured labor saved).

An independent model context did not establish a sufficient task-specific expectation.

## Specification review

The report clearly requires one hit per target per leg, independent of step size. The scenario uses actual projectiles and does not manufacture post-cast state. However, the supplied evidence does not establish the proposed 50 outbound and 100 return damage per hit or confirm the leg timing. Those numeric assertions need source evidence or independent single-hit calibration before freezing. The trace establishes differing damage totals across step sizes. The retained checks below are necessary witnesses, but cannot alone prove exactly one hit per leg.

## Before

Ahri orb hits each stationary target once per leg across step sizes: 10/15
PASS Fine cast creates actual projectile: observed 1 eq expected 1 @ fineCast
PASS Fine pre-impact control: observed 1800 eq expected 1800 @ fineBeforeImpact
FAIL Fine outbound hits once: observed 1250 eq expected 1750 @ fineOutbound
FAIL Fine return hits once: observed 150 eq expected 1150 @ fineComplete
PASS Fine off-path control: observed 1800 eq expected 1800 @ fineComplete
PASS Fine projectile finishes: observed 0 eq expected 0 @ fineComplete
PASS Coarse cast creates actual projectile: observed 1 eq expected 1 @ coarseCast
PASS Coarse pre-impact control: observed 1800 eq expected 1800 @ coarseBeforeImpact
FAIL Coarse outbound hits once: observed 1700 eq expected 1750 @ coarseOutbound
FAIL Coarse return hits once: observed 1500 eq expected 1600 @ coarseComplete
PASS Coarse off-path control: observed 1800 eq expected 1800 @ coarseComplete
PASS Coarse projectile finishes: observed 0 eq expected 0 @ coarseComplete
FAIL Damage independent of step size: observed 1500 eq expected 150 @ coarseComplete
PASS Fine target stays stationary: observed 3 eq expected 3 @ fineComplete
PASS Coarse target stays stationary: observed 3 eq expected 3 @ coarseComplete

## After



## Timeline

0.0s / Investigating
0.2s / Investigating
62.8s / Reproducing
64.0s / Reproduced
64.0s / Reviewing specification
81.6s / Needs developer review


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
