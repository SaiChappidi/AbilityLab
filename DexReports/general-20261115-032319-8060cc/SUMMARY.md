# Project debugging run

Report: Resetting while projectiles are in flight leaves old projectiles in the new match. Reset should clear old projectiles, zones, and effects and restore fresh resources.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 106.7 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires reset to clear projectiles, zones and effects and restore fresh resources. The scenario creates these through gameplay before reset, without manufacturing passing state afterward. The trace reproduces one retained projectile at fresh_match; zones, effects and resources reset successfully. Retain the direct reset witness and its preconditions without imposing new ability balance requirements. Enemy resources are already fresh before the final reset, so their final equality alone does not demonstrate restoration.

## Before

Reset clears active arena objects and restores fresh resources: 23/24
PASS normal_cast_creates_projectile: observed 1 eq expected 1 @ normal_projectile_launched
PASS normal_cast_spends_mana: observed 845 lt expected 900 @ normal_projectile_launched
PASS normal_projectile_damages_enemy: observed 1685.714 lt expected 1800 @ normal_projectile_impact
PASS normal_projectile_roots_enemy: observed 1 eq expected 1 @ normal_projectile_impact
PASS normal_projectile_consumed: observed 0 eq expected 0 @ normal_projectile_impact
PASS control_reset_clears_impact_effect: observed 0 eq expected 0 @ control_reset
PASS control_reset_clears_root: observed 0 eq expected 0 @ control_reset
PASS projectile_present_before_reset: observed 1 eq expected 1 @ active_before_reset
PASS zone_present_before_reset: observed 1 eq expected 1 @ active_before_reset
PASS effect_present_before_reset: observed 1 gt expected 0 @ active_before_reset
PASS health_depleted_before_reset: observed 1700 lt expected 1800 @ active_before_reset
PASS mana_depleted_before_reset: observed 715 lt expected 900 @ active_before_reset
PASS cooldown_active_before_reset: observed 7 gt expected 0 @ active_before_reset
FAIL reset_clears_projectiles: observed 1 eq expected 0 @ fresh_match
PASS reset_clears_zones: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_effects: observed 0 eq expected 0 @ fresh_match
PASS reset_restores_player_health: observed 1800 eq expected 1800 @ fresh_match
PASS reset_restores_player_mana: observed 900 eq expected 900 @ fresh_match
PASS reset_restores_enemy_health: observed 1800 eq expected 1800 @ fresh_match
PASS reset_restores_enemy_mana: observed 900 eq expected 900 @ fresh_match
PASS reset_clears_q_cooldown: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_w_cooldown: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_e_cooldown: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_shield: observed 0 eq expected 0 @ fresh_match

## After

Reset clears active arena objects and restores fresh resources: 24/24
PASS normal_cast_creates_projectile: observed 1 eq expected 1 @ normal_projectile_launched
PASS normal_cast_spends_mana: observed 845 lt expected 900 @ normal_projectile_launched
PASS normal_projectile_damages_enemy: observed 1685.714 lt expected 1800 @ normal_projectile_impact
PASS normal_projectile_roots_enemy: observed 1 eq expected 1 @ normal_projectile_impact
PASS normal_projectile_consumed: observed 0 eq expected 0 @ normal_projectile_impact
PASS control_reset_clears_impact_effect: observed 0 eq expected 0 @ control_reset
PASS control_reset_clears_root: observed 0 eq expected 0 @ control_reset
PASS projectile_present_before_reset: observed 1 eq expected 1 @ active_before_reset
PASS zone_present_before_reset: observed 1 eq expected 1 @ active_before_reset
PASS effect_present_before_reset: observed 1 gt expected 0 @ active_before_reset
PASS health_depleted_before_reset: observed 1700 lt expected 1800 @ active_before_reset
PASS mana_depleted_before_reset: observed 715 lt expected 900 @ active_before_reset
PASS cooldown_active_before_reset: observed 7 gt expected 0 @ active_before_reset
PASS reset_clears_projectiles: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_zones: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_effects: observed 0 eq expected 0 @ fresh_match
PASS reset_restores_player_health: observed 1800 eq expected 1800 @ fresh_match
PASS reset_restores_player_mana: observed 900 eq expected 900 @ fresh_match
PASS reset_restores_enemy_health: observed 1800 eq expected 1800 @ fresh_match
PASS reset_restores_enemy_mana: observed 900 eq expected 900 @ fresh_match
PASS reset_clears_q_cooldown: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_w_cooldown: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_e_cooldown: observed 0 eq expected 0 @ fresh_match
PASS reset_clears_shield: observed 0 eq expected 0 @ fresh_match

## Timeline

0.0s / Investigating
0.2s / Investigating
64.7s / Reproducing
65.9s / Reproduced
65.9s / Reviewing specification
88.3s / Repairing
96.5s / Compiling
102.7s / Verifying
106.7s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
