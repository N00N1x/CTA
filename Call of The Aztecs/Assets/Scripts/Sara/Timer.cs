using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [Header("Shows Time")]
    public float time;

    [Header("Adds Time to Canvas text")]
    public TextMeshProUGUI timerText;

    void Update()
    {
        time += Time.deltaTime;
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }
}
