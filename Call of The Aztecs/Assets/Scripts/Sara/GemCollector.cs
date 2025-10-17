using TMPro;
using UnityEngine;

public class GemCollector : MonoBehaviour
{

    private int Gem = 0;

    public TextMeshProUGUI GemsText;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Gem")
        {
            Gem++;
            GemsText.text = "Gems: " + Gem.ToString();
            Debug.Log(Gem);
            Destroy(other.gameObject);
        }
    }


}
