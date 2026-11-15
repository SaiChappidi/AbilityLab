# Dex — verified ability-interaction repairs

A DevHacks Prompt 02 development-agent demo. An engineer reports an interaction regression; signed-in Codex investigates actual failures and proposes a constrained source repair. Unity compiles it, protected tests check it, and an automated arena replay verifies the visible behavior.

Open **Unity 6000.6.9f1**, then **Dex → Agent Workbench**. Select an encounter and use **DEMO: INJECT + RUN** for an explicitly labeled, repeatable demonstration. Use **RUN AUTOMATED REPAIR** for the current loaded source and your report. No OpenAI API key is required; the installed Codex CLI uses your ChatGPT sign-in and its usage limits.

The arena includes simplified Ahri, Morgana, Ezreal and **current-kit Mel**, with official portrait/ability icons and original chamber art. It is a verification sandbox, not a recreation of live League.

- [Setup, controls, architecture and limitations](DEX_README.md)
- [Current test evidence and live recordings](DexReports/evidence/v2/README.md)
- [Short demo sequence](DEMO_SCRIPT.md)
- [Official artwork attribution](THIRD_PARTY_ASSETS.md)
- [Visual direction](ART_DIRECTION.md)

The current protected oracle has **224 assertions**. Actual scene replays are an additional gate. No human time savings are claimed without a measured manual comparison; an opt-in human baseline timer is included.
