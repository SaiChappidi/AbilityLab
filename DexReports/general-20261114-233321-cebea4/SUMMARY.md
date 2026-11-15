# Project debugging run

Report: Mel's Rebuttal barrier disappears on the next simulation tick even though its duration is still active. With no incoming damage, it should remain during its active duration, then disappear when the duration expires. Investigate expiration handling and preserve consistent combat behavior.

Result: Unresolved

Reproduced: False; source repaired: False; task verified: False.

Elapsed: 16.8 seconds (not measured labor saved).

Codex stopped with exit 1. No successful run assumed.


## Specification review



## Before



## After



## Timeline

0.0s / Investigating
0.2s / Investigating
16.8s / Unresolved


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
