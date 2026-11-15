# Project debugging run

Report: Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.

Mode: repair (repair authorized)

Result: No failure reproduced in tested cases

Reproduced: False; source repaired: False; task verified: False.

Executed checks: 21; passed: 21; valid probes: 2; invalid/error probes: 1.

Elapsed: 89.5 seconds (not measured labor saved).

No failing reproduction established in three probes. 21 checks executed, 21 passed across 2 valid probes; 1 invalid or execution-error probes. Coverage is limited to these tested cases. No source edit attempted.

## Specification review



## Before

Actual arena casts charge once and cooldown recasts preserve resources: 12/12
PASS Q charges listed cost once: observed 845 eq expected 845 @ q_success
PASS Q starts listed cooldown: observed 7 eq expected 7 @ q_success
PASS Q creates projectile: observed 1 eq expected 1 @ q_success
PASS Cooldown rejection preserves mana: observed 845 eq expected 845 @ q_rejected
PASS Cooldown rejection preserves ready time: observed 7 eq expected 7 @ q_rejected
PASS Rejected Q adds no projectile: observed 1 eq expected 1 @ q_rejected
PASS Exact-cost E charges once: observed 0 eq expected 0 @ e_success
PASS Exact-cost E grants shield: observed 210 gt expected 0 @ e_success
PASS E starts listed cooldown: observed 14 eq expected 14 @ e_success
PASS Rejected E preserves mana: observed 0 eq expected 0 @ e_rejected
PASS Rejected E preserves ready time: observed 14 eq expected 14 @ e_rejected
PASS Rejected E preserves shield: observed 210 eq expected 210 @ e_rejected

## After



## Timeline

0.0s / Investigating
0.2s / Investigating
37.3s / Reproducing
37.6s / Investigating
64.1s / Investigating
89.1s / Reproducing
89.5s / No failure reproduced in tested cases


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
