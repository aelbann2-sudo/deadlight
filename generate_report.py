#!/usr/bin/env python3
"""Generate Deliverable 2 Report PDF for Deadlight: Survival After Dark"""

from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.colors import HexColor
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    PageBreak, ListFlowable, ListItem
)
from reportlab.lib.units import inch
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY

OUTPUT = "Group16_Deliverable2_Report.pdf"

def build():
    doc = SimpleDocTemplate(
        OUTPUT,
        pagesize=letter,
        leftMargin=0.9*inch,
        rightMargin=0.9*inch,
        topMargin=0.8*inch,
        bottomMargin=0.8*inch,
    )

    styles = getSampleStyleSheet()

    title_style = ParagraphStyle(
        "CustomTitle", parent=styles["Title"],
        fontSize=22, spaceAfter=6, textColor=HexColor("#1a1a2e"),
        fontName="Helvetica-Bold",
    )
    subtitle_style = ParagraphStyle(
        "Subtitle", parent=styles["Normal"],
        fontSize=12, spaceAfter=20, alignment=TA_CENTER,
        textColor=HexColor("#555555"),
    )
    h1 = ParagraphStyle(
        "H1", parent=styles["Heading1"],
        fontSize=16, spaceBefore=18, spaceAfter=8,
        textColor=HexColor("#1a1a2e"), fontName="Helvetica-Bold",
    )
    h2 = ParagraphStyle(
        "H2", parent=styles["Heading2"],
        fontSize=13, spaceBefore=12, spaceAfter=6,
        textColor=HexColor("#2d3436"), fontName="Helvetica-Bold",
    )
    body = ParagraphStyle(
        "Body", parent=styles["Normal"],
        fontSize=11, leading=15, spaceAfter=8,
        alignment=TA_JUSTIFY,
    )
    bullet = ParagraphStyle(
        "Bullet", parent=body,
        leftIndent=20, bulletIndent=8, spaceAfter=4,
    )

    story = []
    sp = lambda pts: Spacer(1, pts)

    # --- TITLE PAGE ---
    story.append(sp(80))
    story.append(Paragraph("Deadlight: Survival After Dark", title_style))
    story.append(Paragraph("Deliverable 2: Mid-Project Report", subtitle_style))
    story.append(sp(20))
    story.append(Paragraph("Group 16", ParagraphStyle("g", parent=body, alignment=TA_CENTER, fontSize=13)))
    story.append(Paragraph(
        "Simranjeet Singh, Abdelrahman El Banna, Koroush Emari, Ashraf Esam Mahdi",
        ParagraphStyle("names", parent=body, alignment=TA_CENTER, fontSize=11),
    ))
    story.append(sp(10))
    story.append(Paragraph("CS4483B - Game Design", ParagraphStyle("c", parent=body, alignment=TA_CENTER)))
    story.append(Paragraph("March 25, 2026", ParagraphStyle("d", parent=body, alignment=TA_CENTER)))
    story.append(PageBreak())

    # --- 1. LEVEL DESIGN & PLAYER GUIDANCE ---
    story.append(Paragraph("1. Level Design & Player Guidance", h1))

    story.append(Paragraph("1.1 Spatial Layout", h2))
    story.append(Paragraph(
        "The game takes place in an abandoned urban town rendered from a top-down perspective. "
        "The map is structured as a bounded arena (approximately 48 x 38 units) enclosed by perimeter walls "
        "that prevent the player from leaving the play area. This confinement creates tension, as the player "
        "cannot simply flee from threats indefinitely.", body))

    story.append(Paragraph(
        "The town is divided into distinct zones, each with a different strategic purpose:", body))

    items = [
        "<b>Central Safe Zone:</b> The player spawns at the center of the map, surrounded by barricades that "
        "provide initial cover. This area is the safest starting point and serves as a natural home base.",
        "<b>Town Buildings:</b> Seven houses are scattered around the map at varying distances from center. "
        "These act as natural obstacles that break line of sight, create chokepoints, and provide cover "
        "during combat encounters.",
        "<b>Resource Zones:</b> During the day phase, health (red) and ammo (yellow) pickups spawn at random "
        "positions throughout the map, encouraging exploration. Pickups bob gently to draw the player's eye.",
        "<b>Perimeter Danger Zone:</b> Enemy spawn points ring the outer edges of the map. Enemies spawn at a "
        "distance of 12-20 units from the player, giving a brief window to react as they close in.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    story.append(Paragraph("1.2 Visual Cues and Player Guidance", h2))
    story.append(Paragraph(
        "We use several visual strategies to guide the player without explicit tutorials:", body))

    items = [
        "<b>Ground Paths:</b> Dirt/sand-colored paths cross the center of the map in a cross pattern, naturally "
        "guiding the player toward the four cardinal directions where resources and buildings are located.",
        "<b>Color Coding:</b> Health pickups are red, ammo pickups are yellow, and enemies have a greenish tint. "
        "This consistent color language lets players quickly identify objects at a glance.",
        "<b>Environmental Landmarks:</b> Trees, rocks, and crates are placed to create visual variety and "
        "help players orient themselves. Houses serve as major landmarks for navigation.",
        "<b>HUD Indicators:</b> A controls hint at the bottom right of the screen reminds players of available "
        "actions. Status messages fade in at screen center to communicate phase changes.",
        "<b>Camera Follow:</b> The camera smoothly follows the player with configurable smoothing, keeping "
        "the player centered while providing awareness of approaching threats.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    story.append(Paragraph("1.3 Pacing and Challenge Balance", h2))
    story.append(Paragraph(
        "The day/night cycle creates a natural pacing rhythm. The 45-second day phase provides low-pressure "
        "time to explore and collect resources, while the night phase ramps up with wave-based enemy spawning. "
        "The day timer is displayed on screen so the player always knows how much preparation time remains. "
        "This alternation between calm exploration and intense survival prevents fatigue and maintains engagement "
        "across the full 5-night run.", body))

    # --- 2. NARRATIVE DESIGN & WORLD-BUILDING ---
    story.append(PageBreak())
    story.append(Paragraph("2. Narrative Design & World-Building", h1))

    story.append(Paragraph("2.1 Story Context", h2))
    story.append(Paragraph(
        "The player is a lone survivor separated from an evacuation convoy in a zombie-overrun town. "
        "Radio transmissions from EVAC Command provide the primary narrative thread: a rescue helicopter "
        "will arrive at dawn on the fifth day if the player can survive. This simple but compelling premise "
        "gives every night survived a sense of progress toward a concrete goal.", body))

    story.append(Paragraph("2.2 Radio Transmission System", h2))
    story.append(Paragraph(
        "At the start of each day phase, the player receives radio transmissions rendered in green "
        "terminal-style text at the bottom of the screen. Each night's transmissions are unique:", body))

    data = [
        ["Night", "Transmission Theme"],
        ["1", "Introduction: EVAC Command establishes contact, explains the 5-night timeline"],
        ["2", "Intel update: infected are evolving, shotgun availability mentioned"],
        ["3", "Deteriorating situation: new enemy types detected, assault rifle available"],
        ["4", "Massive surge warning: worst night yet, flamethrower unlocked"],
        ["5", "Final night: boss entity detected, everything on the line"],
    ]
    t = Table(data, colWidths=[0.6*inch, 5.4*inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), HexColor("#ffffff")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 10),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#cccccc")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)
    story.append(sp(8))

    story.append(Paragraph(
        "Additionally, gameplay tips are displayed after the narrative transmissions (e.g., reminders about "
        "sprinting, reloading, and using cover). This contextual guidance helps new players without breaking "
        "immersion.", body))

    story.append(Paragraph("2.3 Environmental Storytelling", h2))
    story.append(Paragraph(
        "The abandoned town setting tells its own story through environmental details: boarded-up houses, "
        "scattered crates, and overgrown vegetation suggest a community that was hastily evacuated. "
        "The day/night lighting transitions reinforce the atmosphere, with warm daytime tones giving way "
        "to cold, dark nighttime colors that heighten tension.", body))

    # --- 3. SYSTEMS DESIGN & GAME BALANCE ---
    story.append(PageBreak())
    story.append(Paragraph("3. Systems Design & Game Balance", h1))

    story.append(Paragraph("3.1 Resource Systems", h2))
    story.append(Paragraph(
        "The game manages several interconnected resources:", body))

    items = [
        "<b>Health:</b> The player starts with 100 HP. Damage from enemies reduces health, and reaching 0 "
        "triggers death. Health pickups (red circles) restore 25 HP each. The health bar uses a color gradient "
        "from green (full) to red (low) for immediate visual feedback. An invincibility frame of 0.5 seconds "
        "prevents chain-stagger deaths.",
        "<b>Ammo:</b> The player starts with a 15-round magazine and 60 reserve rounds. Ammo pickups add 30 "
        "rounds to reserves. The reload mechanic (1.2 seconds) creates a risk window where the player is "
        "vulnerable, adding tactical depth to combat.",
        "<b>Stamina:</b> Sprinting consumes stamina (shown as a blue bar below health). When depleted, the "
        "player cannot sprint until stamina regenerates. This prevents infinite kiting and forces players to "
        "manage their escape routes.",
        "<b>Points:</b> Earned from killing enemies (10 per kill) and surviving nights (100 + 50 per night number). "
        "Points are spent at the Dawn Phase shop for upgrades, creating a clear risk-reward economy.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    story.append(Paragraph("3.2 Difficulty Scaling", h2))
    story.append(Paragraph(
        "Difficulty scales across two axes: per-night escalation and player-selected difficulty modes.", body))

    data = [
        ["Parameter", "Night 1", "Night 3", "Night 5"],
        ["Enemies per wave", "5", "8-9", "12+"],
        ["Total waves", "4", "6", "8"],
        ["Enemy HP", "50", "70", "100"],
        ["Spawn interval", "3.0s", "2.0s", "1.5s"],
    ]
    t = Table(data, colWidths=[1.8*inch, 1.2*inch, 1.2*inch, 1.2*inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), HexColor("#ffffff")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 10),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#cccccc")),
        ("ALIGN", (1, 0), (-1, -1), "CENTER"),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)
    story.append(sp(8))

    story.append(Paragraph(
        "Three difficulty modes (Easy, Normal, Hard) adjust enemy stats, resource availability, and score "
        "multipliers. Hard mode applies a 1.5x score multiplier to the leaderboard, rewarding skilled players "
        "while keeping Easy mode accessible for newcomers.", body))

    story.append(Paragraph("3.3 Combat Feel", h2))
    story.append(Paragraph(
        "We invested significant effort in making combat feel satisfying:", body))
    items = [
        "<b>Screen shake</b> on shooting and taking damage provides visceral feedback.",
        "<b>Muzzle flash</b> sprites appear briefly at the fire point.",
        "<b>Hit particles</b> burst from enemies when bullets connect.",
        "<b>Death particles</b> scatter when enemies are killed.",
        "<b>Bullet trails</b> show projectile paths with a fading yellow-orange trail.",
        "<b>Damage flash:</b> A red overlay pulses across the screen when the player takes damage.",
        "<b>Enemy health bars</b> appear above damaged enemies so the player can prioritize targets.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    # --- 4. PROGRESSION SYSTEMS & REWARDS ---
    story.append(PageBreak())
    story.append(Paragraph("4. Progression Systems & Rewards", h1))

    story.append(Paragraph("4.1 Night-Based Progression", h2))
    story.append(Paragraph(
        "The game spans 5 nights of escalating difficulty. Each survived night advances the player to a "
        "Dawn Phase where they can spend points and access newly unlocked weapons. This creates clear, "
        "tangible milestones that motivate continued play.", body))

    data = [
        ["Night", "Milestone Unlock"],
        ["1", "Shotgun available in shop (100 points)"],
        ["2", "Assault Rifle available (200 points)"],
        ["3", "Grenade Launcher planned"],
        ["4", "Flamethrower planned"],
        ["5", "Victory - rescue helicopter arrives"],
    ]
    t = Table(data, colWidths=[0.8*inch, 5.2*inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), HexColor("#ffffff")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 10),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#cccccc")),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)
    story.append(sp(8))

    story.append(Paragraph("4.2 Dawn Phase Shop", h2))
    story.append(Paragraph(
        "After surviving each night, the game pauses and presents a shop interface. Players can spend "
        "their earned points on:", body))
    items = [
        "<b>Health Kit (50 points):</b> Fully restores health. Essential after tough nights.",
        "<b>Ammo Refill (30 points):</b> Adds 60 reserve rounds. Keeps the player combat-ready.",
        "<b>Shotgun (100 points):</b> Close-range powerhouse with 8 pellets per shot. Available from Night 1.",
        "<b>Assault Rifle (200 points):</b> Automatic fire with 30-round magazine. Unlocks at Night 2.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    story.append(Paragraph(
        "Shop buttons are grayed out when the player cannot afford an item or has not reached the required "
        "night, providing clear feedback about what is available and what to work toward.", body))

    story.append(Paragraph("4.3 Intrinsic and Extrinsic Motivation", h2))
    story.append(Paragraph(
        "We balance both types of motivation:", body))
    items = [
        "<b>Extrinsic:</b> Points, weapon unlocks, score tracking, and the Game Over/Victory stat screens "
        "provide concrete rewards. The final score factors in kills, nights survived, and difficulty multiplier.",
        "<b>Intrinsic:</b> The satisfying combat feel (screen shake, particles, responsive controls), the "
        "tension of dwindling resources, and the narrative drive to reach rescue create internal motivation. "
        "Players want to survive not just for points, but because the game makes survival feel meaningful.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    # --- 5. TESTING & ITERATION ---
    story.append(PageBreak())
    story.append(Paragraph("5. Testing & Iteration", h1))

    story.append(Paragraph("5.1 Iterative Development Process", h2))
    story.append(Paragraph(
        "Our development followed an iterative approach where each feature was implemented, tested, and "
        "refined before moving to the next:", body))

    data = [
        ["Issue Found", "Solution Applied"],
        ["Player rotated toward mouse (looked unnatural with pixel art)",
         "Removed body rotation; bullets fire toward mouse cursor independently"],
        ["Enemies required NavMesh (complex setup, failed at runtime)",
         "Created SimpleEnemyAI with direct Rigidbody2D movement"],
        ["Unity tags not defined (Enemy/Player tags caused crashes)",
         "Switched to GameObject.Find() and component-based detection"],
        ["Bullets passed through enemies (trigger vs collision mismatch)",
         "Added both OnTriggerEnter2D and OnCollisionEnter2D handlers"],
        ["No visual feedback on hits (combat felt hollow)",
         "Added screen shake, muzzle flash, hit particles, damage overlay"],
        ["Placeholder circle sprites (game looked unfinished)",
         "Integrated Top-Down 2D RPG asset pack with proper sprites"],
        ["No game flow (just endless spawning)",
         "Built GameFlowController with Day/Night/Dawn phases and menu system"],
    ]
    t = Table(data, colWidths=[2.8*inch, 3.2*inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), HexColor("#ffffff")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 9.5),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#cccccc")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)
    story.append(sp(8))

    story.append(Paragraph("5.2 Playtesting Observations", h2))
    story.append(Paragraph(
        "Internal team testing over multiple sessions revealed several balance issues that were addressed:", body))
    items = [
        "Day phase initially set to 180 seconds felt too long for demo purposes. Shortened to 45 seconds "
        "to maintain pacing while still allowing meaningful exploration.",
        "Enemy health of 50 HP made Night 1 too easy. Scaling now adds 10 HP per wave within each night, "
        "plus base increases per night number.",
        "Ammo scarcity was a frequent complaint. Added more ammo pickups during day (4 per day phase) and "
        "made the ammo shop item affordable at 30 points.",
        "Players were confused about when night would start. Added a visible day countdown timer that "
        "shows remaining seconds.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    # --- 6. TECHNICAL NOTES ---
    story.append(PageBreak())
    story.append(Paragraph("6. Technical Notes", h1))

    story.append(Paragraph("6.1 Project Architecture", h2))
    story.append(Paragraph(
        "The project uses a modular architecture with clear separation of concerns:", body))

    data = [
        ["Directory", "Contents", "Key Scripts"],
        ["Scripts/Core", "Game management, camera, effects", "GameManager, GameFlowController, WaveSpawner, DayNightCycle"],
        ["Scripts/Player", "Player mechanics", "PlayerController, PlayerShooting, PlayerHealth, Bullet"],
        ["Scripts/Enemy", "Enemy behavior", "SimpleEnemyAI, EnemyHealth, EnemyAI"],
        ["Scripts/Systems", "Game systems", "PointsSystem, ResourceManager, ProgressionManager"],
        ["Scripts/UI", "User interface", "LiveHUD, GameUI, ShopUI"],
        ["Scripts/Narrative", "Story systems", "NarrativeManager, DialogueUI, EnvironmentalLore"],
        ["Scripts/Level", "Level elements", "LevelManager, MapZone, SpawnPoint, Obstacle"],
        ["Scripts/Data", "ScriptableObjects", "WeaponData, EnemyData, NightConfig, DifficultySettings"],
    ]
    t = Table(data, colWidths=[1.2*inch, 1.5*inch, 3.3*inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), HexColor("#ffffff")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#cccccc")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
    ]))
    story.append(t)
    story.append(sp(8))

    story.append(Paragraph("6.2 Key Unity Components", h2))
    items = [
        "<b>ScriptableObjects:</b> WeaponData, EnemyData, NightConfig, and DifficultySettings are all "
        "ScriptableObjects, enabling data-driven design where game parameters can be tuned without code changes.",
        "<b>Singleton Managers:</b> GameManager, PointsSystem, ResourceManager, WaveSpawner, and GameEffects "
        "use the singleton pattern for global access while maintaining clean initialization order.",
        "<b>Custom Editor Tools:</b> DeadlightSetupWizard and LevelEditorTools provide one-click scene setup "
        "and rapid level element placement. The Deadlight menu in Unity's toolbar gives quick access to all tools.",
        "<b>Event-Driven Architecture:</b> Systems communicate through C# events (OnGameStateChanged, "
        "OnHealthChanged, OnAmmoChanged, etc.) rather than direct references, keeping components decoupled.",
        "<b>Programmatic Scene Setup:</b> TestSceneSetup builds the entire playable scene at runtime, "
        "loading sprites from Resources and constructing all game objects, UI, and managers programmatically.",
    ]
    for item in items:
        story.append(Paragraph(item, bullet, bulletText="-"))

    story.append(Paragraph("6.3 Asset Integration", h2))
    story.append(Paragraph(
        "We integrated the free \"Top-Down 2D RPG Assets Pack\" from the Unity Asset Store, which provides "
        "character sprites (with 4-directional walk animations), NPC sprites used for enemies, environmental "
        "objects (houses, trees, rocks, crates), and ground tiles. The PlayerAnimator component switches "
        "between directional sprites based on the player's movement velocity, creating smooth 4-frame walk "
        "animations.", body))

    story.append(Paragraph("6.4 Team Contributions", h2))
    data = [
        ["Member", "Primary Responsibility"],
        ["Simranjeet Singh", "Player systems, core architecture, integration, project setup"],
        ["Abdelrahman El Banna", "Enemy AI systems, wave spawning, difficulty balancing"],
        ["Koroush Emari", "Level design, environment layout, map zones"],
        ["Ashraf Esam Mahdi", "UI systems, audio, narrative/dialogue implementation"],
    ]
    t = Table(data, colWidths=[2*inch, 4*inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HexColor("#1a1a2e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), HexColor("#ffffff")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 10),
        ("GRID", (0, 0), (-1, -1), 0.5, HexColor("#cccccc")),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)

    doc.build(story)
    print(f"Report generated: {OUTPUT}")

if __name__ == "__main__":
    build()
