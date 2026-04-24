using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public event Action<float, float> OnHealthChanged;

    [Header("Scene / Respawn")]
    [SerializeField] private float respawnDelay = 0f;

    [Header("Restart UI")]
    [Tooltip("Assign the RestartUI Canvas GameObject that will be activated when the player dies.")]
    [SerializeField] private GameObject restartUICanvas;

    [Tooltip("If true and Restart UI is assigned, the game will freeze (Time.timeScale = 0) when the player dies.")]
    [SerializeField] private bool useRestartUIOnDeath = true;

    private bool isDead = false;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null) return;

        if (hit.collider.CompareTag("Killplane"))
        {
            Die();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentHealth <= 0f)
            currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
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

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player died.");

        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        if (useRestartUIOnDeath && restartUICanvas != null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            restartUICanvas.SetActive(true);

            Time.timeScale = 0f;
        }
        else
        {
            if (respawnDelay <= 0f)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                StartCoroutine(ReloadSceneAfterDelay(respawnDelay));
            }
        }
    }

    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}