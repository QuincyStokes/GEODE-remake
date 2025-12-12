using System.Collections;
using TMPro;
using UnityEngine;

public class DayNumber : MonoBehaviour
{

    [SerializeField] private TMP_Text dayNumText;
    [SerializeField] private int dayNum = 1;
    [SerializeField] private int nightNum = 0;
    [SerializeField] private float scaleTime;
    [SerializeField] private float scaleAmount;
    [SerializeField] private Color dayTextColor;
    [SerializeField] private Color nightTextColor;
    void Start()
    {

    }

    public void IncreaseDay()
    {
        dayNum++;
        StartCoroutine(ScaleDayText());
    }

    public void IncreaseNight()
    {
        nightNum++;
        StartCoroutine(ScaleNightText());
    }
    private IEnumerator ScaleDayText()
    {
        
        float elapsed = 0f;
        float t;
        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            t = elapsed / scaleTime;
            if (t < .5)
            {
                dayNumText.gameObject.transform.localScale = new Vector3(t * scaleAmount + 1, t * scaleAmount + 1, 1);
            }
            else
            {
                dayNumText.color = dayTextColor;
                dayNumText.text = $"Day {dayNum}";
                dayNumText.gameObject.transform.localScale = new Vector3((1 - t) * scaleAmount + 1, (1 - t) * scaleAmount + 1, 1);
            }

            yield return null;
        }


    }
     private IEnumerator ScaleNightText()
    {
        
        float elapsed = 0f;
        float t;
        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            t = elapsed / scaleTime;
            if (t < .5)
            {
                dayNumText.gameObject.transform.localScale = new Vector3(t * scaleAmount + 1, t * scaleAmount + 1, 1);
            }
            else
            {
                dayNumText.color = nightTextColor;
                dayNumText.text = $"Night {nightNum}";
                dayNumText.gameObject.transform.localScale = new Vector3((1 - t) * scaleAmount + 1, (1 - t) * scaleAmount + 1, 1);
            }

            yield return null;
        }


    }
}
