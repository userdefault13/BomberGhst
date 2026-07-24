# Sprite Assets Guide

This folder should contain all sprite assets for the Bomberman game. Below are the required sprites and their recommended specifications.

## Required Sprites

### Player
- **Player_Idle.png** - 32x32 pixels, player standing still
- **Player_Walk_*.png** - 4-frame walk animation for each direction (up, down, left, right)
- **Player_Death.png** - Death animation frames

### Enemies
- **Enemy_Basic.png** - 32x32 pixels, basic enemy sprite
- **Enemy_Chaser.png** - 32x32 pixels, chaser enemy sprite
- **Enemy_Death.png** - Enemy death animation

### Environment
- **Wall_Indestructible.png** - 32x32 pixels, solid gray/stone wall
- **Wall_Destructible.png** - 32x32 pixels, breakable wall (brown/wooden)
- **Floor_Tile.png** - 32x32 pixels, floor tile (grass or concrete)

### Bombs & Explosions
- **Bomb.png** - 32x32 pixels, bomb sprite (can be animated)
- **Explosion_Center.png** - 32x32 pixels, center of explosion
- **Explosion_Horizontal.png** - 32x32 pixels, horizontal explosion beam
- **Explosion_Vertical.png** - 32x32 pixels, vertical explosion beam

### Power-ups
- **PowerUp_Bomb.png** - 32x32 pixels, extra bomb power-up
- **PowerUp_Range.png** - 32x32 pixels, explosion range power-up
- **PowerUp_Speed.png** - 32x32 pixels, speed boost power-up
- **PowerUp_Health.png** - 32x32 pixels, health restore power-up

## Import Settings (Unity)

For all sprites:
1. Texture Type: Sprite (2D and UI)
2. Pixels Per Unit: 32
3. Filter Mode: Point (no filter) for pixel art style
4. Compression: None or Low Quality
5. Max Size: 256 or higher

## Placeholder Creation

If you don't have sprites yet, you can use Unity's built-in shapes:
1. Create simple colored squares using Unity's Sprite Creator
2. Use different colors for different objects:
   - Player: Blue
   - Enemy: Red
   - Indestructible Wall: Gray
   - Destructible Wall: Brown
   - Bomb: Black
   - Explosion: Orange/Yellow
   - Power-ups: Green, Purple, Yellow, Pink

## Animation

For animated sprites, create animations using Unity's Animation system:
- Player walking (4 directions)
- Bomb pulsing before explosion
- Explosion effect
- Enemy movement
