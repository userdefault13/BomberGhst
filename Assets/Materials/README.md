# Materials Folder

This folder can contain Unity materials for sprites and effects.

## Basic Materials

### Default Sprite Material
Unity's built-in "Sprites-Default" material works well for most 2D sprites. No custom material needed unless you want special effects.

### Custom Materials (Optional)

You can create materials for special effects:

#### Glowing Effect Material
- Shader: Sprites/Default
- Add emission for glowing power-ups

#### Flash Effect Material
- Shader: Sprites/Default
- Animate color for damage feedback

#### Transparent Material
- Shader: Sprites/Default
- Adjust alpha for fading effects

## Creating Materials

1. Right-click in Project window
2. Create > Material
3. Name it descriptively
4. Select appropriate shader
5. Assign texture/sprite if needed
6. Adjust properties (color, emission, etc.)

## When to Use Materials

- **Explosion effects**: Additive blending for bright explosions
- **Power-up glow**: Emission for attractive collectibles
- **Damage flash**: Color tint when taking damage
- **Invincibility**: Flickering alpha effect

## Performance Note

For 2D pixel art games, stick with Unity's default sprite shader. Only create custom materials when you need special effects that can't be achieved with sprite properties alone.
