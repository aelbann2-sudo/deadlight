# Deadlight: Survival After Dark

A 2D top-down zombie survival game built in Unity for CS4483B - Game Design.

**Group 16**: Simranjeet Singh, Abdelrahman El Banna, Koroush Emari, Ashraf Esam Mahdi

---

## Game Features

### Controls
| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Aim |
| Left Click | Shoot |
| R | Reload |
| Space | Dodge Roll |
| Shift | Sprint |
| 1 / 2 | Switch Weapon Slot |
| Scroll Wheel | Switch Weapon |
| F | Interact / Loot Crate |
| Q | Throw Grenade |
| E | Throw Molotov |
| ESC | Pause Menu |

---

### Core Gameplay Loop
Survive 5 nights against increasingly difficult zombie waves:

1. **Day Phase** - Explore, loot supply crates, complete objectives, prepare for night
2. **Night Phase** - Survive waves of zombies, earn points from kills
3. **Dawn Phase** - Visit the shop to buy weapons, upgrades, and supplies
4. Repeat until Night 5 is survived (Victory) or you die (Game Over)

---

### Weapons System
8 weapons available, unlocked progressively through nights:

| Weapon | Damage | Fire Rate | Magazine | Unlock | Cost |
|--------|--------|-----------|----------|--------|------|
| Pistol | 15 | 0.3s | 12 | Start | Free |
| Shotgun | 8x8 pellets | 0.8s | 6 | Night 1 | 100 pts |
| SMG | 8 | 0.08s | 40 | Night 2 | 150 pts |
| Sniper Rifle | 75 | 1.2s | 5 | Night 2 | 250 pts |
| Assault Rifle | 12 | 0.12s | 30 | Night 3 | 200 pts |
| Grenade Launcher | 50 | 1.5s | 4 | Night 4 | 350 pts |
| Flamethrower | 5 (burn) | 0.05s | 100 | Night 4 | 400 pts |
| Railgun | 150 | 2.5s | 3 | Night 5 | 500 pts |

**Weapon HUD**: Bottom-right corner displays current weapon icon, name, damage, and fire rate.

---

### Armor System (PUBG-Style)
Armor absorbs damage before health is affected:

**Vests** (Body Protection):
| Tier | Damage Reduction | Durability | Shop Cost |
|------|------------------|------------|-----------|
| Level 1 | 30% | 80 HP | 80 pts |
| Level 2 | 40% | 150 HP | 180 pts |
| Level 3 | 55% | 230 HP | - (crate only) |

**Helmets** (Head Protection):
| Tier | Damage Reduction | Durability | Shop Cost |
|------|------------------|------------|-----------|
| Level 1 | 25% | 50 HP | 60 pts |
| Level 2 | 35% | 100 HP | 140 pts |
| Level 3 | 45% | 150 HP | - (crate only) |

- Armor bars display below health bar (blue for vest, gray for helmet)
- Armor breaks when durability reaches 0 (notification shown)
- Can be found in Rare/Legendary supply crates or purchased in the dawn shop

---

### Supply Crate System
Crates spawn during the Day Phase and can be looted by holding **F**.

**Crate Tiers**:
| Tier | Color | Glow | Loot Quality |
|------|-------|------|--------------|
| Common | Brown | Soft gold | Basic resources |
| Rare | Blue | Pulsing blue | Better resources + 15% powerup + 20% armor |
| Legendary | Gold | Bright pulse | Best resources + 40% powerup + 20% armor |

**Smart Loot System** - Crates prioritize what you need:
- Health < 30%? → Drops health
- Ammo < 20%? → Drops ammo
- Otherwise → Random weighted (ammo, health, points, powerups)

**Content Preview** - A floating icon above each crate shows what's inside before looting:
- Bullet icon (yellow) = Ammo
- Cross icon (green) = Health
- Coin icon (gold) = Points
- Star icon (purple) = Powerup
- Shield icon (blue) = Armor

---

### Helicopter Supply Drops
During **Night Phase**, helicopters may drop emergency supplies:

- **Max 2 drops per night** with 45-second cooldown between drops
- Radio transmission alert: *"Supply drop incoming!"*
- Helicopter flies across screen with spinning rotor
- Crate descends with parachute animation
- Lands near player position with screen shake
- Drop tier scales with current night (higher nights = better chance of Rare/Legendary)

---

### Dawn Shop
Between nights, spend points earned from kills:

**Weapons Tab**:
- Purchase new weapons (unlocked based on current night)
- Purchased weapons go to slot 2

