# Deadlight: Survival After Dark — Deliverable 2 Mid-Project Report

**Course:** CS4483B / 9541 Game Design  
**Team:** Group 16  
**Members:** Simranjeet Singh, Abdelrahman El Banna, Koroush Emari, Ashraf Esam Mahdi  
**Date:** April 2026  

> **Authoritative write-up:** The submission PDF is generated from `Deliverable2_MidProject_Report.tex` (same folder). This Markdown is a plain-text mirror of that document for graders who prefer `.md`.

## Submission scope

| | |
|--|--|
| **Playable scope** | Levels 1–2 (Town Center, Suburban); six objective nights total |
| **Coming soon** | Levels 3–4 are **not completed** in this milestone; full content is planned for **Deliverable 3** (menu previews may appear) |
| **Project** | Top-down zombie survival prototype in Unity |
| **Focus** | Level design, guidance, narrative, systems, progression, testing |

## Project overview

Deliverable 2 is a playable Unity slice of *Deadlight*: **Levels 1–2** (Town Center and Suburban), six campaign nights. **Levels 3–4 are not completed yet** and are planned for **Deliverable 3**.

**Day:** explore, objectives, resources. **Night:** escalating waves. **Dawn:** rewards and shop (weapons, armor, ammo, healing, upgrades). Between day and night, **`DayNightCycle`** runs a short timed handoff; **`GameManager`** stays in **`DayPhase`** until **`StartNightPhase`** (the **`GameState.Transition`** enum exists for UI hooks but is not the active manager state on this path). **Preparation** ties day to night through objectives, contested drops, and resources; radio, environment, and missions carry EVAC Command, Project Lazarus, Dr. Chen, and Subject 23.

The criteria table below matches the section order that follows.

## Criteria coverage

| Rubric point | Implemented | Demonstrated in-game |
|--------------|-------------|----------------------|
| Level design & guidance | Two authored spaces with landmarks and objective routes | Objective HUD, world markers, support markers (drops, nearest enemy) |
| Narrative & world-building | Radio, story objectives, lore, Lazarus arc framing | Plot advances by night; not “waves only” |
| Systems & balance | Day–night–dawn economy (points, ammo, health, armor, utilities, upgrades) | Better day play → better night readiness; contested drops |
| Progression & rewards | Six-night slice, unlock-based level access | Level 2 after Level 1; level-complete panel; deploy next or return to menu |
| Testing & iteration | Playtests + Play Mode checks | UI/objective/comms iteration from observed confusion |

Sections below expand each row in order.

## Level design and player guidance

Levels balance **readable structure** with **player freedom**. Level 1 introduces crash site, checkpoint, and hospital-style beats; Level 2 is more open and punishing. Guidance stacks: objective HUD, world markers, radio, landmarks, and pacing shifts between exploration and defense.

Recent implementation updates focus on **structured onboarding** in Level 1:

- **Day 1 guidance order:** objective guidance appears first, then drop guidance appears after objective completion with a short delay.
- **Drop interaction teaching:** the first Day 1 drop message asks the player to follow the marker; the hold-to-loot prompt appears only when the player is near the crate.
- **Night 1 combat teaching:** aim-and-fire guidance is tied to the first visible enemies, with spaced messages to avoid overlap.
- **Controlled early pressure:** Night 1 uses one-by-one enemy pressure and reduced enemy speed for Level 1 onboarding, then returns to normal campaign pacing.

## Narrative design

Story is **play-first**: radio, objectives, optional lore, dressed environments, short framing beats. The main arc stays clear without requiring every collectible.

**Segment thrust (D2):**

- **L1 N1–N3:** Failed evacuation, EVAC as voice of order, early Lazarus hints (crash, checkpoint, clinic chain).
- **L2 N1–N3:** Suburban escalation; quarantine and Lazarus evidence; extraction fantasy tied to scavenging and survival.

## Systems and balance

Daytime supports scavenging, **contested timed drops**, mission leads, and looting under time pressure. Balance table (defaults in code; tunable in Unity):

