using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button[] restartButtons;

    public bool GameplayActive { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (restartButtons != null)
            foreach (Button button in restartButtons)
                if (button != null && button != restartButton) button.onClick.AddListener(RestartGame);
    }

    private void Start()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        UpdateLives(player != null ? player.RemainingLives : 3);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
        if (restartButtons != null)
            foreach (Button button in restartButtons)
                if (button != null && button != restartButton) button.onClick.RemoveListener(RestartGame);
    }

    public void UpdateLives(int lives)
    {
        if (lifeIcons == null) return;
        for (int i = 0; i < lifeIcons.Length; i++)
            if (lifeIcons[i] != null) lifeIcons[i].gameObject.SetActive(i < lives);
    }

    public void ShowVictory() => EndGame(victoryPanel);
    public void ShowDefeat() => EndGame(defeatPanel);

    private void EndGame(GameObject panel)
    {
        if (!GameplayActive) return;
        GameplayActive = false;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
