using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Sprite centerSprite;
    [SerializeField] private Sprite horizontalSprite;
    [SerializeField] private Sprite verticalSprite;
    
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Initialize(ExplosionDirection direction)
    {
        // Set appropriate sprite based on direction
        if (spriteRenderer != null)
        {
            switch (direction)
            {
                case ExplosionDirection.Center:
                    if (centerSprite != null) spriteRenderer.sprite = centerSprite;
                    break;
                case ExplosionDirection.Horizontal:
                    if (horizontalSprite != null) spriteRenderer.sprite = horizontalSprite;
                    break;
                case ExplosionDirection.Vertical:
                    if (verticalSprite != null) spriteRenderer.sprite = verticalSprite;
                    break;
            }
        }
        
        // Check for enemies and player in explosion range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.4f);
        foreach (var collider in colliders)
        {
            // Damage player
            PlayerController player = collider.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(1);
            }
            
            // Damage enemies
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
            }
            
            // Chain explosions to other bombs
            Bomb bomb = collider.GetComponent<Bomb>();
            if (bomb != null && bomb.gameObject != gameObject)
            {
                Destroy(bomb.gameObject);
            }
        }
        
        // Destroy explosion after duration
        Destroy(gameObject, duration);
    }
}
