using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string SaraTestSceneName = "SaraTestScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuTester";

    [Header("UI")]
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject restartUI;
    [SerializeField] private GameObject howToPlayUI;
    public GameObject CanvasStuff;

    [Header("Player")]
    [SerializeField] private playerHealth playerHealthReference;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        FindAndSubscribePlayer();

        if (optionsUI != null) optionsUI.SetActive(false);
        if (restartUI != null) restartUI.SetActive(false);
        if (howToPlayUI != null) howToPlayUI.SetActive(false);

        if (playerHealthReference == null && debugMode)
        {
            Debug.LogWarning("[MenuManager] No playerHealth reference found. Restart UI will not auto-show on death.");
        }
    }

    private void OnDestroy()
    {
        if (playerHealthReference != null)
            playerHealthReference.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        if (howToPlayUI != null && howToPlayUI.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseHowToPlayImmediately();
            }
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (currentHealth <= 0f)
        {
            ShowRestartUI();
        }
    }

    public void OnPlayButton()
    {
        if (debugMode) Debug.Log("[MenuManager] Loading scene: " + SaraTestSceneName);
        if (!string.IsNullOrEmpty(SaraTestSceneName))
            SceneManager.LoadScene(SaraTestSceneName);
    }

    public void OnQuitButton()
    {
        if (debugMode) Debug.Log("[MenuManager] Quit requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnMainMenuButton()
    {
        if (debugMode) Debug.Log("[MenuManager] Loading main menu scene: " + mainMenuSceneName);

        Time.timeScale = 1f;
        HideRestartUI();

        if (CanvasStuff != null)
        {
            CanvasStuff.SetActive(false);
        }

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnRestartButton()
    {
        if (debugMode) Debug.Log("[MenuManager] Restarting current scene.");

        HideRestartUI();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowRestartUI()
    {
        if (restartUI == null)
        {
            if (debugMode) Debug.LogWarning("[MenuManager] restartUI reference is null.");
            return;
        }

        restartUI.SetActive(true);
        if (debugMode) Debug.Log("[MenuManager] Restart UI activated.");
    }

    public void HideRestartUI()
    {
        if (restartUI == null) return;
        restartUI.SetActive(false);
        if (debugMode) Debug.Log("[MenuManager] Restart UI deactivated.");
    }

    public void OnOpenHowToPlayButton()
    {
        if (howToPlayUI == null)
        {
            if (debugMode) Debug.LogWarning("[MenuManager] howToPlayUI reference is null.");
            return;
        }

        howToPlayUI.SetActive(true);
        if (debugMode) Debug.Log("[MenuManager] HowToPlay UI opened. Press Escape to close.");
    }

    public void OnCloseHowToPlayUI()
    {
        CloseHowToPlayImmediately();
    }

    private void CloseHowToPlayImmediately()
    {
        if (howToPlayUI == null) return;

        howToPlayUI.SetActive(false);
        if (debugMode) Debug.Log("[MenuManager] HowToPlay UI closed.");
    }

    private void FindAndSubscribePlayer()
    {
        if (playerHealthReference == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHealthReference = player.GetComponent<playerHealth>();
            }
        }

        if (playerHealthReference != null)
        {
            playerHealthReference.OnHealthChanged -= HandleHealthChanged;
            playerHealthReference.OnHealthChanged += HandleHealthChanged;
        }
        else if (debugMode)
        {
            Debug.LogWarning("[MenuManager] No playerHealth reference found.");
        }
    }
}