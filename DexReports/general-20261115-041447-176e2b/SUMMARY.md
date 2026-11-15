# Project debugging run

Report: Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.

Mode: test (repair authorized)

Result: Repair verified

Reproduced: True; source repaired: True; task verified: True.

Executed checks: 10; passed: 9; valid probes: 1; invalid/error probes: 0.

Elapsed: 18.8 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires one listed mana charge for a successful actual arena cast and no additional mana spending or cooldown extension on a rejected recast. ChampionKit lists Morgana E at 70 mana and 14 seconds; DexArena.Cast calls BeginCast and grants a shield on success. The trace observes the shield and cooldown but mana falls from 900 to 760, reproducing the double charge. Retain the success control and report-derived resource assertions below. Unaffordable-cast, regeneration-rate and shield-refresh assertions are unnecessary to this frozen witness.

## Before

Arena cast mana charge and rejection controls: 9/10
PASS unaffordable cast preserves mana: observed 54 eq expected 54 @ unaffordable_q
PASS unaffordable cast preserves readiness: observed 0 eq expected 0 @ unaffordable_q
PASS unaffordable cast emits no projectile: observed 0 eq expected 0 @ unaffordable_q
FAIL successful E charges listed cost once: observed 760 eq expected 830 @ successful_e
PASS successful E grants shield: observed 1 eq expected 1 @ successful_e
PASS successful E starts listed cooldown: observed 14 eq expected 14 @ successful_e
PASS normal mana regeneration continues: observed 762.9993 eq expected 763 @ before_recast
PASS cooldown recast spends no mana: observed 762.9993 eq expected 762.9993 @ cooldown_recast
PASS cooldown recast preserves deadline: observed 14 eq expected 14 @ cooldown_recast
PASS cooldown recast does not refresh shield duration: observed 5 eq expected 5 @ cooldown_recast

## After

Arena cast mana charge and rejection controls: 10/10
PASS unaffordable cast preserves mana: observed 54 eq expected 54 @ unaffordable_q
PASS unaffordable cast preserves readiness: observed 0 eq expected 0 @ unaffordable_q
PASS unaffordable cast emits no projectile: observed 0 eq expected 0 @ unaffordable_q
PASS successful E charges listed cost once: observed 830 eq expected 830 @ successful_e
PASS successful E grants shield: observed 1 eq expected 1 @ successful_e
PASS successful E starts listed cooldown: observed 14 eq expected 14 @ successful_e
PASS normal mana regeneration continues: observed 832.9993 eq expected 833 @ before_recast
PASS cooldown recast spends no mana: observed 832.9993 eq expected 832.9993 @ cooldown_recast
PASS cooldown recast preserves deadline: observed 14 eq expected 14 @ cooldown_recast
PASS cooldown recast does not refresh shield duration: observed 5 eq expected 5 @ cooldown_recast

## Timeline

0.0s / Investigating
0.2s / Investigating
53.9s / Reproducing
55.2s / Reproduced
55.2s / Reviewing specification
67.7s / Bug reproduced — no repair attempted
0.1s / Repairing
6.7s / Compiling
13.8s / Verifying
18.8s / Repair verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
