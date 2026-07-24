using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private int maxBombs = 1;
    [SerializeField] private KeyCode bombKey = KeyCode.Space;
    
    [Header("Player Stats")]
    [SerializeField] private int health = 3;
    [SerializeField] private int explosionRange = 1;
    
    private Rigidbody2D rb;
    private Vector2 movement;
    private int currentBombCount = 0;
    private bool isDead = false;
    
    public int ExplosionRange => explosionRange;
    public int MaxBombs => maxBombs;
    public int Health => health;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Handle movement input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        // Handle bomb placement
        if (Input.GetKeyDown(bombKey) && currentBombCount < maxBombs)
        {
            PlaceBomb();
        }
    }
    
    void FixedUpdate()
    {
        if (isDead) return;
        
        // Move the player
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
    
    void PlaceBomb()
    {
        // Round position to grid
        Vector2 bombPosition = new Vector2(
            Mathf.Round(transform.position.x),
            Mathf.Round(transform.position.y)
        );
        
        // Check if there's already a bomb at this position
        Collider2D[] colliders = Physics2D.OverlapCircleAll(bombPosition, 0.2f);
        foreach (var col in colliders)
        {
            if (col.GetComponent<Bomb>() != null)
            {
                return; // Don't place bomb if one already exists here
            }
        }
        
        GameObject bomb = Instantiate(bombPrefab, bombPosition, Quaternion.identity);
        Bomb bombScript = bomb.GetComponent<Bomb>();
        if (bombScript != null)
        {
            bombScript.Initialize(this, explosionRange);
        }
        
        currentBombCount++;
    }
    
    public void OnBombExploded()
    {
        currentBombCount--;
    }
    
    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Optional: Add invincibility frames or visual feedback
            Debug.Log($"Player hit! Health remaining: {health}");
        }
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("Player died!");
        
        // Notify game manager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerDeath();
        }
        
        // Optional: Play death animation before destroying
        Destroy(gameObject);
    }
    
    // Power-up methods
    public void AddBomb()
    {
        maxBombs++;
        Debug.Log($"Max bombs increased to {maxBombs}");
    }
    
    public void IncreaseExplosionRange()
    {
        explosionRange++;
        Debug.Log($"Explosion range increased to {explosionRange}");
    }
    
    public void IncreaseSpeed()
    {
        moveSpeed += 1f;
        Debug.Log($"Speed increased to {moveSpeed}");
    }
    
    public void Heal(int amount = 1)
    {
        health = Mathf.Min(health + amount, 3); // Cap at 3 hearts
        Debug.Log($"Health restored to {health}");
    }
}