**Upgrades Tab**:
- Damage+ (increase weapon damage)
- Fire Rate+ (faster shooting)
- Magazine+ (larger magazines)
- Max Health+ (increase health pool)
- Sprint Speed+ (move faster while sprinting)

**Supplies**:
- Health Kit (50 pts) - Full heal
- Ammo Refill (30 pts) - +60 reserve ammo
- Vest Lv1/Lv2 - Body armor
- Helmet Lv1/Lv2 - Head armor

---

### Difficulty Modes
Select difficulty before starting:

| Setting | Easy | Normal | Hard |
|---------|------|--------|------|
| Player Health | 150% | 100% | 75% |
| Damage Taken | 50% | 100% | 150% |
| Enemy Health | 50% | 100% | 125% |
| Enemy Damage | 50% | 100% | 125% |
| Enemy Speed | 75% | 100% | 110% |
| Spawn Rate | Slow | Normal | Fast |
| Resources | 2x | 1x | 0.75x |
| Score Multiplier | 0.75x | 1x | 1.5x |

---

### Maps
3 playable maps with unique layouts:
1. **Suburbs** - Residential streets, houses, yards
2. **Downtown** - Urban streets, buildings, alleys
3. **Military Base** - Fortified compound, barracks, watchtowers

Select map from the main menu before starting.

---

### Enemy Types
Various zombie types with different behaviors:
- **Basic Zombie** - Standard walker, moderate health/damage
- **Runner** - Fast movement, low health, aggressive
- **Exploder** - Bloated zombie, explodes on death dealing area damage
- **Tank** - Heavy zombie, high health, slow but devastating
- **Spitter** - Ranged attack, spits acid projectiles

**Night Mutations** - Each night may apply random mutations to all enemies (faster, stronger, more numerous, etc.)

---

### Powerups
Temporary buffs dropped from enemies or crates:
- **Damage Boost** - Increased weapon damage
- **Speed Boost** - Faster movement
- **Infinite Ammo** - No ammo consumption
- **Invincibility** - Cannot take damage

---

### Narrative System
Story unfolds through:
- **Intro Sequence** - Opening cinematic explaining the outbreak
- **Radio Transmissions** - Messages from survivors/military
- **Environmental Lore** - Pickups scattered around maps with story bits
- **Night Objectives** - Optional goals with bonus rewards

---

## Quick Start

### Prerequisites
1. **Install Unity Hub**: Download from https://unity.com/download
2. **Install Unity 2022.3 LTS**: Through Unity Hub, install Unity 2022.3.x with:
   - Windows Build Support (IL2CPP)
   - 2D sprite package (included by default)

### Opening the Project
1. Open Unity Hub
2. Click "Add" → "Add project from disk"
3. Navigate to this folder and select it
4. Unity will import all scripts and set up the project

### First Time Setup
After opening in Unity:
1. **Create Scenes**: Go to `File → New Scene`, save as `MainMenu.unity` and `Game.unity` in `Assets/Scenes/`
2. **Set Build Settings**: `File → Build Settings`, add both scenes (MainMenu first)
3. **Import TextMeshPro**: When prompted, click "Import TMP Essentials"

## Project Structure

```
Assets/
├── Scenes/              # Unity scenes (MainMenu, Game, GameOver)
├── Scripts/
│   ├── Core/            # Game management (GameManager, DayNightCycle, WaveManager)
│   ├── Player/          # Player systems (movement, shooting, health)
│   ├── Enemy/           # Enemy AI and spawning
│   ├── Systems/         # Game systems (resources, points, progression)
│   ├── UI/              # User interface (HUD, menus, shop)
│   └── Data/            # ScriptableObjects (weapons, enemies, configs)
├── Prefabs/             # Reusable game objects
├── ScriptableObjects/   # Data assets
├── Art/                 # Sprites and animations
└── Audio/               # Sound effects and music
```

## Core Systems Documentation

### GameManager (Singleton)
Controls game state flow: MainMenu → DayPhase → NightPhase → DawnPhase → repeat/GameOver/Victory

```csharp
// Access anywhere
GameManager.Instance.CurrentState    // Current game state
GameManager.Instance.CurrentNight    // Current night (1-5)
GameManager.Instance.CurrentDifficulty // Easy/Normal/Hard

// Events to subscribe to
GameManager.Instance.OnGameStateChanged += (GameState state) => { };
GameManager.Instance.OnNightChanged += (int night) => { };
```

