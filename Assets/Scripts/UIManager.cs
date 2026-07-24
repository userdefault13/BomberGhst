using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    
    private bool isPaused = false;
    
    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle pause input
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }
    
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";
        }
    }
    
    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Level: {level}";
        }
    }
    
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverScoreText != null && GameManager.Instance != null)
            {
                gameOverScoreText.text = $"Final Score: {GameManager.Instance.Score}";
            }
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }
        
        Time.timeScale = isPaused ? 0f : 1f;
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        Time.timeScale = 1f;
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
