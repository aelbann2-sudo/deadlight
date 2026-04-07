# Deadlight Deliverable 2 Mid-Project Report

**Course:** CS4483B/9541 Game Design
**Team:** Group 16
**Project:** Deadlight: Survival After Dark
**Build Focus:** Deliverable 2 daytime preparation, contested drops, and integrated narrative/world-building

## Project Overview

Deliverable 2 moves our project from an early zombie-survival prototype toward a more complete playable loop with clearer guidance, stronger systemic depth, and a more coherent world. The current Deliverable 2 scope is a two-level campaign (Levels 1-2, six nights total) with alternating day, night, and dawn phases. During the day, the player explores, scavenges, completes objectives, and prepares for the next defense. At night, the player survives escalating zombie waves. At dawn, the player spends points on weapons, upgrades, and supplies before the cycle repeats.

Our main focus for this submission was making daytime play meaningful rather than passive. We added a crafting system, contested supply drops, blueprint-token rewards, and day objective bonuses so the player's preparation directly changes the difficulty and tempo of the following night. At the same time, we strengthened immersion by tying those systems into the game's fiction through radio transmissions, lore pickups, environmental storytelling, and an ongoing narrative about EVAC Command, Dr. Chen, Project Lazarus, and Subject 23.

## Level Design and Player Guidance

Our level structure is built around readable landmarks and a semi-linear flow. We want the player to feel pressure and scarcity, but not confusion. To support that, we use spatial anchors such as the safe area, scavenging routes, contested drop locations, and objective spaces that can be recognized quickly during play. The day phase encourages movement outward from safety, while the night phase collapses the player's attention back toward defense and survival. This phase contrast helps us control pacing without hard-locking exploration.

We guide the player through a combination of visual and systemic cues. Objective titles and descriptions are generated during the day through `StoryObjective`, and supporting UI elements such as the objective HUD and objective markers reinforce where attention should go. Radio transmissions provide short contextual directions at the start of each phase, which keeps guidance in-world instead of relying only on abstract UI. We also use state changes such as helicopter broadcasts, drop descent, secure timers, and dusk summaries to make the world communicate what matters next.

Balancing challenge with exploration was important. The player is rewarded for taking risks during the day, but the systems do not force a single optimal path. Optional exploration can produce lore, resources, crafting materials, or a contested supply drop. If the player ignores preparation entirely, the following night starts under a short soft penalty. This keeps exploration meaningful while still allowing different play styles.

## Narrative Design and World-Building

Our narrative design is built around a "play-first" delivery model. Instead of separating story from gameplay with long cutscenes, we embed fiction into systems the player is already using. The player is framed as the sole survivor of a crashed extraction attempt, holding out across the six-night Deliverable 2 run while EVAC Command tries to assemble a rescue. As the run continues, the radio narrative reveals the history of Project Lazarus, the role of Dr. Chen, and the emergence of Subject 23 as the source of the outbreak.

We use several layers of narrative delivery:

- `RadioTransmissions` gives phase-based story beats, tactical warnings, and world context.
- `NarrativeManager`, `DialogueData`, and `StoryTrigger` support queued dialogue and location-based story moments.
- `EnvironmentalLore` stores discoverable logs, journals, orders, and intercepted transmissions that deepen the setting.
- `IntroSequence` and `EndingSequence` create a stronger beginning and ending frame for the player's survival run.

This structure lets us combine broad world fiction with optional detail. A player who only follows the core loop still learns the basic stakes through EVAC broadcasts and phase transitions. A player who explores more deeply can uncover Dr. Chen's logs, military cover-up orders, survivor journals, and evidence that the infected are not simply mindless enemies but part of a larger evolving system.

World-building also appears in how the mechanics are framed. Daytime crafting is not presented as an abstract tech tree; it represents improvised survival preparation. Recipes such as `Ammo Cache`, `Field Med`, `Shock Beacon`, and `Weakpoint Intel` imply that the player is scavenging, assembling, and planning for the coming night. Contested supply drops reinforce the fiction of a collapsing emergency zone where outside aid still appears occasionally, but only under pressure and limited time. Blueprint tokens work as a fiction-friendly progression material because they suggest recovered plans, instructions, or tactical intel rather than arbitrary currency.

Earlier narrative work also shaped how we think about the environment itself. The safe house, alley routes, supply areas, and crisis landmarks are meant to suggest a district that was defended, evacuated poorly, and then abandoned under pressure. Even when the player is not reading explicit lore, the project aims for the environment to imply last stands, interrupted plans, and improvised communication between survivors.

The result is a narrative layer that supports immersion without interrupting the survival loop. Story is delivered through movement, tension, discovery, and state change rather than only through exposition.

## Systems Design and Game Balance

Deliverable 2's main systems work is the connection between day preparation and night survival. The `CraftingSystem` introduces a resource economy built around Scrap, Wood, Chemicals, Electronics, and Blueprint Tokens. Each craft has an opportunity cost, and per-day caps prevent a single dominant strategy. This means the player must decide whether to invest in immediate security, healing, ammo, or longer-term efficiency.

