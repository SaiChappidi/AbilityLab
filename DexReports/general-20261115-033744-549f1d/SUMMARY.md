# Project debugging run

Report: Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 85.9 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires one listed mana charge per successful arena cast and preservation of mana and cooldown on rejected recasts. ChampionKit lists Morgana E at 70 mana and 14 seconds, and Ahri Q at 55 mana. DexArena.Cast invokes BeginCast and creates the shield or projectile; BeginCast currently subtracts twice the listed cost. The trace reproduces both double charges while showing both cooldown recasts preserve resources. The proposed checks follow from the report and source, use actual casts, and account for regeneration by comparing immediately adjacent checkpoints. Shield and projectile assertions provide successful-cast controls. These checks cover this scenario only.

## Before

Actual arena casts charge listed mana once and cooldown rejections preserve state: 7/9
FAIL Shield cast spends listed 70 mana: observed 760 eq expected 830 @ shield_cast
PASS Successful E creates shield: observed 1 eq expected 1 @ shield_cast
PASS Successful E starts listed cooldown: observed 14 eq expected 14 @ shield_cast
PASS Immediate rejection preserves mana: observed 760 eq expected 760 @ immediate_recast
PASS Immediate rejection preserves deadline: observed 14 eq expected 14 @ immediate_recast
PASS Delayed rejection preserves mana: observed 762.9993 eq expected 762.9993 @ delayed_recast
PASS Delayed rejection preserves original deadline: observed 14 eq expected 14 @ delayed_recast
FAIL Independent Q spends listed 55 mana: observed 790 eq expected 845 @ independent_q
PASS Independent Q creates projectile: observed 1 eq expected 1 @ independent_q

## After

Actual arena casts charge listed mana once and cooldown rejections preserve state: 9/9
PASS Shield cast spends listed 70 mana: observed 830 eq expected 830 @ shield_cast
PASS Successful E creates shield: observed 1 eq expected 1 @ shield_cast
PASS Successful E starts listed cooldown: observed 14 eq expected 14 @ shield_cast
PASS Immediate rejection preserves mana: observed 830 eq expected 830 @ immediate_recast
PASS Immediate rejection preserves deadline: observed 14 eq expected 14 @ immediate_recast
PASS Delayed rejection preserves mana: observed 832.9993 eq expected 832.9993 @ delayed_recast
PASS Delayed rejection preserves original deadline: observed 14 eq expected 14 @ delayed_recast
PASS Independent Q spends listed 55 mana: observed 845 eq expected 845 @ independent_q
PASS Independent Q creates projectile: observed 1 eq expected 1 @ independent_q

## Timeline

0.0s / Investigating
0.2s / Investigating
44.4s / Reproducing
45.4s / Reproduced
45.4s / Reviewing specification
68.6s / Repairing
75.1s / Compiling
81.5s / Verifying
85.9s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
