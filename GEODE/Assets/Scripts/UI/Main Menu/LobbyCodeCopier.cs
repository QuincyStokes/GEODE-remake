using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class LobbyCodeCopier : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TMP_Text tooltipText;

    private bool isShowingTooltip = false;

    private void Update()
    {
        if(isShowingTooltip)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                Input.mousePosition,
                null,
                out position);
            tooltip.transform.localPosition = position + new Vector2(5, 5);
        }   
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        GUIUtility.systemCopyBuffer = lobbyCodeText.text;
        tooltipText.text = "Copied!";
        LobbyErrorMessages.Instance.SetError("Lobby code copied to clipboard!", false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isShowingTooltip = true;
        tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isShowingTooltip = false;
        tooltip.SetActive(false);
        tooltipText.text = "Click to copy";
    }
}
