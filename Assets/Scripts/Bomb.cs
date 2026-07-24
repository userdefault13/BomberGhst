using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private LayerMask obstacleLayer;
    
    private int explosionRange = 1;
    private PlayerController owner;
    
    public void Initialize(PlayerController player, int range)
    {
        owner = player;
        explosionRange = range;
        StartCoroutine(ExplodeAfterDelay());
    }
    
    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }
    
    void Explode()
    {
        // Notify owner that bomb exploded
        if (owner != null)
        {
            owner.OnBombExploded();
        }
        
        // Create explosion at bomb position
        CreateExplosion(transform.position, ExplosionDirection.Center);
        
        // Create explosions in four directions
        ExplodeInDirection(Vector2.up);
        ExplodeInDirection(Vector2.down);
        ExplodeInDirection(Vector2.left);
        ExplodeInDirection(Vector2.right);
        
        // Destroy the bomb
        Destroy(gameObject);
    }
    
    void ExplodeInDirection(Vector2 direction)
    {
        for (int i = 1; i <= explosionRange; i++)
        {
            Vector2 position = (Vector2)transform.position + direction * i;
            
            // Check for obstacles
            RaycastHit2D hit = Physics2D.Raycast(
                (Vector2)transform.position + direction * (i - 0.5f),
                direction,
                0.6f,
                obstacleLayer
            );
            
            if (hit.collider != null)
            {
                // Check if it's a destructible wall
                DestructibleWall wall = hit.collider.GetComponent<DestructibleWall>();
                if (wall != null)
                {
                    wall.Destroy();
                    CreateExplosion(position, GetDirectionType(direction));
                }
                
                // Stop explosion if it hits any obstacle
                break;
            }
            else
            {
                CreateExplosion(position, GetDirectionType(direction));
            }
        }
    }
    
    void CreateExplosion(Vector2 position, ExplosionDirection direction)
    {
        GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        Explosion explosionScript = explosion.GetComponent<Explosion>();
        if (explosionScript != null)
        {
            explosionScript.Initialize(direction);
        }
    }
    
    ExplosionDirection GetDirectionType(Vector2 direction)
    {
        if (direction == Vector2.up) return ExplosionDirection.Vertical;
        if (direction == Vector2.down) return ExplosionDirection.Vertical;
        if (direction == Vector2.left) return ExplosionDirection.Horizontal;
        if (direction == Vector2.right) return ExplosionDirection.Horizontal;
        return ExplosionDirection.Center;
    }
}

public enum ExplosionDirection
{
    Center,
    Horizontal,
    Vertical
}
