# Scene Setup Guide

Follow these steps to set up the MainGame scene from scratch.

## Step 1: Create Game Manager

1. Create empty GameObject (Right-click in Hierarchy > Create Empty)
2. Rename to "GameManager"
3. Add Component: `GameManager` script
4. This object will persist between scenes

## Step 2: Create UI Canvas

1. Right-click in Hierarchy > UI > Canvas
2. Rename to "GameUI"
3. Set Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920x1080

### Create HUD Elements

Inside the Canvas, create:

**Score Text:**
- Right-click Canvas > UI > Text - TextMeshPro
- Name: "ScoreText"
- Position: Top-left (Anchor: top-left)
- Text: "Score: 0"
- Font Size: 36
- Color: White

**Lives Text:**
- Right-click Canvas > UI > Text - TextMeshPro
- Name: "LivesText"
- Position: Top-right (Anchor: top-right)
- Text: "Lives: 3"
- Font Size: 36
- Color: Red

**Level Text:**
- Right-click Canvas > UI > Text - TextMeshPro
- Name: "LevelText"
- Position: Top-center (Anchor: top-center)
- Text: "Level: 1"
- Font Size: 36
- Color: Yellow

### Create Game Over Panel

1. Right-click Canvas > UI > Panel
2. Name: "GameOverPanel"
3. Set to inactive by default
4. Add child Text element: "Game Over!"
5. Add child Text element for score display

### Create Pause Menu Panel

1. Right-click Canvas > UI > Panel
2. Name: "PauseMenuPanel"
3. Set to inactive by default
4. Add child Text element: "PAUSED"
5. Add buttons for Resume and Quit

### Add UI Manager to Canvas

1. Select Canvas GameObject
2. Add Component: `UIManager` script
3. Assign all UI references in Inspector:
   - Score Text
   - Lives Text
   - Level Text
   - Game Over Panel
   - Game Over Score Text
   - Pause Menu Panel

## Step 3: Create Level Generator

1. Create empty GameObject
2. Rename to "LevelGenerator"
3. Add Component: `LevelGenerator` script
4. Configure settings:
   - Width: 13
   - Height: 11
   - Destructible Wall Density: 0.7
   - Enemy Count: 3
5. Assign prefab references (after creating prefabs):
   - Indestructible Wall Prefab
   - Destructible Wall Prefab
   - Player Prefab
   - Enemy Prefab
   - Floor Prefab

## Step 4: Configure Main Camera

1. Select Main Camera
2. Set values:
   - Projection: Orthographic
   - Size: 6
   - Position: (6, 5, -10)
   - Background: Solid Color (your choice, e.g., dark blue)
3. Optional: Add `CameraController` script for camera following

## Step 5: Configure Lighting

For 2D games:
1. Window > Rendering > Lighting
2. Disable "Auto Generate"
3. Set Ambient Source to "Color"
4. Set Ambient Color to white or bright gray

## Step 6: Test the Scene

1. Make sure all prefabs are created and assigned
2. Press Play button
3. Player should spawn at (1, 1)
4. Enemies should spawn randomly
5. Walls should generate in a grid pattern
6. UI should display current stats

## Common Issues

**Nothing spawns:**
- Check if prefabs are assigned in LevelGenerator
- Make sure LevelGenerator script is active

**Player can't move:**
- Check if PlayerController script is on Player prefab
- Verify Rigidbody2D is set correctly (Dynamic, no gravity)

**Collisions don't work:**
- Configure collision matrix in Physics 2D settings
- Ensure colliders are on all GameObjects
- Check layer assignments

**UI doesn't update:**
- Verify UIManager references are assigned
- Check if GameManager has UIManager reference
- Look for errors in Console

## Next Steps

After scene setup:
1. Create all prefabs (see Assets/Prefabs/README.md)
2. Import or create sprites (see Assets/Sprites/README.md)
3. Configure project settings (see Assets/ProjectSettings.md)
4. Test gameplay and adjust values as needed
