using TMPro;
using UnityEngine;

public class Pickups : MonoBehaviour
{
    [Header("Resets Gem count to 0")]
    private int Gem = 0;

    [Header("Refrences to Gem: text in Canvas")]
    public TextMeshProUGUI GemsText;

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
        GemsText.text = "Gems: " + Gem.ToString();
        Debug.Log("Gems total: " + Gem + " (added " + pointsToAdd + ")");

        pickupsDestroyed += 1;

        ApplySlowEffect();

        Destroy(other.gameObject);
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
}