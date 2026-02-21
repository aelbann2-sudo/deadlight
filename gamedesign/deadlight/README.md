# Deadlight: Survival After Dark

A 2D top-down zombie survival game built in Unity for CS4483B - Game Design.

**Group 16**: Simranjeet Singh, Abdelrahman El Banna, Koroush Emari, Ashraf Esam Mahdi

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

## Team Responsibilities

### Simranjeet - Player Systems
**Files to extend**: `Assets/Scripts/Player/`
- `PlayerController.cs` - WASD movement, mouse aiming, sprinting
- `PlayerShooting.cs` - Weapon system, firing, reloading
- `PlayerHealth.cs` - Health management, damage, death
- `Bullet.cs` - Projectile behavior

**To add new weapons**:
1. Create a new `WeaponData` ScriptableObject in `Assets/ScriptableObjects/Weapons/`
2. Configure stats (damage, fire rate, magazine size, etc.)
3. Assign to PlayerShooting component or add to shop

### Abdelrahman - AI Systems
**Files to extend**: `Assets/Scripts/Enemy/`
- `EnemyAI.cs` - Pathfinding, state machine (Idle, Chase, Attack)
- `EnemyHealth.cs` - Damage, death, loot drops
- `EnemySpawner.cs` - Spawn point logic

**To add new enemy types**:
1. Create a new `EnemyData` ScriptableObject in `Assets/ScriptableObjects/Enemies/`
2. Create a prefab with EnemyAI + EnemyHealth components
3. Add to WaveManager's available enemies or NightConfig

**Enemy States**: Idle → Chase (when player detected) → Attack (when in range)

### Koroush - Level Design
**Key systems**:
- `EnemySpawner.cs` - Place these around the map for spawn points
- `Pickup.cs` - Place health/ammo/resource pickups
- `CameraController.cs` - Set map bounds

**To create a map**:
1. Create a new scene
2. Add `GameBootstrap` to an empty object
3. Place spawn points around the edges
4. Add colliders for walls/obstacles
5. Bake NavMesh (Window → AI → Navigation → Bake)

### Ashraf - UI & Audio
**Files to extend**: `Assets/Scripts/UI/`
- `HUDManager.cs` - In-game HUD (health, ammo, timer, wave)
- `MenuManager.cs` - Main menu, pause, game over screens
- `ShopUI.cs` - Between-night shop

**Files to extend**: `Assets/Scripts/Core/`
- `AudioManager.cs` - Music and sound effects

**To add UI elements**:
1. Create Canvas in scene if not exists
2. Add UI elements (Image, Text - TextMeshPro)
3. Link references in HUDManager/MenuManager inspector

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
