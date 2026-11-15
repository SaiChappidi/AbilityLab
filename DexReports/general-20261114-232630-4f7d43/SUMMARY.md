# Project debugging run

Report: Resetting the practice arena while projectiles are in flight leaves stale projectiles in the new match. Reset should remove old projectiles, zones and effects and begin with fresh resources. Reproduce the problem and repair the cleanup.

Result: Verified

Reproduced: True; source repaired: True; task verified: True.

Elapsed: 146.8 seconds (not measured labor saved).

The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.

## Specification review

The report explicitly requires reset to clear old projectiles, zones and effects and restore fresh resources. Gameplay creates all three before reset and depletes health and mana without manufacturing post-reset state. The supplied trace reproduces the defect: shots remains 1 after reset. Original ResetWorld clears zones and effects and constructs fresh fighters but omits shots.Clear(). Retain the direct cleanup and resource witness below. The final enemy-health, enemy-control and clock assertions are vacuous because those values already equal their expected values immediately before that reset; ordinary projectile damage and root balance are outside this cleanup specification.

## Before

Reset removes active combat objects and restores resources: 20/21
PASS normal projectile exists: observed 1 eq expected 1 @ normal_launch
PASS normal projectile damages: observed 1685.714 lt expected 1800 @ normal_hit
PASS normal projectile roots: observed 1 eq expected 1 @ normal_hit
PASS normal projectile consumed: observed 0 eq expected 0 @ normal_hit
PASS projectile active before reset: observed 1 eq expected 1 @ before_reset
PASS zone active before reset: observed 1 eq expected 1 @ before_reset
PASS effect active before reset: observed 1 gt expected 0 @ before_reset
PASS health depleted before reset: observed 1700 lt expected 1800 @ before_reset
PASS mana spent before reset: observed 715 lt expected 900 @ before_reset
FAIL reset clears projectiles: observed 1 eq expected 0 @ after_reset
PASS reset clears zones: observed 0 eq expected 0 @ after_reset
PASS reset clears effects: observed 0 eq expected 0 @ after_reset
PASS reset restores player health: observed 1800 eq expected 1800 @ after_reset
PASS reset restores player mana: observed 900 eq expected 900 @ after_reset
PASS reset restores enemy health: observed 1800 eq expected 1800 @ after_reset
PASS reset clears Q cooldown: observed 0 eq expected 0 @ after_reset
PASS reset clears W cooldown: observed 0 eq expected 0 @ after_reset
PASS reset clears E cooldown: observed 0 eq expected 0 @ after_reset
PASS reset clears shield: observed 0 eq expected 0 @ after_reset
PASS reset clears enemy control: observed 0 eq expected 0 @ after_reset
PASS reset restarts clock: observed 0 eq expected 0 @ after_reset

## After

Reset removes active combat objects and restores resources: 21/21
PASS normal projectile exists: observed 1 eq expected 1 @ normal_launch
PASS normal projectile damages: observed 1685.714 lt expected 1800 @ normal_hit
PASS normal projectile roots: observed 1 eq expected 1 @ normal_hit
PASS normal projectile consumed: observed 0 eq expected 0 @ normal_hit
PASS projectile active before reset: observed 1 eq expected 1 @ before_reset
PASS zone active before reset: observed 1 eq expected 1 @ before_reset
PASS effect active before reset: observed 1 gt expected 0 @ before_reset
PASS health depleted before reset: observed 1700 lt expected 1800 @ before_reset
PASS mana spent before reset: observed 715 lt expected 900 @ before_reset
PASS reset clears projectiles: observed 0 eq expected 0 @ after_reset
PASS reset clears zones: observed 0 eq expected 0 @ after_reset
PASS reset clears effects: observed 0 eq expected 0 @ after_reset
PASS reset restores player health: observed 1800 eq expected 1800 @ after_reset
PASS reset restores player mana: observed 900 eq expected 900 @ after_reset
PASS reset restores enemy health: observed 1800 eq expected 1800 @ after_reset
PASS reset clears Q cooldown: observed 0 eq expected 0 @ after_reset
PASS reset clears W cooldown: observed 0 eq expected 0 @ after_reset
PASS reset clears E cooldown: observed 0 eq expected 0 @ after_reset
PASS reset clears shield: observed 0 eq expected 0 @ after_reset
PASS reset clears enemy control: observed 0 eq expected 0 @ after_reset
PASS reset restarts clock: observed 0 eq expected 0 @ after_reset

## Timeline

0.0s / Investigating
0.2s / Investigating
57.0s / Reproducing
58.1s / Investigating
103.7s / Reproducing
104.1s / Reproduced
104.1s / Reviewing specification
127.4s / Repairing
134.5s / Compiling
143.3s / Verifying
146.8s / Verified


Scope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.
