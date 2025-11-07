using TMPro;
using UnityEngine;

public class GemCollector : MonoBehaviour
{
    [Header("Resets Gem count in Canvas to 0")]
    private int Gem = 0;

    [Header("Refrences to Gem: text in Canvas")]
    public TextMeshProUGUI GemsText;
 

    #region Function that destroys game when colliding with it, and adds to the Gem count in Canvas. 
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
    #endregion

   
}