The effects of crafting are intentionally direct and legible:

- `Ammo Cache` increases reserve ammo at night start.
- `Field Med` grants healing value at night start.
- `Shock Beacon` slows enemies.
- `Weakpoint Intel` lowers enemy health and damage.

This design makes the daytime loop easy to read. The player is not asked to trust invisible backend math; the dusk summary and following night communicate what changed and why. The soft no-prep penalty is also part of this philosophy. If the player neither crafts nor secures a contested drop, the game applies a short damage increase to enemies early in the night. This is not meant as punishment for experimentation, but as a clear reminder that daytime actions matter.

Contested supply drops extend the same risk-reward principle. The drop state flow (`Broadcast -> Descent -> Secure -> Resolved/Expired`) creates a timed decision point that interrupts routine scavenging. Higher-tier contested rewards include better materials and blueprint tokens, so the player is encouraged to take controlled risks rather than simply sweep the map passively.

We also connect progression systems together so they feel coherent instead of isolated. Points, ammo rewards, resources, objective rewards, weapon unlocks, and day buffs all contribute to the same survival curve. This helps maintain fairness because the player can understand how preparation, combat performance, and shop decisions influence later nights.

## Progression Systems and Rewards

Player progression in *Deadlight* is structured across both short-term and long-term loops. The short-term loop is the individual day-night cycle: gather, prepare, survive, recover. The long-term loop in Deliverable 2 is the six-night run across Levels 1-2, where each night introduces stronger enemies, new threats, and additional weapon options.

We use a mix of intrinsic and extrinsic rewards. Intrinsic motivation comes from survival mastery, better routing, improved preparation, and learning how systems combine. Extrinsic motivation comes from point rewards, unlockable weapons, objective payouts, rare drop materials, and better build options in later phases. The shop at dawn, weapon availability by night, armor upgrades, and day objective bonuses all contribute to a sense of upward momentum.

Narrative discovery also acts as a progression layer. Lore is optional, but it gives players another reason to explore and creates a second kind of reward beyond raw combat strength. This helps the game appeal to both mastery-driven and curiosity-driven players.

## Testing and Iteration

We iterated on both mechanics and narrative clarity through a combination of peer feedback and runtime validation. Earlier narrative playtesting showed that players could correctly read the broad crisis context of the world, but some more specific story elements were too subtle. In response, we strengthened explicit communication through radio beats, clearer trigger-driven story moments, stronger lore framing, and a more legible connection between tactical systems and fiction.

On the systems side, we added automated PlayMode coverage for the new daytime-preparation mechanics. Current tests verify:

- day-only crafting restrictions,
- resource spending and per-day recipe caps,
- contested secure and expire flows,
- blueprint token rewards and consumption,
- pickup purpose hints,
- no-prep penalty behavior,
- regression coverage for regular crates and night emergency drops.

Local smoke validation also confirms that the runtime loop transitions correctly across day, night, and dawn states without errors or warnings. This combination of peer feedback and runtime checks helped us iterate with more confidence and avoid adding features that only worked in ideal cases.

## Technical Notes

Several Unity-specific systems supported this iteration:

- Event-driven managers such as `GameManager`, `DayObjectiveSystem`, `CraftingSystem`, and `NarrativeManager` keep phase transitions and feature interactions synchronized.
- Coroutines are used for timed transmissions, intro pacing, drop sequencing, and other temporal beats.
- The narrative layer uses `DialogueData` and queued dialogue playback so story content can be triggered by state, space, or discovery.
- Runtime UI such as objective displays, crafting feedback, and radio overlays help maintain legibility without forcing the player into separate menus too often.
- PlayMode tests were used to validate new rules and reduce regression risk as systems became more interdependent.

Key implementation files include:

- `Assets/Scripts/Core/DayObjectiveSystem.cs`
- `Assets/Scripts/Core/RadioTransmissions.cs`
- `Assets/Scripts/Core/GameFlowController.cs`
- `Assets/Scripts/Systems/CraftingSystem.cs`
- `Assets/Scripts/Systems/SupplyCrate.cs`
- `Assets/Scripts/Systems/Pickup.cs`
- `Assets/Scripts/Narrative/NarrativeManager.cs`
- `Assets/Scripts/Narrative/EnvironmentalLore.cs`
- `Assets/Scripts/Narrative/StoryTrigger.cs`
- `Assets/Scripts/Narrative/IntroSequence.cs`
- `Assets/Tests/PlayMode/DaytimePrepRuntimeTests.cs`

## Conclusion

Deliverable 2 represents a meaningful step toward a cohesive survival game rather than a collection of disconnected features. The project now has clearer player guidance, a more intentional day-to-night preparation loop, stronger progression structure, and a narrative layer that supports immersion through radio dialogue, lore, world logic, and environmental context.

Most importantly, the narrative and world-building elements are not separate from the gameplay systems. They are part of how the player understands why they are scavenging, why contested drops matter, why EVAC keeps transmitting, and what is really happening in the world. That integration is the main design goal of this iteration, and it is what makes the current build feel closer to a complete game experience.
