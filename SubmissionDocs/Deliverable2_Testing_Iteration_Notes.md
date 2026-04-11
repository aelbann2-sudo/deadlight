# Deliverable 2 Testing and Iteration Notes

## Narrative and World-Building Playtest Takeaways

Earlier narrative playtests focused on whether players could understand the state of the world without being given a long explanation first. The strongest results were:

- players understood the world as a defensive holdout rather than a neutral arena,
- evacuation failure and survivor desperation were readable from context,
- environmental details and radio framing made the crisis feel ongoing rather than static.

The weaker points were:

- some specific actors and motives were too subtle,
- certain gated or objective-driven spaces could be read as "just gameplay locks" rather than story logic,
- optional clues needed a clearer hierarchy so important ones stood out faster.

## Iteration Decisions Carried into Dev2

Based on that feedback, the current branch emphasizes clearer story signaling:

- stronger radio communication across the current two-level / six-night structure,
- more direct phase messaging during day and night transitions,
- optional lore entries that name actors and explain consequences,
- day objectives and contested events that feel like in-world tactical activity rather than abstract tasks,
- narrative hooks that connect preparation, danger, and escalation.

## Latest Implementation Update (Onboarding and Message Clarity)

The newest iteration in this branch focused on first-session onboarding quality:

- Day 1 guidance is staged in sequence (objective first, then drop guidance).
- The first drop instruction asks the player to follow the marker; hold-to-loot appears only near the crate.
- Night 1 aim/fire guidance is tied to first visible hostiles instead of stacked startup prompts.
- Night 1 pressure in Level 1 is intentionally moderated (slower speed and one-by-one pacing) so players can learn controls.
- COMMS queue handling was updated to reduce cases where one message cuts off another too early.

These updates were made to keep early gameplay readable while preserving the core day-night survival loop.

## Runtime Validation Used for Dev2

The current branch includes PlayMode coverage for the new daytime preparation systems in `Assets/Tests/PlayMode/DaytimePrepRuntimeTests.cs`.

Those tests cover:

- crafting only during day phase,
- resource spending and recipe caps,
- contested drop secure flow,
- contested drop expiry flow,
- blueprint token reward and use,
- pickup purpose hints,
- no-prep soft penalty behavior,
- regression for regular crates and emergency night drops.

The checked-in smoke report in `tmp/runtime_smoke_report.txt` also confirms a clean runtime pass across day, night, and dawn state coverage with zero reported errors or warnings.

## Why This Matters for the Report

The deliverable is not only about adding features. It is also about showing that we tested whether those features:

- communicate clearly,
- fit together coherently,
- preserve game balance,
- strengthen the intended player experience.

These notes give us a direct way to explain that iteration process in the written report without relying on vague statements like "we playtested it and improved it."
