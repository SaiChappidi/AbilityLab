# Project debugging run

Report: Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.

Mode: test (repair authorized)

Result: Needs developer review

Reproduced: True; source repaired: True; task verified: True.

Executed checks: 9; passed: 8; valid probes: 1; invalid/error probes: 0.

Elapsed: 18.0 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.
Source or frozen evidence changed during investigation: Saved regression corpus changed during this run.

## Specification review

The report explicitly requires one listed mana charge per successful cast and no additional mana charge or cooldown extension on a rejected recast. ChampionKit lists Morgana E at 70 mana and 14 seconds; DexArena.Cast calls BeginCast, whose original deduction is twice the listed cost. The trace reproduces a 140-mana deduction while confirming a successful shield cast. Retain success and active-cooldown controls alongside the report-derived resource assertions. These checks cover this cast and recast, not every ability.

## Before

Actual arena cast charges listed mana once and cooldown rejection preserves resources: 8/9
FAIL successful cast spends listed cost once: observed 760 eq expected 830 @ successful_cast
PASS successful cast grants shield: observed 210 gt expected 0 @ successful_cast
PASS successful cast starts listed cooldown: observed 14 eq expected 14 @ successful_cast
PASS ordinary mana regeneration control: observed 762.9993 eq expected 763 @ before_recast
PASS recast occurs during cooldown: observed 13.5 gt expected 0 @ before_recast
PASS rejected recast preserves mana: observed 762.9993 eq expected 762.9993 @ cooldown_recast
PASS rejected recast preserves cooldown deadline: observed 14 eq expected 14 @ cooldown_recast
PASS rejected recast preserves shield lifetime: observed 5 eq expected 5 @ cooldown_recast
PASS idle opponent mana control: observed 900 eq expected 900 @ cooldown_recast

## After

Actual arena cast charges listed mana once and cooldown rejection preserves resources: 9/9
PASS successful cast spends listed cost once: observed 830 eq expected 830 @ successful_cast
PASS successful cast grants shield: observed 210 gt expected 0 @ successful_cast
PASS successful cast starts listed cooldown: observed 14 eq expected 14 @ successful_cast
PASS ordinary mana regeneration control: observed 832.9993 eq expected 833 @ before_recast
PASS recast occurs during cooldown: observed 13.5 gt expected 0 @ before_recast
PASS rejected recast preserves mana: observed 832.9993 eq expected 832.9993 @ cooldown_recast
PASS rejected recast preserves cooldown deadline: observed 14 eq expected 14 @ cooldown_recast
PASS rejected recast preserves shield lifetime: observed 5 eq expected 5 @ cooldown_recast
PASS idle opponent mana control: observed 900 eq expected 900 @ cooldown_recast

## Timeline

0.0s / Investigating
0.2s / Investigating
36.9s / Reproducing
38.1s / Reproduced
38.1s / Reviewing specification
54.7s / Bug reproduced — no repair attempted
0.0s / Repairing
7.1s / Compiling
13.6s / Verifying
18.0s / Needs developer review


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
