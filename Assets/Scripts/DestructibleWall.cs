using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] [Range(0f, 1f)] private float powerUpDropChance = 0.3f;
    
    [Header("Destruction Settings")]
    [SerializeField] private GameObject destructionEffectPrefab;
    
    public void Destroy()
    {
        // Spawn destruction effect
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Chance to drop power-up
        if (powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value <= powerUpDropChance)
        {
            int randomIndex = Random.Range(0, powerUpPrefabs.Length);
            if (powerUpPrefabs[randomIndex] != null)
            {
                Instantiate(powerUpPrefabs[randomIndex], transform.position, Quaternion.identity);
            }
        }
        
        // Notify game manager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnWallDestroyed();
        }
        
        // Destroy this wall
        Destroy(gameObject);
    }
}
