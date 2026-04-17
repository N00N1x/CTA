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

    // Prevent multiple death/respawn calls causing a loop
    private bool isDead = false;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null) return;

        if (hit.collider.CompareTag("Killplane"))
        {
            Die();
        }
    }

    private void OnEnable()
    {
        print("joakim enabled");
    }
    private void OnDisable()
    {
        print("joakim disabled");
    }
    private void OnDestroy()
    {
        print("joakim destroyed");
    }
    private void Start()
    {
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
        // Guard to avoid repeated death/scene loads when multiple triggers/collisions occur
        if (isDead) return;
        isDead = true;

        Debug.Log("Player died.");

        // Don't Destroy(gameObject) here — reloading the scene will recreate objects.
        // Destroying immediately can cause unexpected race conditions where death is retriggered.
        // If you need a death visual, play it here and optionally destroy after the respawn.

        // Optionally disable this component or player control scripts here:
        // var controller = GetComponent<CharacterController>();
        // if (controller != null) controller.enabled = false;

        if (respawnDelay <= 0f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            StartCoroutine(ReloadSceneAfterDelay(respawnDelay));
        }
    }
    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}