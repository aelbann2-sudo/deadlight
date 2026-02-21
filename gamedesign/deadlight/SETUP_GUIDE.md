# Deadlight Setup Guide

This guide walks you through setting up the Unity project from scratch.

## Step 1: Install Unity

1. Download **Unity Hub** from https://unity.com/download
2. Install Unity Hub and sign in with a Unity account (free)
3. In Unity Hub, go to **Installs** → **Install Editor**
4. Select **Unity 2022.3 LTS** (any 2022.3.x version)
5. In modules, ensure these are checked:
   - ✅ Windows Build Support (IL2CPP)
   - ✅ Documentation (optional but helpful)
6. Click Install and wait for completion

## Step 2: Create Unity Project

1. Open Unity Hub
2. Go to **Projects** → **New Project**
3. Select **2D (Built-in Render Pipeline)** template
4. Name it "Deadlight" or use existing folder
5. If using existing scripts folder:
   - Create project in a NEW location first
   - Then copy the `Assets/Scripts` folder from this repo into your new project's Assets folder

## Step 3: Import Required Packages

After project opens:

### TextMeshPro (Required for UI)
1. Unity will prompt to import TMP Essentials - click **Import TMP Essentials**
2. If not prompted: Window → TextMeshPro → Import TMP Essential Resources

### 2D Lighting (For Day/Night Cycle)
1. Window → Package Manager
2. Search for "Universal RP" 
3. Install Universal RP
4. Or use the simpler approach: Just adjust the Global Light 2D color in scripts

### AI Navigation (For Enemy Pathfinding)
1. Window → Package Manager
2. Click "+" → Add package by name
3. Enter: `com.unity.ai.navigation`
4. Click Add

For 2D NavMesh (recommended), install NavMeshPlus:
1. Window → Package Manager → "+" → Add package from git URL
2. Enter: `https://github.com/h8man/NavMeshPlus.git`

## Step 4: Project Setup

### Create Scenes
1. File → New Scene → Save as `Assets/Scenes/MainMenu.unity`
2. File → New Scene → Save as `Assets/Scenes/Game.unity`
3. File → New Scene → Save as `Assets/Scenes/GameOver.unity`

### Build Settings
1. File → Build Settings
2. Add scenes in order:
   - MainMenu (index 0)
   - Game (index 1)
   - GameOver (index 2)
3. Set Platform to "Windows, Mac, Linux"

### Player Settings
1. Edit → Project Settings → Player
2. Set Company Name and Product Name: "Deadlight"
3. Set Default Icon (optional)

## Step 5: Create Basic Prefabs

### Player Prefab
1. In Hierarchy, right-click → Create Empty → name "Player"
2. Add components:
   - Sprite Renderer (assign any placeholder sprite)
   - Rigidbody 2D (set Gravity Scale = 0, Freeze Rotation Z)
   - Circle Collider 2D
3. Add scripts (drag from Project):
   - PlayerController
   - PlayerShooting
   - PlayerHealth
4. Create child object "FirePoint", position at (0, 0.5, 0)
5. Set Tag to "Player"
6. Drag to `Assets/Prefabs/Player/` to create prefab

### Bullet Prefab
1. Create Empty → name "Bullet"
2. Add components:
   - Sprite Renderer (small circle/square)
   - Rigidbody 2D (Gravity Scale = 0)
   - Circle Collider 2D (Is Trigger = true)
3. Add script: Bullet
4. Drag to `Assets/Prefabs/Weapons/`

### Enemy Prefab
1. Create Empty → name "BasicZombie"
2. Add components:
   - Sprite Renderer
   - Rigidbody 2D (Gravity Scale = 0, Freeze Rotation Z)
   - Circle Collider 2D
   - Nav Mesh Agent (configure for 2D)
3. Add scripts:
   - EnemyAI
   - EnemyHealth
4. Set Tag to "Enemy"
5. Drag to `Assets/Prefabs/Enemies/`

## Step 6: Setup Game Scene

1. Open `Assets/Scenes/Game.unity`
2. Create Empty → name "GameBootstrap", add GameBootstrap script
3. Create Empty → name "Managers" with children:
   - GameManager object with GameManager script
   - DayNightCycle object with DayNightCycle script
   - WaveManager object with WaveManager script
4. Add Camera:
   - Select Main Camera
   - Add CameraController script
5. Create spawn points around the map edges
6. Assign prefabs to GameBootstrap inspector

## Step 7: Setup Main Menu Scene

1. Open `Assets/Scenes/MainMenu.unity`
2. Right-click Hierarchy → UI → Canvas
3. Add UI elements:
   - Panel (background)
   - TextMeshPro - Text for title
   - Buttons for Play, Settings, Quit
4. Create Empty "MenuManager", add MenuManager script
5. Link UI elements in inspector

## Step 8: Create ScriptableObjects

### Difficulty Settings
1. Right-click in `Assets/ScriptableObjects/` → Create → Deadlight → Difficulty Settings
2. Create three: EasySettings, NormalSettings, HardSettings
3. Configure values per the DifficultySettings.cs defaults

### Weapon Data
1. Right-click → Create → Deadlight → Weapon Data
2. Create: Pistol, Shotgun, AssaultRifle
3. Configure stats

### Night Configs
1. Right-click → Create → Deadlight → Night Configuration
2. Create: Night_1, Night_2, Night_3, Night_4, Night_5
3. Configure wave counts, enemy multipliers, etc.

## Step 9: NavMesh Setup (for Enemy AI)

### Option A: NavMeshPlus (Recommended for 2D)
1. Create Empty → name "NavMesh"
2. Add component: NavMeshSurface2D
3. Configure settings for 2D
4. Click "Bake"

### Option B: Standard NavMesh
1. Window → AI → Navigation
2. Mark floor/walkable areas as Navigation Static
3. Bake tab → Bake

## Step 10: Test the Game

1. Open Game scene
2. Press Play
3. Verify:
   - Player moves with WASD
   - Player aims with mouse
   - Day/Night cycle timer works
   - Enemies spawn during night (if configured)

## Common Setup Issues

### "Script not found" errors
- Make sure script file names match class names exactly
- Check namespace declarations match folder structure

### NavMesh agent won't move
- Ensure NavMesh is baked
- Check agent is on NavMesh (select agent, check "On NavMesh" in debug)
- For 2D, use NavMeshPlus package

### UI not responding
- Ensure Canvas has Event System child object
- Check button onClick events are connected
- Verify Canvas is in Screen Space - Overlay mode

### Lighting not changing
- Add Global Light 2D to scene
- Assign it to DayNightCycle's globalLight field
- Ensure URP is configured (or remove Light2D references if using Built-in RP)

## Build Checklist

Before building:
- [ ] All scenes added to Build Settings
- [ ] Prefabs have all required components
- [ ] ScriptableObjects assigned in inspectors
- [ ] Test in Play mode first
- [ ] Remove debug logs (or use Debug.Log with #if UNITY_EDITOR)

To build:
1. File → Build Settings
2. Select target platform (Windows)
3. Click Build
4. Choose output folder
5. Test the .exe runs correctly
