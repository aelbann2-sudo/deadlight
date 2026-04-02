# Deliverable 2 Narrative and World-Building Appendix

## Narrative Premise

The player is the only known survivor of a failed extraction attempt inside a quarantined urban district. EVAC Command promises rescue in five nights, but each transmission reveals that the outbreak is tied to a military research program called Project Lazarus. As the nights progress, the player learns that Dr. Chen's regenerative experiments produced Subject 23, a learning and adaptive infected organism that became the source of the collapse.

## What Happened Before the Player Arrived

Before gameplay begins, the district was part of an emergency evacuation corridor. Initial response efforts held for a short time, but communications collapsed, infrastructure failed, and survivor groups became isolated. Military personnel attempted to contain evidence of Project Lazarus while civilians improvised barricades, scavenged supplies, and left route markers behind. By the time the player takes control, the space feels suspended between evacuation and abandonment.

## Key Figures and Forces

- **EVAC Command:** the player's main contact and the source of tactical and narrative updates during the five-night holdout.
- **Dr. Chen:** lead researcher on Project Lazarus whose personal logs reveal both scientific ambition and moral collapse.
- **Subject 23:** the original infected source, framed as a learning biological threat rather than a generic zombie.
- **Military response units:** present through orders, reports, and evidence of cover-up behavior.
- **Civilian survivors:** represented indirectly through journals, improvised defenses, abandoned supplies, and route markings.

## Story Delivery Methods in the Current Build

### 1. Phase-Based Radio Storytelling

`RadioTransmissions` delivers lore, warnings, and escalation beats at major phase transitions. This keeps core narrative information attached to active play rather than hidden in menus.

### 2. Triggered and Queued Dialogue

`NarrativeManager`, `DialogueData`, and `StoryTrigger` support story delivery tied to game state, specific spaces, and one-time events. This allows us to place important dialogue where the player is already moving or making decisions.

### 3. Environmental Lore Discovery

`EnvironmentalLore` stores optional entries such as:

- lab notes,
- survivor journals,
- military orders,
- newspaper clippings,
- intercepted radio logs.

These entries build the history of the world without forcing the player to stop the action unless they choose to engage more deeply.

### 4. Intro and Ending Sequences

The intro establishes the player's isolation and the rescue countdown. The ending system frames the run as a larger conflict tied to Subject 23 and the consequences of Project Lazarus.

## Environmental Storytelling Goals

The environment is meant to suggest:

1. The district was actively defended before it was abandoned.
2. Survivors used improvised systems, route logic, and emergency supplies to stay alive.
3. The outbreak was not purely accidental; it was shaped by failed experimentation and institutional secrecy.

We aim for the player to infer this through layout, pacing, narrative hotspots, safe-to-dangerous transitions, and the placement of clues rather than through exposition alone.

## How Dev2 Systems Support World-Building

Deliverable 2's new mechanics are also part of the fiction:

- **Crafting** represents improvised survival prep from scavenged material.
- **Blueprint Tokens** imply recovered technical instructions or tactical intel.
- **Contested drops** reinforce the idea that aid still exists, but only arrives in unstable, high-risk bursts.
- **Day objectives** frame preparation as purposeful tactical work rather than filler between combat phases.
- **Dusk summaries and night modifiers** communicate that planning and survival knowledge materially affect the world.

This matters because the narrative layer should not sit on top of the mechanics as decoration. It should explain why the systems exist and why the player cares about them.

## Intended Player Inference

By the end of the build, a player should understand the following even without reading every lore entry:

1. The outbreak came from a human-made project, not a random apocalypse.
2. The district was part of a failed containment and evacuation effort.
3. The player's preparation, scavenging, and survival are part of a larger attempt to outlast a collapsing military response.
4. Subject 23 is the central threat behind the infected escalation.

## Narrative Design Outcome

The goal of this appendix is not to claim a fully authored story campaign. Instead, it shows that the project now has a clear fiction, identifiable actors, layered story channels, and a world that supports the gameplay loop through consistent theme and context.
