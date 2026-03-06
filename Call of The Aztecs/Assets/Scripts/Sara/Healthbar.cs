using System;
using System.Collections.Generic;
using UnityEngine;

public class Healthbar : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private playerHealth playerHealth;

    [Header("Hearts")]
    [SerializeField] private int heartsCount = 3;

    [SerializeField] private GameObject[] heartHealthObjects;

    [Header("Per-heart damage")]
    [SerializeField] private float damagePerHeart = 0f;

    private void Awake()
    {
        EnsureHeartArray();
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = UnityEngine.Object.FindFirstObjectByType<playerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning($"{nameof(Healthbar)}: playerHealth not found.");
            return;
        }

        playerHealth.OnHealthChanged += OnPlayerHealthChanged;

        OnPlayerHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
    }

    private void OnValidate()
    {
        if (heartsCount < 1) heartsCount = 1;
        EnsureHeartArray();
    }

    private void EnsureHeartArray()
    {
        if (heartHealthObjects == null || heartHealthObjects.Length != heartsCount)
            heartHealthObjects = new GameObject[heartsCount];

        for (int i = 0; i < heartsCount; i++)
        {
            if (heartHealthObjects[i] != null) continue;

            var heartTransform = transform.Find($"Heart{i}");
            if (heartTransform != null)
            {
                var healthChild = heartTransform.Find("Health");
                if (healthChild != null)
                    heartHealthObjects[i] = healthChild.gameObject;
                else
                    heartHealthObjects[i] = heartTransform.gameObject;
            }
            else
            {
                heartHealthObjects[i] = null;
            }
        }
    }

    private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        float perHeart = damagePerHeart > 0f ? damagePerHeart : (maxHealth > 0f ? maxHealth / heartsCount : 1f);
        if (perHeart <= 0f) perHeart = 1f;

        int activeHearts = Mathf.Clamp(Mathf.CeilToInt(currentHealth / perHeart), 0, heartsCount);

        for (int i = 0; i < heartsCount; i++)
        {
            var obj = (i < heartHealthObjects.Length) ? heartHealthObjects[i] : null;
            if (obj == null)
                continue;

            bool shouldBeActive = i < activeHearts;
            if (obj.activeSelf != shouldBeActive)
                obj.SetActive(shouldBeActive);
        }
    }

    public void SetDamagePerHeart(float newDamagePerHeart)
    {
        damagePerHeart = Mathf.Max(0f, newDamagePerHeart);
        if (playerHealth != null)
            OnPlayerHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }
}