using UnityEngine;

/// <summary>
/// Debug helper to visualize game state and object information
/// Attach to any GameObject to enable debug drawing
/// </summary>
public class GameDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showPlayerInfo = true;
    [SerializeField] private bool showEnemyInfo = true;
    [SerializeField] private bool showBombInfo = true;
    [SerializeField] private bool showColliders = false;
    [SerializeField] private bool showGridLines = false;
    
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 13;
    [SerializeField] private int gridHeight = 11;
    
    private PlayerController player;
    private GUIStyle labelStyle;
    
    void Start()
    {
        labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 12;
    }
    
    void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }
        
        // Toggle debug features with function keys
        if (Input.GetKeyDown(KeyCode.F1))
            showPlayerInfo = !showPlayerInfo;
        if (Input.GetKeyDown(KeyCode.F2))
            showEnemyInfo = !showEnemyInfo;
        if (Input.GetKeyDown(KeyCode.F3))
            showBombInfo = !showBombInfo;
        if (Input.GetKeyDown(KeyCode.F4))
            showColliders = !showColliders;
        if (Input.GetKeyDown(KeyCode.F5))
            showGridLines = !showGridLines;
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label("=== Debug Info ===", labelStyle);
        GUILayout.Label("F1: Player Info | F2: Enemy Info", labelStyle);
        GUILayout.Label("F3: Bomb Info | F4: Colliders | F5: Grid", labelStyle);
        GUILayout.Space(10);
        
        if (showPlayerInfo && player != null)
        {
            GUILayout.Label($"Player Health: {player.Health}", labelStyle);
            GUILayout.Label($"Max Bombs: {player.MaxBombs}", labelStyle);
            GUILayout.Label($"Explosion Range: {player.ExplosionRange}", labelStyle);
            GUILayout.Label($"Position: {player.transform.position}", labelStyle);
        }
        
        if (showEnemyInfo)
        {
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            GUILayout.Label($"Enemies Alive: {enemies.Length}", labelStyle);
        }
        
        if (showBombInfo)
        {
            Bomb[] bombs = FindObjectsOfType<Bomb>();
            GUILayout.Label($"Active Bombs: {bombs.Length}", labelStyle);
        }
        
        if (GameManager.Instance != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Score: {GameManager.Instance.Score}", labelStyle);
            GUILayout.Label($"Lives: {GameManager.Instance.Lives}", labelStyle);
            GUILayout.Label($"Level: {GameManager.Instance.CurrentLevel}", labelStyle);
        }
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        // Draw colliders
        if (showColliders)
        {
            Collider2D[] colliders = FindObjectsOfType<Collider2D>();
            foreach (var col in colliders)
            {
                Gizmos.color = Color.green;
                if (col is BoxCollider2D box)
                {
                    Gizmos.DrawWireCube(box.transform.position, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.DrawWireSphere(circle.transform.position, circle.radius);
                }
            }
        }
        
        // Draw grid lines
        if (showGridLines)
        {
            Gizmos.color = Color.gray;
            
            // Vertical lines
            for (int x = 0; x <= gridWidth; x++)
            {
                Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, gridHeight, 0));
            }
            
            // Horizontal lines
            for (int y = 0; y <= gridHeight; y++)
            {
                Gizmos.DrawLine(new Vector3(0, y, 0), new Vector3(gridWidth, y, 0));
            }
        }
    }
}
