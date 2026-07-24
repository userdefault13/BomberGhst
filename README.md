# BomberGhst 💣

A classic 2D Bomberman-style arcade game built with Unity. Place bombs, destroy walls, defeat enemies, collect power-ups, and clear levels!

![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![Language](https://img.shields.io/badge/Language-C%23-green)

## 🎮 Game Features

- **Classic Bomberman Gameplay**: Place bombs to destroy walls and defeat enemies
- **Dynamic Level Generation**: Procedurally generated levels with destructible and indestructible walls
- **Enemy AI**: Two types of enemies - random wanderers and player chasers
- **Power-Up System**: 
  - 💣 Extra Bomb - Increase max bomb count
  - 💥 Explosion Range - Extend explosion reach
  - ⚡ Speed Boost - Move faster
  - ❤️ Health - Restore health points
- **Progressive Difficulty**: Multiple levels with increasing challenge
- **Score System**: Earn points by defeating enemies and clearing levels
- **Lives System**: Three lives to complete your mission
- **Pause Menu**: Pause/resume gameplay with ESC or P key

## 🚀 Getting Started

### Prerequisites

- Unity 2021.3 or higher (LTS recommended)
- Basic understanding of Unity Editor

### Installation

1. Clone this repository:
```bash
git clone https://github.com/yourusername/BomberGhst.git
cd BomberGhst
```

2. Open the project in Unity:
   - Launch Unity Hub
   - Click "Add" and select the project folder
   - Open the project with Unity 2021.3+

3. Configure Project Settings:
   - Follow instructions in `Assets/ProjectSettings.md` to set up layers, tags, and collision matrix

4. Create Prefabs:
   - Follow the guide in `Assets/Prefabs/README.md` to set up all game object prefabs
   - Create or import sprites following `Assets/Sprites/README.md`

5. Open the main scene:
   - Navigate to `Assets/Scenes/MainGame.unity`
   - Add a Level Generator GameObject with the LevelGenerator script
   - Add a Game Manager GameObject with the GameManager script
   - Create a Canvas with UIManager for the HUD

### Quick Start Setup

1. **Create Game Manager**:
   - Create empty GameObject named "GameManager"
   - Add `GameManager` script
   - Add `UIManager` script component
   - Create UI Canvas with Score, Lives, and Level text elements

2. **Create Level Generator**:
   - Create empty GameObject named "LevelGenerator"
   - Add `LevelGenerator` script
   - Assign all prefab references (walls, player, enemies, floor)

3. **Set Camera**:
   - Set Main Camera to Orthographic
   - Set Size to 6
   - Position at (6, 5, -10)
   - Optionally add `CameraController` script

## 🎯 Controls

| Action | Key |
|--------|-----|
| Move Up | W or ↑ |
| Move Down | S or ↓ |
| Move Left | A or ← |
| Move Right | D or → |
| Place Bomb | Space |
| Pause/Resume | ESC or P |

## 📁 Project Structure

```
BomberGhst/
├── Assets/
│   ├── Scenes/
│   │   └── MainGame.unity         # Main game scene
│   ├── Scripts/
│   │   ├── PlayerController.cs    # Player movement and bomb placement
│   │   ├── Bomb.cs                # Bomb logic and explosion
│   │   ├── Explosion.cs           # Explosion damage and effects
│   │   ├── Enemy.cs               # Enemy AI behavior
│   │   ├── DestructibleWall.cs   # Breakable walls
│   │   ├── IndestructibleWall.cs # Solid walls
│   │   ├── PowerUp.cs             # Power-up collectibles
│   │   ├── GameManager.cs         # Game state and score management
│   │   ├── LevelGenerator.cs     # Procedural level generation
│   │   ├── UIManager.cs           # UI and HUD management
│   │   ├── CameraController.cs    # Camera follow logic
│   │   └── DestructionEffect.cs   # Visual effects
│   ├── Prefabs/                   # Game object prefabs
│   ├── Sprites/                   # Sprite assets
│   ├── Materials/                 # Materials (if needed)
│   └── ProjectSettings.md         # Unity configuration guide
└── README.md                      # This file
```

## 🎨 Customization

### Adjusting Difficulty

Edit values in the Inspector:

**Player Settings** (PlayerController):
- `Move Speed`: Default 5
- `Max Bombs`: Default 1
- `Health`: Default 3
- `Explosion Range`: Default 1

**Enemy Settings** (Enemy):
- `Move Speed`: Default 2
- `Enemy Type`: Random or Chaser
- `Chase Range`: Default 5
- `Score Value`: Default 100

**Level Generation** (LevelGenerator):
- `Width`: Default 13
- `Height`: Default 11
- `Destructible Wall Density`: 0-1 (default 0.7)
- `Enemy Count`: Default 3

**Bomb Settings** (Bomb):
- `Explosion Delay`: Default 3 seconds

### Adding New Power-Ups

1. Add new `PowerUpType` to the enum in `PowerUp.cs`
2. Implement the power-up effect in `PlayerController.cs`
3. Update the switch statement in `PowerUp.ApplyPowerUp()`
4. Create a new prefab and assign the sprite

## 🐛 Known Issues

- No save/load system implemented yet
- Audio system not included (add Unity AudioSource components as needed)
- Animations require manual setup using Unity's Animation system

## 🔧 Development

### Adding New Features

The codebase is modular and easy to extend:

- **New enemy types**: Extend the `Enemy` class or add new `EnemyType` enum values
- **New power-ups**: Add to `PowerUpType` enum and implement effects
- **Different level layouts**: Modify `LevelGenerator.GenerateWalls()`
- **Boss battles**: Create a new `Boss` class inheriting from `Enemy`

### Testing

1. Press Play in Unity Editor
2. Test all controls and features
3. Check console for any errors or warnings

## 📝 TODO / Future Enhancements

- [ ] Add sound effects and background music
- [ ] Create sprite animations for player and enemies
- [ ] Add particle effects for explosions
- [ ] Implement different level themes/tilesets
- [ ] Add multiplayer support (local or online)
- [ ] Create boss battles
- [ ] Add menu system (main menu, options, credits)
- [ ] Implement save/load functionality
- [ ] Mobile touch controls
- [ ] Leaderboard system

## 📜 License

This project is open source and available under the MIT License.

## 🤝 Contributing

Contributions are welcome! Feel free to:
1. Fork the project
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 🙏 Acknowledgments

- Inspired by the classic Bomberman game by Hudson Soft
- Built with Unity Game Engine
- Created as a learning project for game development

## 📧 Contact

For questions or suggestions, please open an issue on GitHub.

---

**Have fun blowing things up! 💣💥**
