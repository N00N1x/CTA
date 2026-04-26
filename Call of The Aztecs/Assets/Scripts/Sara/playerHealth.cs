using System;
using System.Reflection;
using UnityEngine;

public class playerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public event Action<float, float> OnHealthChanged;

    [Header("Menu")]
    [Tooltip("Optional reference to the MenuManager. If not set, the script will try to find one at Start.")]
    [SerializeField] private MenuManager menuManager;

    private bool isDead = false;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null) return;

        if (hit.collider.CompareTag("Killplane"))
        {
            currentHealth = 0f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            HandleDeathIfNeeded();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentHealth <= 0f)
            currentHealth = maxHealth;

        if (menuManager == null)
        {
#if UNITY_2023_2_OR_NEWER
            menuManager = UnityEngine.Object.FindFirstObjectByType<MenuManager>();
#elif UNITY_2021_2_OR_NEWER
            menuManager = UnityEngine.Object.FindObjectOfType<MenuManager>();
#else
            menuManager = FindObjectOfType<MenuManager>();
#endif
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        HandleDeathIfNeeded();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        HandleDeathIfNeeded();
    }

    public void ApplyDamage(float amount)
    {
        TakeDamage(amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        Debug.Log($"Player healed {amount}. Current health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ForceDeath()
    {
        if (isDead) return;

        currentHealth = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        HandleDeathIfNeeded();
    }

    private void HandleDeathIfNeeded()
    {
        if (isDead || currentHealth > 0f) return;

        isDead = true;
        Debug.Log("Player health reached 0 — freezing game and showing Restart UI via MenuManager.");

        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;

        if (menuManager != null)
        {
            menuManager.ShowRestartUI();
        }
        else
        {
            Debug.LogWarning("[playerHealth] MenuManager reference is missing. Restart UI cannot be shown from here.");
        }

        gameObject.SetActive(false);
    }
}