- **Night survived:** `PointsSystem` base 100 + 50 × `nightsSurvived` (serialized defaults); `nightsSurvived` increments every dawn, including objective-retry loops.
- **Milestones:** `ProgressionManager` matches stipends to milestone rows by campaign night index (defaults: nights 1–3 at +100 / +150 / +200). Rows reset on new-level setup; add nights 4–6 in Unity if Level 2 should repeat that cadence.
- **Contested tiers:** +110 / +170 / +260 points plus supplies (`SupplyCrate`).
- **Missed objectives:** Default `GameManager` flow: **one retry of the same campaign night** (`maxObjectiveRetriesPerStep = 1`), so one miss **replays the step** instead of immediately spiking difficulty. After a forced advance, a **queued** **1.2×** multiplier is consumed on the **next night index advance** (`CurrentNightEnemyPenaltyMultiplier` → `WaveManager`). Radio/UI distinguish retry vs penalty. Level advance uses a **fresh baseline** (see below); no partial point carry between maps on the playable `StartNextLevel` path (carryover helpers exist but are not wired there).
- **Dawn weapons:** SMG and sniper at shop night 2+; assault rifle and grenade launcher night 3+; flamethrower night 4+; **shotgun not listed** in dawn weapon shop (`GameUI`).
- **Level 1 onboarding pace:** first-night enemy speed and spawn pacing are intentionally reduced for onboarding clarity, then normal pressure scaling resumes on later nights.

**Core loop:** Day → **transition** (`DayNightCycle` into `NightPhase`) → Night → Rewards → Shop → repeat.

## Progression

Six nights: L1×3 then L2×3. Beat 3 (L1 Night 3) clears Level 1 and surfaces Level 2. **Fresh start per level:** `GameManager.StartNextLevel` / `ResetInterLevelProgressionState` resets points session, progression snapshot, upgrades, armor, throwables, and loadout (pistol baseline) so Deliverable 2 stays readable.

**Weapon loadout:** `PlayerShooting` uses **four slots** with hotkeys; the shop adds to **free** slots and blocks duplicate weapon types already carried.

## Testing and iteration

Iteration mixed **observation** (hesitation, missed markers, busy COMMS) with **Play Mode** regression on phase transitions. Examples: simplified objective/comms labeling; support markers for drops and threats; clearer **retry vs queued-penalty** messaging at the day-to-night transition; level-complete details consolidated in the **dedicated panel** (transient congrats overlays reduced on level complete).

The latest iteration also addressed message clarity and timing:

- COMMS playback was stabilized so lines are less likely to be cut off by overlapping queue events.
- Day 1 objective and drop guidance was moved to staged, on-screen prompts to reduce early-channel overload.
- Night 1 aim/fire prompts were paced and tied to visible contact events rather than stacked startup text.

**Playtest protocol (summary):** Fresh Level 1; one full day–night–dawn; story objective; dawn shop; think-aloud where possible; take notes on objectives, contested drops, summaries; one fix category per pass before re-test.

## Technical notes

- **Flow:** `GameManager`, `GameFlowController` (day/night/dawn, drops).
- **Guidance:** `StoryObjective`, objective HUD, and markers, plus staged controllers for objective, drop, and first-contact combat onboarding.
- **Narrative:** Radio, scripted missions, lore systems.
- **COMMS reliability:** `NarrativeManager` queue processing and interrupt protection were revised to reduce premature message overrides.
- **Economy:** `PointsSystem`, pickups, `SupplyCrate`, waves.
- **Crafting:** Hooks exist; **`enableCrafting = false`** in this build.
- **Package:** Unity project (`Assets`, `Packages`, `ProjectSettings`) + Windows executable per course instructions + this report PDF.

## Deliverable 3 (planned)

Extend to **four playable levels**, finish Lazarus arc, polish presentation/audio, rebalance full campaign, stability pass, optional **crafting** if legible, clearer end-of-run UI.

## Conclusion

Deliverable 2 is a coherent slice: guided flow, fiction on missions and radio, systems that reward preparation, and visible progression across Town Center and Suburban. That stack supports exploration, mission leads, radio context, and consequences of daytime choices, and it sets up the four-level campaign in Deliverable 3.

---

**Appendix files (same folder):** `Deliverable2_Narrative_Worldbuilding_Appendix.md`, `Deliverable2_Testing_Iteration_Notes.md`
