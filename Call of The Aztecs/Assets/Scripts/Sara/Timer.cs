using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Optional: assign timer UI in inspector")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Scenes that should show the stashed time on load")]
    [Tooltip("Exact scene names where TotalTimeSeconds should be displayed on the TimerText.")]
    [SerializeField] private List<string> timeApplySceneNames = new List<string>();

    [Header("Reset")]
    [Tooltip("If set, TotalTimeSeconds will be reset when this scene is loaded.")]
    [SerializeField] private string resetStashSceneName = "";
    public float TotalTimeSeconds { get; private set; } = 0f;

    private const string PlayerPrefsTimeKey = "TotalPlayTimeSeconds";

    private void Awake()
    {
        var rootGameObject = transform.root.gameObject;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(rootGameObject);

        TotalTimeSeconds = PlayerPrefs.GetFloat(PlayerPrefsTimeKey, 0f);

        SceneManager.sceneLoaded += OnSceneLoaded;

        var active = SceneManager.GetActiveScene().name;
        if (ShouldApplyTimeToScene(active))
        {
            FindAndAssignTimerTextIfNeeded();
            UpdateTimerUI();
        }

        if (!string.IsNullOrEmpty(resetStashSceneName) && active == resetStashSceneName)
        {
            ResetStashedTime();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(resetStashSceneName) && scene.name == resetStashSceneName)
        {
            ResetStashedTime();
        }

        if (ShouldApplyTimeToScene(scene.name))
        {
            FindAndAssignTimerTextIfNeeded();
            UpdateTimerUI();
        }
    }

    private void Update()
    {
        TotalTimeSeconds += Time.deltaTime;
        if (ShouldApplyTimeToScene(SceneManager.GetActiveScene().name))
            UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + FormatTime(TotalTimeSeconds);
        }
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", mins, secs);
    }

    private bool ShouldApplyTimeToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return timeApplySceneNames != null && timeApplySceneNames.Contains(sceneName);
    }

    private void FindAndAssignTimerTextIfNeeded()
    {
        if (timerText != null) return;

        var go = GameObject.Find("TimerText");
        if (go != null)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                timerText = tmp;
                return;
            }
        }

        var byTag = GameObject.FindWithTag("TimerText");
        if (byTag != null)
        {
            var tmp2 = byTag.GetComponent<TextMeshProUGUI>();
            if (tmp2 != null)
            {
                timerText = tmp2;
                return;
            }
        }
    }

    public void AddSeconds(float seconds)
    {
        TotalTimeSeconds += seconds;
        SaveTotalTime();
        if (ShouldApplyTimeToScene(SceneManager.GetActiveScene().name))
            UpdateTimerUI();
    }

    public void RegisterTimerText(TextMeshProUGUI text)
    {
        timerText = text;
        if (ShouldApplyTimeToScene(SceneManager.GetActiveScene().name))
            UpdateTimerUI();
    }

    public void SaveTotalTime()
    {
        PlayerPrefs.SetFloat(PlayerPrefsTimeKey, TotalTimeSeconds);
        PlayerPrefs.Save();
    }

    public void ResetStashedTime()
    {
        TotalTimeSeconds = 0f;
        PlayerPrefs.DeleteKey(PlayerPrefsTimeKey);
        PlayerPrefs.Save();
        if (ShouldApplyTimeToScene(SceneManager.GetActiveScene().name))
        {
            FindAndAssignTimerTextIfNeeded();
            UpdateTimerUI();
        }
    }
}