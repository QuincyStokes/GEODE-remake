using System.Collections;
using TMPro;
using UnityEngine;

public class DayNumber : MonoBehaviour
{

    [SerializeField] private TMP_Text dayNumText;
    [SerializeField] private int dayNum = 1;
    [SerializeField] private float scaleTime;
    [SerializeField] private float scaleAmount;
    void Start()
    {
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.becameDay += IncreaseDay;
        }
    }

    private void IncreaseDay()
    {
        dayNum++;
        StartCoroutine(ScaleDayText());
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
                dayNumText.text = $"Day {dayNum}";
                dayNumText.gameObject.transform.localScale = new Vector3((1-t) * scaleAmount + 1, (1-t) * scaleAmount + 1, 1);
            }
            
            yield return null;
        }
        

    }
}
