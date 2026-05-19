using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("Name of the scene to load (must be added to Build Settings).")]
    public string sceneName;

    [Tooltip("If true, load by build index instead of name.")]
    public bool useBuildIndex = false;

    [Tooltip("Build index to load when using build index.")]
    public int sceneBuildIndex = 0;

    [Header("Collision / Timing")]
    [Tooltip("Tag that identifies the player GameObject.")]
    public string playerTag = "Player";

    [Tooltip("Delay (seconds) before loading the scene after collision.")]
    public float delay = 0f;

    [Header("Debug")]
    [Tooltip("Enable debug logs.")]
    public bool debugMode = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log($"SceneChanger triggered by {other.name}. Loading scene in {delay} seconds.");
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log($"SceneChanger2D triggered by {other.name}. Loading scene in {delay} seconds.");
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        if (collision.collider.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log($"SceneChanger collision with {collision.collider.name}. Loading scene in {delay} seconds.");
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private void OnCollisionEnter2D(UnityEngine.Collision2D collision)
    {
        if (collision == null) return;
        if (collision.collider.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log($"SceneChanger2D collision with {collision.collider.name}. Loading scene in {delay} seconds.");
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (useBuildIndex)
        {
            if (debugMode) Debug.Log($"Loading scene by build index: {sceneBuildIndex}");
            SceneManager.LoadScene(sceneBuildIndex);
            yield break;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneChanger: sceneName is empty and useBuildIndex is false. Set a scene name in the inspector or enable useBuildIndex.");
            yield break;
        }

        if (debugMode) Debug.Log($"Loading scene by name: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}