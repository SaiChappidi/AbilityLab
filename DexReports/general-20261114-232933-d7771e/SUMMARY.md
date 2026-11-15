# Project debugging run

Report: Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 86.8 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires one listed mana charge per successful arena cast and unchanged mana/readiness on cooldown rejection. ChampionKit lists Morgana E at 70 mana and Q at 55; the HUD displays those values. Actual arena Cast calls BeginCast, whose original implementation deducts twice that cost. The trace reproduces both excessive charges while confirming successful shield/projectile effects. The cooldown recast already preserves resources. These checks retain source-supported success controls without prescribing new balance or manufacturing state.

## Before

Actual arena casts charge listed mana once and cooldown rejection preserves resources: 8/10
FAIL Shield cast spends listed cost once: observed 760 eq expected 830 @ shield_cast
PASS Successful E grants shield: observed 210 eq expected 210 @ shield_cast
PASS Successful E starts listed cooldown: observed 14 eq expected 14 @ shield_cast
PASS Ordinary mana regeneration control: observed 762.9993 eq expected 763 @ before_recast
PASS Cooldown recast spends no mana: observed 762.9993 eq expected 762.9993 @ cooldown_recast
PASS Cooldown recast preserves ready timestamp: observed 14 eq expected 14 @ cooldown_recast
PASS Cooldown elapsed normally: observed 13.5 eq expected 13.5 @ cooldown_recast
FAIL Independent Q spends listed cost once: observed 790 eq expected 845 @ binding_cast
PASS Successful Q creates projectile: observed 1 eq expected 1 @ binding_cast
PASS Successful Q starts listed cooldown: observed 7 eq expected 7 @ binding_cast

## After

Actual arena casts charge listed mana once and cooldown rejection preserves resources: 10/10
PASS Shield cast spends listed cost once: observed 830 eq expected 830 @ shield_cast
PASS Successful E grants shield: observed 210 eq expected 210 @ shield_cast
PASS Successful E starts listed cooldown: observed 14 eq expected 14 @ shield_cast
PASS Ordinary mana regeneration control: observed 832.9993 eq expected 833 @ before_recast
PASS Cooldown recast spends no mana: observed 832.9993 eq expected 832.9993 @ cooldown_recast
PASS Cooldown recast preserves ready timestamp: observed 14 eq expected 14 @ cooldown_recast
PASS Cooldown elapsed normally: observed 13.5 eq expected 13.5 @ cooldown_recast
PASS Independent Q spends listed cost once: observed 845 eq expected 845 @ binding_cast
PASS Successful Q creates projectile: observed 1 eq expected 1 @ binding_cast
PASS Successful Q starts listed cooldown: observed 7 eq expected 7 @ binding_cast

## Timeline

0.0s / Investigating
0.2s / Investigating
47.7s / Reproducing
48.8s / Reproduced
48.8s / Reviewing specification
70.2s / Repairing
77.0s / Compiling
83.9s / Verifying
86.8s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
