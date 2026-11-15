# Project debugging run

Report: Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.

Mode: test (source changes not authorized)

Result: No failure reproduced in tested cases

Reproduced: False; source repaired: False; task verified: False.

Executed checks: 32; passed: 32; valid probes: 3; invalid/error probes: 0.

Elapsed: 119.5 seconds (not measured labor saved).

No failing reproduction established in three probes. 32 checks executed, 32 passed across 3 valid probes; 0 invalid or execution-error probes. Coverage is limited to these tested cases. No source edit attempted.

## Specification review



## Before

Actual arena single mana charge and delayed cooldown rejection: 8/8
PASS Successful E charges listed cost once: observed 830 eq expected 830 @ successful_e
PASS Successful E starts listed cooldown: observed 14 eq expected 14 @ successful_e
PASS Successful E grants shield: observed 1 eq expected 1 @ successful_e
PASS Ordinary regeneration is observed: observed 832.9999 eq expected 833 @ before_rejection
PASS Rejected recast preserves mana: observed 832.9999 eq expected 832.9999 @ rejected_e
PASS Rejected recast preserves cooldown deadline: observed 14 eq expected 14 @ rejected_e
PASS Rejected recast does not refresh shield: observed 5 eq expected 5 @ rejected_e
PASS Uninvolved opponent keeps mana: observed 900 eq expected 900 @ rejected_e

## After



## Timeline

0.0s / Investigating
0.2s / Investigating
47.7s / Reproducing
48.1s / Investigating
78.8s / Reproducing
79.2s / Investigating
119.1s / Reproducing
119.5s / No failure reproduced in tested cases


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
