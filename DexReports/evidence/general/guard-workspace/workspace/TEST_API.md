# Reusable actual-arena testing API

The host owns Unity and verification. Request read/search operations in the structured requests array; do not use native shell tools. A read request has tool=read,path (relative snapshot file),query (empty),start (1-based line),count (up to 400). A search request has tool=search,path (file or directory, empty for whole snapshot),query (literal substring),start (unused),count (up to 400 matches). All five fields are required. Return requests=[] when ready to propose the scenario. During inspection, scenario may contain a placeholder with empty actions/checks; it is executed only after requests are empty.
The host executes proposed JSON scenarios and returns observations and failures. There are no scenario identifiers or report-specific routes in general mode. Search source to choose useful operations and quantitative expectations. Do not assume a failure or request edits before an observable reproduction fails.

Scenario: title, expectedBehavior, player/opponent (Ahri/Morgana/Ezreal/Mel), integer seed, actions array, checks array. Items, waves, AI, sound and practice healing are off. Initial player/enemy positions are (-6,-2)/(6,2), with 1800 health and 900 mana. Ordinary mana regeneration in Step still occurs. Every action records state. Give relevant actions unique label strings for assertions. initial is the first checkpoint.

The strict response schema requires all action/check keys. Use empty strings, zero and false for unused action fields, and empty compare strings for literal assertions.
Actions:
- place: unit,x,y (board bounds x +/-13,y +/-6.5); sets position and stationary goal.
- spawn: unit (new id),champion (roster or Minion),red (team),x,y.
- reset: real arena reset; drops spawned ids and rebinds player/enemy.
- set: unit,field,value. Float fields: hp,maxHp,mana,ap,ad,armor,mr,haste,controlUntil,shield,shieldUntil,barrier,barrierUntil,novaUntil,speedUntil,slowUntil,overwhelmUntil,markUntil,tetherUntil,attackAt,respawn,readyQ,readyW,readyE,readyR. Integers: searing,overwhelm,rushCharges. Use set for INITIAL CONDITIONS, not manufacturing passing state after gameplay.
- move: unit,x,y. attack: unit,target. stop: unit.
- cast: unit,slot (Q/W/E/R),x,y; actual DexArena.Cast.
- control: unit,text (Root/Stun/Charm/Ground),seconds; actual Combat.CC.
- damage: unit (source),target,value (raw damage),text (physical or magic); actual arena Hit.
- advance: seconds (0..30),dt (optional, default 1/60, .005..0.1); same TickSimulation used by the playable Update.
- observe: named checkpoint without mutation.
Limits: 100 actions, 120 total simulated seconds, 15000 ticks.

Checks: label,at (checkpoint),unit (id or world),field,relation (eq/ne/lt/lte/gt/gte),value (default zero),tolerance (0..0.1, default .001). Optional compareAt,compareUnit,compareField add that earlier observed value to value, enabling deltas without hardcoding balances. A failure means observed does not satisfy relation against expected. Checks must express the report with meaningful controls; invalid commands and exceptions are not reproductions. The independent specification reviewer may reject unsupported expectations.

Observed unit fields: all float/integer fields above, x,y,present,alive,controlled,shielded,cooldownQ/W/E/R. The textual control is in the trace; assertions use controlled (0/1). World fields: time,shots,zones,effects,units,kills,deaths,cs,worldRoots. Combat events are recorded. Seed and actions are saved for frozen replay.

Repair proposal: analysis and edits [{path,find,replace,create}]. Existing files require exact uniquely occurring find text. create=true requires a new file and empty find. Ordinary multi-line, multi-file C# gameplay edits are supported. Paths must be relative Assets/... .cs paths; no traversal, Editor, Tests, Diagnostics or verification infrastructure. Host stages patches in this isolated copy, rejects host/OS APIs, checks live hashes, backs up originals and captures actual diffs. Frozen scenarios/assertions cannot be changed by a repair proposal. No test-detection or report-specific hacks in gameplay.

When the expected behavior is ambiguous, return needsReview=true with the ambiguity. Passing a test cannot prove the entire game correct. State only observations.
