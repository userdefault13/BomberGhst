# Game Design Document

## Core Gameplay Loop

1. Player spawns in level
2. Navigate maze avoiding enemies
3. Place bombs to destroy walls
4. Collect power-ups from destroyed walls
5. Defeat all enemies to complete level
6. Progress to next level with increased difficulty

## Game Mechanics

### Movement System
- **Grid-based movement**: 1 unit per grid cell
- **Smooth interpolation**: Visual smoothness while maintaining grid logic
- **8-directional collision**: Can slide along walls
- **Speed**: Base 5 units/second

### Bomb System
- **Placement**: Press Space to place bomb at current grid position
- **Timer**: 3 seconds before explosion
- **Limit**: Can only place 1 bomb at a time (increases with power-ups)
- **Chain reactions**: Bombs can trigger other bombs
- **Grid snapping**: Always placed at grid coordinates

### Explosion System
- **Range**: Default 1 tile, increases with power-ups (max recommended: 5)
- **Spread pattern**: Cross-shaped (up, down, left, right)
- **Damage**: Instant kill to enemies, 1 heart damage to player
- **Wall destruction**: Stops at indestructible walls, destroys destructible walls
- **Duration**: 0.5 seconds

### Enemy AI

#### Random Enemy (Basic)
- Moves randomly in valid directions
- Changes direction when hitting obstacle
- Changes direction every 2 seconds
- Speed: 2 units/second
- Score value: 100 points

#### Chaser Enemy (Advanced)
- Wanders randomly when player far away
- Chases player when within 5 unit range
- Speed: 2 units/second (same as basic)
- Score value: 200 points (optional, can increase)
- More challenging due to pursuit behavior

### Power-Up System

#### Extra Bomb 💣
- Effect: +1 max bomb capacity
- Stacks: Unlimited
- Recommended cap: 5 bombs

#### Explosion Range 💥
- Effect: +1 explosion range in all directions
- Stacks: Unlimited
- Recommended cap: 5 range

#### Speed Boost ⚡
- Effect: +1 movement speed
- Stacks: Unlimited
- Recommended cap: 8-9 (too fast becomes hard to control)

#### Health ❤️
- Effect: Restore 1 heart
- Maximum health: 3 hearts
- Most valuable in later levels

### Level Design

#### Dimensions
- Width: 13 tiles
- Height: 11 tiles
- Total: 143 tiles

#### Wall Pattern
- Border: All indestructible walls
- Grid: Indestructible walls at every even position (2,2), (2,4), etc.
- Filler: 70% destructible walls in remaining spaces
- Safe zones: 3x3 area around player spawn (1,1) kept clear

#### Spawning
- Player: Always at (1, 1) bottom-left corner
- Enemies: Random positions, not near player spawn
- Power-ups: 30% drop rate from destructible walls

## Progression System

### Score System
- Enemy kill: 100 points (basic), 200 points (chaser)
- Level completion: 1000 bonus points
- Power-up collection: 50 points (optional)

### Lives System
- Start: 3 lives
- Game over: When lives reach 0
- Extra lives: Can be earned every 10,000 points (optional)

### Difficulty Progression
Increase difficulty by level:

**Level 1-3:**
- Enemy count: 3
- Enemy type: All random
- Wall density: 70%

**Level 4-6:**
- Enemy count: 4
- Enemy type: 75% random, 25% chaser
- Wall density: 75%

**Level 7-9:**
- Enemy count: 5
- Enemy type: 50% random, 50% chaser
- Wall density: 75%

**Level 10+:**
- Enemy count: 6
- Enemy type: 25% random, 75% chaser
- Wall density: 80%
- Enemy speed: +0.5 every 5 levels

## Balance Considerations

### Player vs Enemy Speed
- Player: 5 units/sec (with power-ups up to 9)
- Enemy: 2 units/sec (increased in later levels)
- Player should always be able to outrun enemies when needed

### Bomb Timer vs Movement
- Explosion delay: 3 seconds
- Player movement: 5 units/sec
- Safe escape distance: ~15 units minimum
- Allows player to place bomb and escape safely

### Power-Up Balance
- Drop rate: 30% - Ensures progression without being too easy
- Distribution: Equal chance for all types
- Can adjust individual drop rates:
  - Bomb: 30%
  - Range: 30%
  - Speed: 20%
  - Health: 20%

### Map Size vs Gameplay
- 13x11 grid provides good balance:
  - Large enough for strategy
  - Small enough to see everything
  - Fits standard screen resolutions
  - Can fit on single screen with no scrolling

## Game Feel Enhancements

### Visual Feedback (To Add)
- Bomb pulse animation (scale up/down)
- Player blink when taking damage
- Screen shake on explosion
- Particle effects for destruction
- Power-up glow effect

### Audio Feedback (To Add)
- Bomb placement sound
- Explosion sound
- Enemy death sound
- Power-up pickup sound
- Background music
- Footstep sounds

### Animation (To Add)
- Player walk cycle (4 directions)
- Enemy movement animation
- Bomb pulsing before explosion
- Explosion animation frames
- Wall destruction animation

## Future Expansion Ideas

### New Power-Ups
- **Remote Detonator**: Trigger bombs with button press
- **Bomb Pass**: Walk through bombs
- **Wall Pass**: Walk through destructible walls
- **Invincibility**: Temporary immunity (5 seconds)
- **Max Power**: Instantly max out all stats

### New Enemy Types
- **Fast Enemy**: High speed, low health
- **Tank Enemy**: Slow speed, requires 2 hits
- **Bomb Enemy**: Leaves bomb when killed
- **Ghost Enemy**: Passes through walls

### Game Modes
- **Time Attack**: Complete level before time runs out
- **Survival**: Endless waves of enemies
- **Puzzle**: Pre-placed bombs, must use wisely
- **Battle Mode**: Multiplayer competitive

### Level Themes
- **Forest**: Green tiles, tree walls
- **Ice**: Slippery movement
- **Lava**: Periodic damage zones
- **Space**: Altered gravity/physics

## Technical Specifications

### Performance Targets
- Frame Rate: 60 FPS
- Max active objects: ~100
- Grid updates: Per frame
- Physics updates: Fixed 50Hz

### Save Data (Future)
- High score
- Level progress
- Unlocked features
- Settings (volume, controls)

### Platform Targets
- PC (Windows, Mac, Linux)
- WebGL (browser)
- Mobile (iOS, Android) - requires touch controls

---

**Design Philosophy:**
Keep it simple, fun, and true to classic Bomberman gameplay while allowing for creative expansion.
