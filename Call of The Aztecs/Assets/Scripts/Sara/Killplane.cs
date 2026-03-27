using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Killplane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject obj)
    {
        if (obj == null) return;

        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag("Player"))
            {
                Destroy(t.gameObject);
                StartCoroutine(ReloadSceneNextFrame());
                return;
            }

            t = t.parent;
        }
    }

    private IEnumerator ReloadSceneNextFrame()
    {
        yield return null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}