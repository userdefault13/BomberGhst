using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int score = 0;
    [SerializeField] private int lives = 3;
    
    [Header("Level Progress")]
    private int enemiesRemaining;
    private int destructibleWallsRemaining;
    
    [Header("UI References")]
    [SerializeField] private UIManager uiManager;
    
    private static GameManager instance;
    
    public static GameManager Instance => instance;
    
    public int Score => score;
    public int Lives => lives;
    public int CurrentLevel => currentLevel;
    
    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        UpdateUI();
        CountLevelObjects();
    }
    
    void CountLevelObjects()
    {
        enemiesRemaining = FindObjectsOfType<Enemy>().Length;
        destructibleWallsRemaining = FindObjectsOfType<DestructibleWall>().Length;
    }
    
    public void OnEnemyKilled(int scoreValue)
    {
        score += scoreValue;
        enemiesRemaining--;
        
        UpdateUI();
        
        // Check if level is complete
        if (enemiesRemaining <= 0)
        {
            LevelComplete();
        }
    }
    
    public void OnWallDestroyed()
    {
        destructibleWallsRemaining--;
    }
    
    public void OnPlayerDeath()
    {
        lives--;
        
        if (lives > 0)
        {
            // Respawn player
            Invoke(nameof(RestartLevel), 2f);
        }
        else
        {
            // Game Over
            Invoke(nameof(GameOver), 2f);
        }
        
        UpdateUI();
    }
    
    void LevelComplete()
    {
        Debug.Log($"Level {currentLevel} Complete!");
        
        currentLevel++;
        
        // Wait a moment then load next level
        Invoke(nameof(LoadNextLevel), 2f);
    }
    
    void LoadNextLevel()
    {
        // In a complete game, you'd load different level scenes
        // For now, we'll just restart the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        CountLevelObjects();
        UpdateUI();
    }
    
    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        CountLevelObjects();
        UpdateUI();
    }
    
    void GameOver()
    {
        Debug.Log("Game Over!");
        
        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
        
        // Restart after a delay
        Invoke(nameof(RestartGame), 3f);
    }
    
    void RestartGame()
    {
        score = 0;
        lives = 3;
        currentLevel = 1;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateScore(score);
            uiManager.UpdateLives(lives);
            uiManager.UpdateLevel(currentLevel);
        }
    }
    
    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }
}
