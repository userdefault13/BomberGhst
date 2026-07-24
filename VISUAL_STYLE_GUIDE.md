# Visual Style Guide - Classic Bomberman Aesthetic

Based on the classic Bomberman visual style, here's what your game should look like.

## Overall Art Style

- **Pixel Art**: Retro 16-bit style with clean, readable sprites
- **Colorful & Vibrant**: Bright, saturated colors
- **Chibi Characters**: Large heads, small bodies (cute proportions)
- **Clear Silhouettes**: Easy to distinguish at a glance
- **Grid-Aligned**: All sprites fit perfectly in 32x32 pixel grid

## Color Palette

### Player Character
- **Primary Color**: White/cream body with blue/orange accents
- **Helmet**: Orange/red with bright highlights
- **Style**: Cute, rounded shapes with big expressive face

### Enemies
- **Variety**: Each enemy type has distinct color
  - Basic: Green body
  - Chaser: Red/pink body
  - Each with unique pattern/details
- **Eyes**: Large, expressive
- **Consistent Style**: Match player's chibi proportions

### Environment

#### Indestructible Walls
- **Color**: Gray/silver stone blocks
- **Pattern**: Decorative brick texture
- **Style**: Slightly 3D appearance with shading
- **Border**: Dark outline for clarity

#### Destructible Walls
- **Color**: Purple/blue with decorative pattern
- **Design**: Ornate tile design (like in screenshot)
- **Detail**: Central icon/symbol (bomb symbol in reference)
- **Breakable Look**: Less solid than indestructible walls

#### Floor
- **Color**: Light gray/white concrete
- **Texture**: Subtle grid lines or tile pattern
- **Style**: Simple, doesn't distract from gameplay

### Bombs & Explosions

#### Bomb
- **Color**: Black sphere
- **Details**: White fuse on top
- **Animation**: Pulse/flash before exploding
- **Size**: Takes up most of one grid cell

#### Explosion
- **Center**: Bright yellow/orange burst with white core
- **Rays**: Yellow-to-orange gradient flames
- **Shape**: Perfect cross pattern (4 directions)
- **Effect**: Bright, eye-catching, animated
- **Duration**: 0.5 seconds with fade-out

### Power-Ups

All power-ups should:
- Be **32x32 pixels**
- Have **bright, attractive colors**
- Include **icon that shows function**
- Have **subtle animation** (bobbing, rotating, or glowing)

#### Extra Bomb 💣
- **Color**: Black/dark gray
- **Icon**: Bomb symbol
- **Background**: Bright color field (green/blue)

#### Explosion Range 💥
- **Color**: Orange/red
- **Icon**: Flame or explosion symbol
- **Background**: Yellow/orange field

#### Speed Boost ⚡
- **Color**: Yellow/gold
- **Icon**: Lightning bolt or shoe
- **Background**: Bright yellow field

#### Health ❤️
- **Color**: Red/pink
- **Icon**: Heart symbol
- **Background**: Pink/red field

## UI Elements

### HUD (Top Bar)
- **Background**: Dark green with decorative border
- **Layout**: Lives | Bombs | Time | Score
- **Icons**: Large, colorful, easy to read
- **Font**: Bold, pixel-art style
- **Colors**: White text on dark background

### Character Portraits (Side Panels)
- **Style**: Large character faces/artwork
- **Position**: Left and right borders
- **Design**: Colorful artwork of characters
- **Purpose**: Visual flair and branding

## Sprite Specifications

### All Sprites Should Be:
- **32x32 pixels** for characters, walls, items
- **PNG format** with transparency
- **Pixel-perfect** - no anti-aliasing on edges
- **High contrast** - clear outlines
- **Readable** at small size

### Animation Guidelines

#### Player Animation
- **Idle**: 1-2 frames (slight breathing)
- **Walk**: 2-4 frames per direction
- **4 Directions**: Up, Down, Left, Right
- **Frame Rate**: 8-12 FPS

#### Enemy Animation
- **Walk**: 2 frames per direction
- **Death**: 3-5 frame explosion/disappear

#### Bomb Animation
- **Idle**: 3-4 frames (pulse effect)
- **Speed**: Gets faster before exploding
- **Final Frame**: Bright flash before explosion

#### Explosion Animation
- **Frames**: 4-6 frames
- **Start**: Bright burst
- **Middle**: Full cross spread
- **End**: Fade out
- **Frame Rate**: 12-15 FPS

## Color Schemes

### Suggested Palette

**Player:**
- Body: #FFF8DC (cream)
- Helmet: #FF6B35 (orange-red)
- Accents: #4A90E2 (blue)
- Outline: #2C3E50 (dark blue-gray)

**Enemies:**
- Green: #7FD957, #5A9F3E
- Red: #FF6B6B, #C92A2A
- Blue: #4DABF7, #1971C2
- Outline: #2C3E50

**Walls:**
- Indestructible: #95A5A6, #7F8C8D, #34495E
- Destructible: #9B59B6, #8E44AD, #6C3483
- Floor: #ECF0F1, #BDC3C7

**Explosions:**
- Core: #FFFFFF (white)
- Inner: #FFF176 (bright yellow)
- Outer: #FF9800 (orange)
- Edge: #FF5722 (red-orange)

**Power-ups:**
- Bomb: #2C3E50 on #7FD957
- Range: #FF6B35 on #FFF176
- Speed: #FFD700 on #FFF9C4
- Health: #E74C3C on #FFB3BA

## Creating Sprites

### Method 1: Pixel Art Software
Use tools like:
- **Aseprite** (paid, best for pixel art)
- **Piskel** (free, browser-based)
- **GIMP** (free, set up for pixel art)
- **Pixilart** (free, online)

### Method 2: Quick Placeholders
For prototyping in Unity:
1. Create 32x32 colored squares
2. Use the colors suggested above
3. Add simple shapes/icons for detail
4. Replace with proper art later

### Method 3: Unity Sprite Creator
1. Assets → Create → Sprites → Square
2. Create Material with colored texture
3. Assign to sprite
4. Duplicate and recolor for different objects

## Implementation in Unity

### Import Settings
For each sprite:
```
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 32
Filter Mode: Point (no filter)
Compression: None
Max Size: 256 or higher
```

### Sorting Layers (Bottom to Top)
1. Background (-10)
2. Floor (0)
3. PowerUps (1)
4. Walls (2)
5. Bombs (3)
6. Player (5)
7. Enemies (4)
8. Explosions (6)
9. UI (10)

### Material Settings
- Use **Sprites/Default** shader
- For glowing effects: Add emission
- For transparency: Use alpha channel

## Visual Effects

### Screen Shake
- On explosion
- Intensity: 0.1-0.3 units
- Duration: 0.2 seconds

### Flash Effects
- Player hit: White flash for 1 frame
- Enemy death: Sprite blinks before disappearing

### Particle Effects
- Wall destruction: Small debris particles
- Explosion: Fire particles (optional)
- Power-up: Sparkle effect

## Reference
The screenshot shows classic Super Bomberman style with:
- ✅ Clean, colorful pixel art
- ✅ Decorative purple/blue destructible walls
- ✅ Gray indestructible walls
- ✅ Chibi character proportions
- ✅ Bright cross-shaped explosions
- ✅ Green HUD bar at top
- ✅ Character artwork on sides
- ✅ Clear visual hierarchy

Aim for this level of visual polish and readability!
