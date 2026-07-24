# Troubleshooting Guide

Common issues and solutions for BomberGhst.

## Compilation Errors

### "Type or namespace could not be found"

**Problem:** Missing using statements or Unity packages

**Solutions:**
1. Add required using statements at top of script:
   ```csharp
   using UnityEngine;
   using UnityEngine.UI;
   using TMPro; // For TextMeshPro
   using System.Collections;
   ```

2. Install TextMeshPro:
   - Window > TextMeshPro > Import TMP Essential Resources

3. Check Unity version (needs 2021.3+)

### "Script cannot be loaded"

**Problem:** Compilation errors in scripts

**Solutions:**
1. Open Console window (Ctrl/Cmd + Shift + C)
2. Fix all red error messages
3. Common fixes:
   - Check for typos in variable/class names
   - Ensure all brackets { } are closed
   - Check semicolons at end of statements

## Runtime Errors

### NullReferenceException

**Problem:** Script trying to access object that doesn't exist

**Common Causes:**

1. **Prefabs not assigned:**
   - Select LevelGenerator in Hierarchy
   - Check Inspector - assign all prefab slots
   - Drag prefabs from Project to Inspector fields

2. **UI references not assigned:**
   - Select Canvas or GameManager
   - Assign all Text/Panel references in UIManager

3. **Missing components:**
   - Make sure prefabs have required components
   - Player needs: SpriteRenderer, Rigidbody2D, Collider2D, PlayerController
   - Check each prefab follows guides in Assets/Prefabs/README.md

### "Object reference not set to an instance of an object"

**Solutions:**
1. Check all public/serialized fields are assigned in Inspector
2. Use null checks in code:
   ```csharp
   if (player != null)
   {
       player.TakeDamage(1);
   }
   ```

## Gameplay Issues

### Player Won't Move

**Possible causes:**

1. **No input:**
   - Edit > Project Settings > Input Manager
   - Check "Horizontal" and "Vertical" axes exist
   - Test with both WASD and arrow keys

2. **Rigidbody2D settings:**
   - Body Type: Dynamic
   - Gravity Scale: 0
   - Constraints: Freeze Rotation Z

3. **Colliding with something:**
   - Player spawning inside a wall
   - Check collision layers in Physics2D settings

4. **Script not enabled:**
   - Select Player in Hierarchy
   - Check PlayerController script has checkmark

### Bombs Won't Place

**Possible causes:**

1. **Bomb prefab not assigned:**
   - Select Player prefab
   - Assign Bomb Prefab in PlayerController script

2. **Already at max bombs:**
   - Default is 1 bomb at a time
   - Wait for bomb to explode

3. **Wrong key:**
   - Default is Spacebar
   - Check bombKey setting in PlayerController

### Bombs Don't Explode

**Possible causes:**

1. **Explosion prefab not assigned:**
   - Select Bomb prefab
   - Assign Explosion Prefab in Bomb script

2. **Explosion Delay too long:**
   - Check explosionDelay in Bomb script
   - Default is 3 seconds

3. **Coroutine issue:**
   - Bomb must be active GameObject
   - Check console for errors

### Explosions Don't Damage Enemies/Player

**Possible causes:**

1. **Layer collision settings:**
   - Edit > Project Settings > Physics 2D
   - Check collision matrix
   - Explosions need triggers, not collisions

2. **Collider not set as Trigger:**
   - Explosion prefab needs Circle Collider 2D
   - "Is Trigger" must be checked

3. **Script issue:**
   - Check Explosion.cs Initialize() is called
   - Verify Bomb.cs calls explosionScript.Initialize()

### Enemies Don't Move

**Possible causes:**

1. **Rigidbody2D settings:**
   - Body Type: Dynamic
   - Gravity Scale: 0
   - Constraints: Freeze Rotation Z

2. **Script not enabled:**
   - Check Enemy script has checkmark

3. **Spawned in wall:**
   - Check LevelGenerator spawn logic
   - Enemies need clear space to spawn

4. **Collision blocking:**
   - Enemies colliding with walls/each other
   - Check Physics 2D collision matrix

### Power-Ups Don't Work

**Possible causes:**

1. **Collider not Trigger:**
   - PowerUp needs Circle Collider 2D
   - "Is Trigger" must be checked

2. **PowerUpType not set:**
   - Select PowerUp prefab
   - Set PowerUpType in Inspector

3. **Script issue:**
   - Verify OnTriggerEnter2D exists
   - Check Player has a collider (not trigger)

## Visual Issues

### Sprites Not Showing

**Possible causes:**

1. **No sprite assigned:**
   - Select GameObject
   - Assign sprite in SpriteRenderer component

2. **Sprite behind camera:**
   - Camera Z position should be -10
   - Sprites should be at Z = 0

3. **Wrong sorting layer:**
   - Check Sorting Layer in SpriteRenderer
   - Player/Enemies should be above floor/walls

4. **Scale too small:**
   - Check Transform scale is (1, 1, 1)

### UI Not Visible

**Possible causes:**

1. **Canvas render mode:**
   - Canvas > Render Mode: Screen Space - Overlay

2. **Text color:**
   - Check text color isn't same as background

3. **Text size:**
   - Increase font size
   - Check auto-sizing settings

4. **Camera settings:**
   - UI Camera should be "Main Camera" or none for overlay

## Performance Issues

### Game Running Slow

**Solutions:**

1. **Reduce enemy count:**
   - Lower enemyCount in LevelGenerator

2. **Reduce wall density:**
   - Lower destructibleWallDensity (try 0.5)

3. **Disable debug features:**
   - Remove GameDebugger script if attached
   - Disable Gizmos in Game view

4. **Optimize collision:**
   - Use box/circle colliders (not polygon)
   - Reduce physics update rate if needed

### Frame Rate Drops During Explosions

**Solutions:**

1. **Limit particle effects:**
   - Reduce particle count
   - Use simpler destruction effects

2. **Optimize explosion raycasts:**
   - Bomb.cs uses raycasts for each direction
   - Already optimized, but can reduce range

## Build Issues

### Build Fails

**Solutions:**

1. **Fix all compilation errors first**
2. **Check scenes in build:**
   - File > Build Settings
   - Add MainGame scene to build

3. **Platform settings:**
   - Select correct platform
   - Switch platform if needed

## Still Having Issues?

### Debug Tools

1. **Enable Debug Mode:**
   - Add GameDebugger script to any GameObject
   - Press F1-F5 in Play mode for debug info

2. **Console Window:**
   - Window > General > Console (Ctrl/Cmd + Shift + C)
   - Enable "Error Pause" to stop on errors
   - Click error messages for details

3. **Inspector Debug Mode:**
   - Inspector > ⋮ (top right) > Debug
   - See all variables, even private ones

### Getting Help

1. Check console for specific error messages
2. Read relevant README files in Assets folders
3. Verify all setup steps were completed
4. Compare your setup to guides

### Common Unity Issues

**"Assembly has reference to non-existent assembly":**
- Delete Library folder
- Reopen project

**"Scripts are not reloading":**
- Assets > Refresh (Ctrl/Cmd + R)
- Reimport All

**"Prefab instance is broken":**
- Delete instance from scene
- Drag fresh copy from Prefabs folder

## Prevention Tips

1. **Save frequently:** Ctrl/Cmd + S
2. **Test often:** Press Play after each change
3. **Use version control:** Git/GitHub
4. **Keep backups:** Copy working versions
5. **Read console:** Fix warnings before they become errors

---

If you encounter an issue not listed here, please check the console error message and search Unity documentation or forums.
