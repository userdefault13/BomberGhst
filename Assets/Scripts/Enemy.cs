using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float directionChangeInterval = 2f;
    
    [Header("AI Behavior")]
    [SerializeField] private EnemyType enemyType = EnemyType.Random;
    [SerializeField] private float chaseRange = 5f;
    
    [Header("Combat")]
    [SerializeField] private int scoreValue = 100;
    
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float directionTimer;
    private PlayerController player;
    private bool isDead = false;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ChooseNewDirection();
    }
    
    void Start()
    {
        player = FindObjectOfType<PlayerController>();
    }
    
    void Update()
    {
        if (isDead) return;
        
        directionTimer -= Time.deltaTime;
        
        if (directionTimer <= 0)
        {
            ChooseNewDirection();
            directionTimer = directionChangeInterval;
        }
    }
    
    void FixedUpdate()
    {
        if (isDead) return;
        
        // Determine movement based on enemy type
        Vector2 moveDirection = currentDirection;
        
        if (enemyType == EnemyType.Chaser && player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= chaseRange)
            {
                moveDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            }
        }
        
        // Try to move in current direction
        Vector2 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        
        // Check if path is clear
        RaycastHit2D hit = Physics2D.Raycast(rb.position, moveDirection, moveSpeed * Time.fixedDeltaTime + 0.3f);
        
        if (hit.collider == null || hit.collider.isTrigger)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            // Hit an obstacle, choose new direction
            ChooseNewDirection();
        }
    }
    
    void ChooseNewDirection()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        
        // Shuffle directions
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 temp = directions[i];
            int randomIndex = Random.Range(i, directions.Length);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }
        
        // Try each direction until we find a clear one
        foreach (Vector2 direction in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.6f);
            
            if (hit.collider == null || hit.collider.isTrigger)
            {
                currentDirection = direction;
                return;
            }
        }
        
        // If no clear direction, stay still
        currentDirection = Vector2.zero;
    }
    
    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Notify game manager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnEnemyKilled(scoreValue);
        }
        
        // Optional: Play death animation or effect
        Debug.Log($"Enemy killed! Score: {scoreValue}");
        
        Destroy(gameObject);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Damage player on contact
        PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            hitPlayer.TakeDamage(1);
        }
    }
}

public enum EnemyType
{
    Random,   // Moves randomly
    Chaser    // Chases player when in range
}
