# Sprite Assets Guide

This folder should contain all sprite assets for the Bomberman game. Below are the required sprites and their recommended specifications.

**📖 See VISUAL_STYLE_GUIDE.md in root folder for detailed visual style matching the classic Bomberman aesthetic!**

## Required Sprites

### Player
- **Player_Idle.png** - 32x32 pixels, chibi character with large head
  - White/cream colored body
  - Orange/red helmet
  - Cute, rounded proportions
- **Player_Walk_*.png** - 2-4 frame walk animation for each direction (up, down, left, right)
- **Player_Death.png** - Death animation frames

### Enemies
- **Enemy_Basic.png** - 32x32 pixels, green enemy (random wanderer)
  - Cute chibi style matching player
  - Distinct color (green)
  - Large expressive eyes
- **Enemy_Chaser.png** - 32x32 pixels, red/pink enemy (chases player)
  - Similar style to basic enemy
  - Different color (red/pink)
  - Aggressive appearance
- **Enemy_Death.png** - Enemy death animation

### Environment
- **Wall_Indestructible.png** - 32x32 pixels, solid gray stone blocks
  - Gray/silver color
  - Brick texture with depth/shading
  - Dark outline for clarity
- **Wall_Destructible.png** - 32x32 pixels, decorative purple/blue walls
  - Purple/blue ornate tile design
  - Central icon/symbol (like bomb icon)
  - Looks breakable (less solid than indestructible)
- **Floor_Tile.png** - 32x32 pixels, light gray concrete
  - Simple, subtle pattern
  - Doesn't distract from gameplay

### Bombs & Explosions
- **Bomb.png** - 32x32 pixels, black sphere with white fuse
  - Black/dark gray round bomb
  - White fuse on top
  - Can have 2-3 frames for pulsing animation
- **Explosion_Center.png** - 32x32 pixels, bright yellow/orange burst
  - Bright yellow core with white center
  - Circular burst pattern
  - Eye-catching and vibrant
- **Explosion_Horizontal.png** - 32x32 pixels, horizontal flame beam
  - Yellow-to-orange gradient flames
  - Extends left/right from center
  - Same brightness as center
- **Explosion_Vertical.png** - 32x32 pixels, vertical flame beam
  - Yellow-to-orange gradient flames
  - Extends up/down from center
  - Matches horizontal style

### Power-ups
All power-ups should be 32x32 pixels with bright, attractive colors:
- **PowerUp_Bomb.png** - Extra bomb power-up
  - Black bomb icon on bright green/blue background
  - Clear, readable icon
- **PowerUp_Range.png** - Explosion range power-up
  - Orange flame/explosion icon on yellow background
  - Shows expansion concept
- **PowerUp_Speed.png** - Speed boost power-up
  - Yellow lightning bolt or shoe icon
  - Gold/yellow color scheme
- **PowerUp_Health.png** - Health restore power-up
  - Red/pink heart icon
  - Warm, healing colors

## Visual Style Reference

**Match the classic Bomberman aesthetic:**
- ✅ Colorful, vibrant 16-bit pixel art
- ✅ Chibi character proportions (large head, small body)
- ✅ Clean outlines on all sprites
- ✅ High contrast colors for readability
- ✅ Decorative, ornate wall designs
- ✅ Bright, eye-catching explosions

See **VISUAL_STYLE_GUIDE.md** in the root folder for:
- Detailed color palettes
- Animation frame counts
- Character design specs
- Visual effects guidelines

## Import Settings (Unity)

For all sprites (to maintain pixel-perfect appearance):
1. Texture Type: Sprite (2D and UI)
2. Pixels Per Unit: 32
3. Filter Mode: **Point (no filter)** ← IMPORTANT for pixel art!
4. Compression: None or Low Quality
5. Max Size: 256 or higher

## Color Palette Suggestions

Use vibrant, saturated colors like classic Bomberman:

**Player:** Cream body (#FFF8DC), Orange helmet (#FF6B35)
**Enemies:** Green (#7FD957), Red (#FF6B6B), Blue (#4DABF7)
**Walls:** Gray (#95A5A6) for solid, Purple (#9B59B6) for breakable
**Explosions:** White center (#FFFFFF), Yellow (#FFF176), Orange (#FF9800)
**Power-ups:** Use bright, eye-catching colors on each

See **VISUAL_STYLE_GUIDE.md** for complete color specifications!

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
