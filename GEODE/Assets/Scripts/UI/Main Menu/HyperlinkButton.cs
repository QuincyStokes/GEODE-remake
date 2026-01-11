using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]


public class HyperlinkButton : MonoBehaviour
{
    [SerializeField] private string hyperlinkURL;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        button.onClick.AddListener(GoLink);
    }

    private void GoLink()
    {
        if(hyperlinkURL == null) return; 
        Application.OpenURL(hyperlinkURL);
    }   
}
