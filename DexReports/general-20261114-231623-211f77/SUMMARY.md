# Project debugging run

Report: Ahri's Orb of Deception damages the same stationary enemy repeatedly while passing through it. It should damage that enemy once going out and once coming back, regardless of simulation step size. Reproduce this with the actual projectile and fix the hit accounting.

Result: Needs developer review

Reproduced: False; source repaired: False; task verified: False.

Elapsed: 17.7 seconds (not measured labor saved).

Agent could not establish unambiguous expected behavior: Workspace reads were rejected by the tool policy, so I could not inspect PROJECT_CONTEXT.txt, TEST_API.md, or gameplay source. The report suggests missing per-target hit accounting for each projectile travel direction, but this remains an unverified hypothesis. No host scenario was executed, no failure is confirmed, and no repair is proposed. A valid reusable scenario requires the documented host operations and source-derived projectile timing, damage, and observable fields.

## Specification review



## Before



## After



## Timeline

0.0s / Investigating
0.2s / Investigating
17.7s / Needs developer review


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
