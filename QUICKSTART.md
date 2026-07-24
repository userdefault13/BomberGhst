# Quick Start Guide

Get BomberGhst up and running in 15 minutes!

## Step 1: Open Project (2 min)

1. Clone repository
2. Open in Unity Hub
3. Use Unity 2021.3 LTS or newer

## Step 2: Create Placeholder Sprites (5 min)

Since you might not have sprites yet, create colored squares:

1. In Unity: Assets > Create > Sprites > Square
2. Create 9 sprites with different names:
   - `Sprite_Player` (Blue)
   - `Sprite_Enemy` (Red)
   - `Sprite_WallIndestructible` (Gray)
   - `Sprite_WallDestructible` (Brown)
   - `Sprite_Bomb` (Black)
   - `Sprite_Explosion` (Orange)
   - `Sprite_PowerUpBomb` (Green)
   - `Sprite_PowerUpRange` (Purple)
   - `Sprite_PowerUpSpeed` (Yellow)
   - `Sprite_Floor` (Light Gray)

3. To color them:
   - Import Settings > Advanced > Read/Write Enabled
   - Use materials with different colors, or
   - Create actual colored square images (32x32 pixels)

**OR use this faster method:**
- Use TextMeshPro icons as temporary sprites
- Just use Unity primitives with SpriteRenderer

## Step 3: Create Prefabs (5 min)

### Quick Prefab Creation Method:

**Player Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Player"
3. Add: Rigidbody2D (Dynamic, Gravity=0, Freeze Rotation Z)
4. Add: Box Collider 2D (size 0.8x0.8)
5. Add: PlayerController script
6. Set Layer: Player (create if needed)
7. Drag to Prefabs folder
```

**Bomb Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Bomb"
3. Add: Circle Collider 2D (radius 0.4, Is Trigger)
4. Add: Bomb script
5. Drag to Prefabs folder
```

**Explosion Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Explosion"
3. Add: Circle Collider 2D (radius 0.4, Is Trigger)
4. Add: Explosion script
5. Set duration to 0.5 in script
6. Drag to Prefabs folder
```

**Enemy Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Enemy"
3. Add: Rigidbody2D (Dynamic, Gravity=0, Freeze Rotation Z)
4. Add: Circle Collider 2D (radius 0.4)
5. Add: Enemy script
6. Drag to Prefabs folder
```

**Wall Indestructible Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Wall_Indestructible"
3. Add: Box Collider 2D (size 1x1)
4. Add: IndestructibleWall script
5. Set Layer: Wall
6. Drag to Prefabs folder
```

**Wall Destructible Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Wall_Destructible"
3. Add: Box Collider 2D (size 1x1)
4. Add: DestructibleWall script
5. Set Layer: Wall
6. Drag to Prefabs folder
```

**Floor Prefab:**
```
1. GameObject > 2D Object > Sprite
2. Name: "Floor"
3. Set Sorting Layer: Background (create if needed)
4. Drag to Prefabs folder
```

**Power-Up Prefabs (create 4):**
```
For each: ExtraBomb, ExplosionRange, SpeedBoost, Health:
1. GameObject > 2D Object > Sprite
2. Name: "PowerUp_[Type]"
3. Add: Circle Collider 2D (radius 0.4, Is Trigger)
4. Add: PowerUp script
5. Set PowerUpType in Inspector
6. Drag to Prefabs folder
```

## Step 4: Set Up Scene (2 min)

1. Open `Assets/Scenes/MainGame.unity`
2. Create empty GameObject "GameManager"
   - Add GameManager script
3. Create empty GameObject "LevelGenerator"
   - Add LevelGenerator script
   - Assign all prefab references
4. Select Main Camera
   - Set Orthographic, Size 6
   - Position (6, 5, -10)

## Step 5: Create Simple UI (1 min)

1. GameObject > UI > Canvas
2. GameObject > UI > Text - TextMeshPro (3 times)
   - Name: ScoreText, LivesText, LevelText
   - Position in corners/top
3. Select Canvas, Add UIManager script
4. Assign text references

## Step 6: Configure Physics (30 sec)

1. Edit > Project Settings > Physics 2D
2. Uncheck collisions between:
   - Player and Bomb
   - Player and PowerUp
   - Enemy and Bomb
   - Explosion and everything (make it trigger only)

## Step 7: Play! (30 sec)

1. Press Play button
2. Use WASD/Arrows to move
3. Press Space to place bombs
4. Test gameplay

## Common First-Time Issues

**"Script not found":**
- Make sure all .cs files are in Assets/Scripts
- Wait for Unity to compile
- Check console for compile errors

**"NullReferenceException":**
- Assign all prefab references in LevelGenerator
- Assign UI references in UIManager

**"Nothing spawns":**
- Check LevelGenerator has prefabs assigned
- Make sure scripts are enabled
- Look in Scene hierarchy - objects might be spawning at (0,0,0)

**"Can't move player":**
- Rigidbody2D must be Dynamic
- Gravity Scale = 0
- Check if player spawned (look in Hierarchy)

## Next Steps

Now that it's working:
1. Replace placeholder sprites with actual art
2. Add animations
3. Add sound effects
4. Tune gameplay values
5. Create more levels

## Need Help?

- Check console for errors (Ctrl/Cmd + Shift + C)
- Read full documentation in Assets folders
- Attach GameDebugger script to any GameObject for debug info (F1-F5 keys)

**Enjoy your game! 💣**
