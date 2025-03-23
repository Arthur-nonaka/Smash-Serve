using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingDots : MonoBehaviour
{
    public Text loadingText;
    public float dotInterval = 0.5f;
    private string baseText = "Loading";
    private int dotCount = 0;
    private int maxDots = 3;

    void Start()
    {
        if (loadingText == null)
        {
            loadingText = GetComponent<Text>();
        }
        StartCoroutine(AnimateDots());
    }

    void OnEnable()
    {
        if (loadingText == null)
        {
            loadingText = GetComponent<Text>();
        }
        StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        while (true)
        {
            dotCount = (dotCount + 1) % (maxDots + 1);
            loadingText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(dotInterval);
        }
    }
}
