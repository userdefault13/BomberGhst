# Unity Project Settings

This folder contains Unity-specific project settings and configurations.

## Layer Setup

Configure the following layers in Unity (Edit > Project Settings > Tags and Layers):

### Layers:
- **Layer 8**: Player
- **Layer 9**: Enemy
- **Layer 10**: Bomb
- **Layer 11**: Explosion
- **Layer 12**: Wall
- **Layer 13**: PowerUp

## Physics 2D Collision Matrix

Configure collision in Edit > Project Settings > Physics 2D:

| Layer | Player | Enemy | Bomb | Explosion | Wall | PowerUp |
|-------|--------|-------|------|-----------|------|---------|
| **Player** | ✓ | ✓ | ✗ | ✗ | ✓ | ✗ |
| **Enemy** | ✓ | ✓ | ✗ | ✗ | ✓ | ✗ |
| **Bomb** | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| **Explosion** | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| **Wall** | ✓ | ✓ | ✓ | ✗ | ✓ | ✗ |
| **PowerUp** | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |

✓ = Collides
✗ = No collision

## Tags

Create the following tags:
- Player
- Enemy
- Bomb
- Explosion
- Wall
- DestructibleWall
- PowerUp

## Input Settings

The game uses Unity's default Input Manager with these axes:
- **Horizontal**: A/D or Left/Right arrows
- **Vertical**: W/S or Up/Down arrows
- **Fire1** (Space): Place bomb
- **Pause** (Escape or P): Pause game

## Sorting Layers

Set up sorting layers for proper sprite rendering:
1. Background (Floor)
2. Items (Power-ups)
3. Walls
4. Bombs
5. Effects (Explosions)
6. Enemies
7. Player
8. UI

## Quality Settings

Recommended settings for 2D pixel art:
- Texture Quality: Full Res
- Anti Aliasing: Disabled
- VSync: On
- Target Frame Rate: 60
