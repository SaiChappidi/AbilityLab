# Project debugging run

Report: Mel's Rebuttal barrier disappears on the next simulation tick even though its duration is still active. With no incoming damage, it should remain during its active duration, then disappear when the duration expires. Investigate expiration handling and preserve consistent combat behavior.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 84.1 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires persistence without damage until expiration. Original CastMel grants a barrier for two seconds, independently of the 0.75-second nova. The trace reproduces premature clearing on the first tick. Relative barrier assertions preserve existing balance rather than inventing it. Black Shield is a supported expiration control, but this witness focuses on the reported barrier lifecycle and unchanged health; it does not certify all combat behavior.

## Before

Rebuttal barrier persists until expiration without damage: 8/11
PASS Rebuttal grants barrier: observed 180 gt expected 0 @ rebuttal_cast
FAIL Barrier survives first tick unchanged: observed 0 eq expected 180 @ first_tick
FAIL Barrier persists after nova ends: observed 0 eq expected 180 @ after_nova
FAIL Barrier persists just before expiration: observed 0 eq expected 180 @ before_expiration
PASS Pre-expiration checkpoint precedes deadline: observed 1.98 lt expected 2 @ before_expiration
PASS Barrier clears after expiration: observed 0 eq expected 0 @ after_expiration
PASS Mel health unchanged without damage: observed 1800 eq expected 1800 @ black_shield_expired
PASS Black Shield control granted: observed 210 gt expected 0 @ black_shield_cast
PASS Black Shield control persists beyond barrier duration: observed 210 eq expected 210 @ after_expiration
PASS Black Shield control expires: observed 0 eq expected 0 @ black_shield_expired
PASS Morgana health unchanged without damage: observed 1800 eq expected 1800 @ black_shield_expired

## After

Rebuttal barrier persists until expiration without damage: 11/11
PASS Rebuttal grants barrier: observed 180 gt expected 0 @ rebuttal_cast
PASS Barrier survives first tick unchanged: observed 180 eq expected 180 @ first_tick
PASS Barrier persists after nova ends: observed 180 eq expected 180 @ after_nova
PASS Barrier persists just before expiration: observed 180 eq expected 180 @ before_expiration
PASS Pre-expiration checkpoint precedes deadline: observed 1.98 lt expected 2 @ before_expiration
PASS Barrier clears after expiration: observed 0 eq expected 0 @ after_expiration
PASS Mel health unchanged without damage: observed 1800 eq expected 1800 @ black_shield_expired
PASS Black Shield control granted: observed 210 gt expected 0 @ black_shield_cast
PASS Black Shield control persists beyond barrier duration: observed 210 eq expected 210 @ after_expiration
PASS Black Shield control expires: observed 0 eq expected 0 @ black_shield_expired
PASS Morgana health unchanged without damage: observed 1800 eq expected 1800 @ black_shield_expired

## Timeline

0.0s / Investigating
0.2s / Investigating
44.9s / Reproducing
46.1s / Reproduced
46.2s / Reviewing specification
66.4s / Repairing
74.3s / Compiling
81.0s / Verifying
84.1s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
