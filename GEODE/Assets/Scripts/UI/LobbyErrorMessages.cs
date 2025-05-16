using System.Collections;
using TMPro;
using UnityEngine;

public class LobbyErrorMessages : MonoBehaviour
{
    public static LobbyErrorMessages Instance;
    public float lifetime = 3f;
    [SerializeField] private TMP_Text errorText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        errorText.text = "";
    }

    public void SetError(string text, bool red=true)
    {
        errorText.text += text;
        StartCoroutine(FadeOutText(red));
    }

    private IEnumerator FadeOutText(bool red)
    {
        float elapsed = 0f;
        float t;
        if (red)
        {
            errorText.color = new Color(1, .5f, .5f, 1);
        }
        else
        {
            errorText.color = new Color(.5f, 1f, .5f, 1);
        }
        yield return new WaitForSeconds(2f);
        
        while (elapsed <= lifetime)
        {
            elapsed += Time.deltaTime;
            t = elapsed / lifetime;
            if (red)
            {
                errorText.color = new Color(1, .5f, .5f, 1-t);
            }
            else
            {
                errorText.color = new Color(.5f, 1f, .5f, 1-t);
            }
            
            yield return null;
        }
        errorText.text = "";
        
        
    }
}
