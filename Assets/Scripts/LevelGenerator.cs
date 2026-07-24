using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Level Dimensions")]
    [SerializeField] private int width = 13;
    [SerializeField] private int height = 11;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject indestructibleWallPrefab;
    [SerializeField] private GameObject destructibleWallPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject floorPrefab;
    
    [Header("Generation Settings")]
    [SerializeField] [Range(0f, 1f)] private float destructibleWallDensity = 0.7f;
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private Vector2 playerSpawnOffset = new Vector2(1, 1);
    
    [Header("Parent Objects")]
    private Transform wallsParent;
    private Transform enemiesParent;
    private Transform floorParent;
    
    void Start()
    {
        GenerateLevel();
    }
    
    public void GenerateLevel()
    {
        // Create parent objects for organization
        wallsParent = new GameObject("Walls").transform;
        enemiesParent = new GameObject("Enemies").transform;
        floorParent = new GameObject("Floor").transform;
        
        // Generate floor
        GenerateFloor();
        
        // Generate walls
        GenerateWalls();
        
        // Spawn player
        SpawnPlayer();
        
        // Spawn enemies
        SpawnEnemies();
    }
    
    void GenerateFloor()
    {
        if (floorPrefab == null) return;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = new Vector2(x, y);
                Instantiate(floorPrefab, position, Quaternion.identity, floorParent);
            }
        }
    }
    
    void GenerateWalls()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = new Vector2(x, y);
                
                // Create border walls (indestructible)
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    Instantiate(indestructibleWallPrefab, position, Quaternion.identity, wallsParent);
                }
                // Create grid pattern of indestructible walls
                else if (x % 2 == 0 && y % 2 == 0)
                {
                    Instantiate(indestructibleWallPrefab, position, Quaternion.identity, wallsParent);
                }
                // Create destructible walls randomly
                else if (!IsPlayerSpawnArea(x, y) && Random.value < destructibleWallDensity)
                {
                    Instantiate(destructibleWallPrefab, position, Quaternion.identity, wallsParent);
                }
            }
        }
    }
    
    bool IsPlayerSpawnArea(int x, int y)
    {
        // Keep area around player spawn clear
        int spawnX = Mathf.RoundToInt(playerSpawnOffset.x);
        int spawnY = Mathf.RoundToInt(playerSpawnOffset.y);
        
        return (x >= spawnX - 1 && x <= spawnX + 1 && 
                y >= spawnY - 1 && y <= spawnY + 1);
    }
    
    void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, playerSpawnOffset, Quaternion.identity);
        }
    }
    
    void SpawnEnemies()
    {
        if (enemyPrefab == null) return;
        
        int spawnedEnemies = 0;
        int maxAttempts = enemyCount * 10;
        int attempts = 0;
        
        while (spawnedEnemies < enemyCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Random position
            int x = Random.Range(2, width - 2);
            int y = Random.Range(2, height - 2);
            Vector2 position = new Vector2(x, y);
            
            // Check if position is valid (not on walls or player spawn)
            if (!IsPlayerSpawnArea(x, y) && IsPositionClear(position))
            {
                Instantiate(enemyPrefab, position, Quaternion.identity, enemiesParent);
                spawnedEnemies++;
            }
        }
        
        Debug.Log($"Spawned {spawnedEnemies} enemies");
    }
    
    bool IsPositionClear(Vector2 position)
    {
        // Check if there's any collider at this position
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.4f);
        return colliders.Length == 0;
    }
}
