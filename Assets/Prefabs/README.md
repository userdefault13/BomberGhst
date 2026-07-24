# Prefab Setup Guide

This folder contains all game object prefabs. Follow these instructions to create them.

## Player Prefab

1. Create empty GameObject named "Player"
2. Add components:
   - Sprite Renderer
   - Rigidbody2D (Body Type: Dynamic, Gravity Scale: 0, Freeze Rotation Z)
   - Box Collider 2D (Size: 0.8, 0.8)
   - PlayerController script
3. Set Layer to "Player"
4. Set Tag to "Player"

## Bomb Prefab

1. Create empty GameObject named "Bomb"
2. Add components:
   - Sprite Renderer
   - Circle Collider 2D (Radius: 0.4, Is Trigger: true)
   - Bomb script
3. Set Layer to "Bomb"
4. Set Tag to "Bomb"
5. Assign Explosion Prefab reference in Bomb script
6. Set Obstacle Layer to "Wall"

## Explosion Prefab

1. Create empty GameObject named "Explosion"
2. Add components:
   - Sprite Renderer
   - Circle Collider 2D (Radius: 0.4, Is Trigger: true)
   - Explosion script
3. Set Layer to "Explosion"
4. Set Tag to "Explosion"
5. Set duration to 0.5 seconds

## Enemy Prefab

1. Create empty GameObject named "Enemy"
2. Add components:
   - Sprite Renderer
   - Rigidbody2D (Body Type: Dynamic, Gravity Scale: 0, Freeze Rotation Z)
   - Circle Collider 2D (Radius: 0.4)
   - Enemy script
3. Set Layer to "Enemy"
4. Set Tag to "Enemy"
5. Configure enemy type (Random or Chaser)

## Wall Prefabs

### Indestructible Wall
1. Create empty GameObject named "Wall_Indestructible"
2. Add components:
   - Sprite Renderer
   - Box Collider 2D (Size: 1, 1)
   - IndestructibleWall script
3. Set Layer to "Wall"

### Destructible Wall
1. Create empty GameObject named "Wall_Destructible"
2. Add components:
   - Sprite Renderer
   - Box Collider 2D (Size: 1, 1)
   - DestructibleWall script
3. Set Layer to "Wall"
4. Set Tag to "DestructibleWall"
5. Assign power-up prefabs array
6. Set drop chance (0.3 = 30%)

## Power-Up Prefabs

For each power-up type (ExtraBomb, ExplosionRange, SpeedBoost, Health):

1. Create empty GameObject named "PowerUp_[Type]"
2. Add components:
   - Sprite Renderer
   - Circle Collider 2D (Radius: 0.4, Is Trigger: true)
   - PowerUp script
3. Set Layer to "PowerUp"
4. Set Tag to "PowerUp"
5. Set PowerUpType in script
6. Assign appropriate sprite

## Floor Tile Prefab

1. Create empty GameObject named "Floor"
2. Add components:
   - Sprite Renderer (Sorting Layer: Background)
3. No collider needed

## Destruction Effect Prefab (Optional)

1. Create empty GameObject named "DestructionEffect"
2. Add components:
   - Particle System (configure for explosion effect)
   - DestructionEffect script
3. Set lifetime to 0.5 seconds
