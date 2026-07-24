using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Type")]
    [SerializeField] private PowerUpType powerUpType;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        
        if (player != null)
        {
            ApplyPowerUp(player);
            Destroy(gameObject);
        }
    }
    
    void ApplyPowerUp(PlayerController player)
    {
        switch (powerUpType)
        {
            case PowerUpType.ExtraBomb:
                player.AddBomb();
                break;
                
            case PowerUpType.ExplosionRange:
                player.IncreaseExplosionRange();
                break;
                
            case PowerUpType.SpeedBoost:
                player.IncreaseSpeed();
                break;
                
            case PowerUpType.Health:
                player.Heal();
                break;
        }
        
        Debug.Log($"Power-up collected: {powerUpType}");
    }
}

public enum PowerUpType
{
    ExtraBomb,      // Increases max bomb count
    ExplosionRange, // Increases explosion range
    SpeedBoost,     // Increases movement speed
    Health          // Restores health
}