### DayNightCycle
Manages the 3-minute day / 3.5-minute night cycle with lighting changes.

```csharp
// Access
var cycle = FindObjectOfType<DayNightCycle>();
cycle.IsDay           // true during day phase
cycle.TimeRemaining   // seconds left in current phase

// Events
cycle.OnDayStart += () => { };
cycle.OnNightStart += () => { };
cycle.OnTimeUpdate += (float remaining) => { };
```

### WaveManager
Spawns enemy waves during night phase.

```csharp
var waveManager = FindObjectOfType<WaveManager>();
waveManager.CurrentWave       // Current wave number
waveManager.EnemiesRemaining  // Enemies left alive

// Events
waveManager.OnWaveStarted += (int wave) => { };
waveManager.OnWaveCompleted += (int wave) => { };
waveManager.OnEnemyKilled += (int totalKilled) => { };
```

### PointsSystem (Singleton)
Tracks score and currency.

```csharp
PointsSystem.Instance.CurrentPoints  // Spendable points
PointsSystem.Instance.AddPoints(100, "Kill Bonus");
PointsSystem.Instance.SpendPoints(50, "Ammo");
PointsSystem.Instance.CanAfford(75);
```

### ResourceManager (Singleton)
Tracks crafting materials.

```csharp
ResourceManager.Instance.GetResource(ResourceType.Scrap);
ResourceManager.Instance.AddResource(ResourceType.Wood, 5);
ResourceManager.Instance.SpendResource(ResourceType.Chemicals, 2);
```

## Creating Prefabs

### Player Prefab
1. Create empty GameObject, name "Player"
2. Add components: `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`
3. Add scripts: `PlayerController`, `PlayerShooting`, `PlayerHealth`
4. Set tag to "Player"
5. Create child "FirePoint" for bullet spawn position
6. Save to `Assets/Prefabs/Player/`

### Enemy Prefab
1. Create empty GameObject, name "BasicZombie"
2. Add components: `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `NavMeshAgent`
3. Add scripts: `EnemyAI`, `EnemyHealth`
4. Set tag to "Enemy"
5. Configure NavMeshAgent for 2D (Agent Type, etc.)
6. Save to `Assets/Prefabs/Enemies/`

### Bullet Prefab
1. Create empty GameObject, name "Bullet"
2. Add components: `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D` (Is Trigger)
3. Add script: `Bullet`
4. Save to `Assets/Prefabs/Weapons/`

## Building for Windows

1. `File → Build Settings`
2. Select "Windows, Mac, Linux"
3. Click "Build"
4. Choose output folder
5. The build creates `YourGame.exe` + `YourGame_Data/` folder

## ScriptableObjects

### WeaponData
Right-click in Project → Create → Deadlight → Weapon Data
Configure: damage, fire rate, magazine size, reload time, spread, etc.

### EnemyData  
Right-click in Project → Create → Deadlight → Enemy Data
Configure: health, damage, speed, detection range, drop chance, etc.

### NightConfig
Right-click in Project → Create → Deadlight → Night Configuration
Configure: wave count, enemy count, difficulty multipliers, rewards, etc.

### DifficultySettings
Right-click in Project → Create → Deadlight → Difficulty Settings
Configure: player/enemy modifiers, resource rates, score multiplier, etc.

## Recommended Free Assets

Search Unity Asset Store for these free assets:
- **2D Characters**: "Pixel Adventure" or "Zombie Character Pack"
- **Tilemap**: "2D Roguelike Tileset" or "Top Down Environment"
- **UI**: "Simple UI" or "Minimalist UI Pack"
- **Audio**: "FREE Casual Game SFX" and "Horror Sound Effects"

## Common Issues

### NavMesh not working
- Ensure you have baked the NavMesh: Window → AI → Navigation → Bake
- NavMeshAgent needs to be configured for 2D (may need NavMeshPlus package)

### Scripts not compiling
- Check for missing `using` statements
- Ensure TextMeshPro is imported

### Player not moving
- Check Rigidbody2D is set to Dynamic, Gravity Scale = 0
- Ensure PlayerController is enabled

## Development Timeline

**Deliverable 2 (March 25)**: Working prototype with:
- Core survival mechanics
- Day/night cycle
- Wave spawning
- Resource/points system
- Basic UI
- 3 difficulty modes

**Deliverable 3 (Final)**: Complete game with:
- Mutation system
- Full crafting
- All enemy types
- Polish and audio
