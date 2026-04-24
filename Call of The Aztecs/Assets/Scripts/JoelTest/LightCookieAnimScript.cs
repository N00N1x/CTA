using System.Collections;
using UnityEngine;

public class LightCookieAnimScript : MonoBehaviour
{
    Light RayLight;
    [SerializeField] Texture2D[] Cookies;

    void Start()
    {
        Debug.Log("Hejhej");
        RayLight = GetComponent<Light>();
        StartCoroutine(CookiesScrollCor());
    }

    IEnumerator CookiesScrollCor()
    {
        foreach(Texture2D n in Cookies)
        {
            RayLight.cookie = n;
            yield return new WaitForSeconds(0.1f);
        }
        StartCoroutine(CookiesScrollCor());
    }
}
