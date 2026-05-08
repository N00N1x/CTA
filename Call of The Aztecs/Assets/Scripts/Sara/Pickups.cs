using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Pickups : MonoBehaviour
{
    [Header("Resets Gem count to 0")]
    private int Gem = 0;

    [Header("Refrences to Gem: text in Canvas")]
    public TextMeshProUGUI GemsText;

    [Header("Scenes that should display the stashed Gems on load")]
    [Tooltip("Enter scene names (exact) where the stashed gems should be applied to the GemsText when that scene loads.")]
    [SerializeField] private List<string> gemApplySceneNames = new List<string>();

    [Header("TopDownMovementNew slowdown")]
    public float slowPerPoint = 1f;
    public float minMoveSpeed = 1f;
    private float baseMoveSpeed = -1f;

    [Header("Points per Pickup")]
    public int diamondPoints = 10;
    public int gemPoints = 2;
    public int goldbarPoints = 5;

    [Header("Health per Healthpoint")]
    public float healthRestoreAmount = 25f;

    private int pickupsDestroyed = 0;

    private TopDownMovementNew movement;
    private playerHealth playerHealthRef;

    private const string PlayerPrefsGemsKey = "TotalGems";
    private const string PlayerPrefsGemsHistoryKey = "GemsHistory";

    private List<int> addedHistory = new List<int>();

    [Header("Reset")]
    [Tooltip("If a scene name is set here, the stashed gems will be reset when that scene starts/loads.")]
    [SerializeField] private string resetStashSceneName = "Level1";

    private void Awake()
    {
        movement = GetComponent<TopDownMovementNew>();
        playerHealthRef = GetComponent<playerHealth>();

        if (movement == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                movement = player.GetComponent<TopDownMovementNew>();
            }
        }

        if (playerHealthRef == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHealthRef = player.GetComponent<playerHealth>();
            }
        }

        if (movement != null)
        {
            baseMoveSpeed = movement.moveSpeed;
        }
        else
        {
            Debug.LogWarning("TopDownMovementNew component not found in Awake. Slow effects will be skipped until found.");
        }

        if (playerHealthRef == null)
        {
            Debug.LogWarning("playerHealth component not found in Awake. Health pickups will attempt to find it at runtime.");
        }

        LoadGemData();

        var activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(resetStashSceneName) && activeSceneName == resetStashSceneName)
        {
            ResetStashedGems();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (ShouldApplyGemsToScene(activeSceneName))
        {
            FindAndAssignGemsTextIfNeeded();
            UpdateGemsText();
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
            ResetStashedGems();
        }

        if (ShouldApplyGemsToScene(scene.name))
        {
            FindAndAssignGemsTextIfNeeded();
            UpdateGemsText();
        }
        else
        {
            // Optional: if we want to hide GemsText in scenes not listed, you can clear reference here.
            // GemsText = null;
        }
    }

    private bool ShouldApplyGemsToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return gemApplySceneNames != null && gemApplySceneNames.Contains(sceneName);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UndoLastAdded();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Healthpoint"))
        {
            if (playerHealthRef == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerHealthRef = player.GetComponent<playerHealth>();
                }
            }

            if (playerHealthRef != null)
            {
                playerHealthRef.Heal(healthRestoreAmount);
                Debug.Log($"Collected HealthPoint. Restored {healthRestoreAmount} health.");
            }
            else
            {
                Debug.LogWarning("Collected HealthPoint but playerHealth component not found. Cannot apply heal.");
            }

            Destroy(other.gameObject);
            return;
        }

        int pointsToAdd = 0;
        int otherLayer = other.gameObject.layer;

        if (otherLayer == LayerMask.NameToLayer("Diamond"))
        {
            pointsToAdd = diamondPoints;
        }
        else if (otherLayer == LayerMask.NameToLayer("Gem"))
        {
            pointsToAdd = gemPoints;
        }
        else if (otherLayer == LayerMask.NameToLayer("Goldbar"))
        {
            pointsToAdd = goldbarPoints;
        }
        else if (other.transform.tag == "Gem")
        {
            pointsToAdd = gemPoints;
        }
        else
        {
            return;
        }

        Gem += pointsToAdd;
        addedHistory.Add(pointsToAdd);

        SaveGemData();

        if (ShouldApplyGemsToScene(SceneManager.GetActiveScene().name))
        {
            FindAndAssignGemsTextIfNeeded();
            UpdateGemsText();
        }

        Debug.Log("Gems total: " + Gem + " (added " + pointsToAdd + ")");

        pickupsDestroyed += 1;

        ApplySlowEffect();

        Destroy(other.gameObject);
    }
    private void UndoLastAdded()
    {
        if (addedHistory.Count == 0)
        {
            Debug.Log("[Pickups] No pickup history to undo.");
            return;
        }

        int last = addedHistory[addedHistory.Count - 1];
        addedHistory.RemoveAt(addedHistory.Count - 1);

        Gem = Mathf.Max(0, Gem - last);

        SaveGemData();

        pickupsDestroyed = Mathf.Max(0, pickupsDestroyed - 1);
        ApplySlowEffect();

        if (ShouldApplyGemsToScene(SceneManager.GetActiveScene().name))
        {
            FindAndAssignGemsTextIfNeeded();
            UpdateGemsText();
        }

        Debug.Log($"Removed last-added gems: {last}. New total: {Gem}");
    }

    private void UpdateGemsText()
    {
        if (GemsText != null)
        {
            GemsText.text = "Gems: " + Gem.ToString();
        }
        else
        {
            var go = GameObject.Find("GemsText");
            if (go != null)
            {
                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    GemsText = tmp;
                    GemsText.text = "Gems: " + Gem.ToString();
                    return;
                }
            }

            var byTag = GameObject.FindWithTag("GemsText");
            if (byTag != null)
            {
                var tmp2 = byTag.GetComponent<TextMeshProUGUI>();
                if (tmp2 != null)
                {
                    GemsText = tmp2;
                    GemsText.text = "Gems: " + Gem.ToString();
                    return;
                }
            }

            Debug.LogWarning("[Pickups] GemsText is not assigned and no fallback found. Current gems: " + Gem);
        }
    }
    private void FindAndAssignGemsTextIfNeeded()
    {
        if (GemsText != null) return;

        var go = GameObject.Find("GemsText");
        if (go != null)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                GemsText = tmp;
                return;
            }
        }

        var byTag = GameObject.FindWithTag("GemsText");
        if (byTag != null)
        {
            var tmp2 = byTag.GetComponent<TextMeshProUGUI>();
            if (tmp2 != null)
            {
                GemsText = tmp2;
                return;
            }
        }

    }

    private void ApplySlowEffect()
    {
        if (movement == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                movement = player.GetComponent<TopDownMovementNew>();
                if (movement != null && baseMoveSpeed < 0f)
                {
                    baseMoveSpeed = movement.moveSpeed;
                }
            }
        }

        if (movement == null || baseMoveSpeed < 0f)
        {
            Debug.LogWarning("TopDownMovementNew not Working. Cannot apply slowdown.");
            return;
        }

        float newSpeed = baseMoveSpeed - (pickupsDestroyed * slowPerPoint);
        movement.moveSpeed = Mathf.Max(newSpeed, minMoveSpeed);
    }

    private void SaveGemData()
    {
        PlayerPrefs.SetInt(PlayerPrefsGemsKey, Gem);

        // Save history as CSV oldest->newest
        if (addedHistory.Count > 0)
        {
            PlayerPrefs.SetString(PlayerPrefsGemsHistoryKey, string.Join(",", addedHistory));
        }
        else
        {
            PlayerPrefs.SetString(PlayerPrefsGemsHistoryKey, "");
        }

        PlayerPrefs.Save();
    }

    private void LoadGemData()
    {
        Gem = PlayerPrefs.GetInt(PlayerPrefsGemsKey, 0);

        addedHistory.Clear();
        var hist = PlayerPrefs.GetString(PlayerPrefsGemsHistoryKey, "");
        if (!string.IsNullOrEmpty(hist))
        {
            var tokens = hist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                if (int.TryParse(t, out int v))
                {
                    addedHistory.Add(v);
                }
            }
        }

        pickupsDestroyed = addedHistory.Count;
    }

    private void ResetStashedGems()
    {
        Gem = 0;
        addedHistory.Clear();
        pickupsDestroyed = 0;

        PlayerPrefs.DeleteKey(PlayerPrefsGemsKey);
        PlayerPrefs.DeleteKey(PlayerPrefsGemsHistoryKey);
        PlayerPrefs.Save();

        FindAndAssignGemsTextIfNeeded();
        UpdateGemsText();

        Debug.Log("[Pickups] Stashed gems reset for scene: " + resetStashSceneName);
    }
}