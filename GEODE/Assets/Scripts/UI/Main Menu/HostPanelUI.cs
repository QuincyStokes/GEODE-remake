using TMPro;
using UnityEngine;
public class HostPanelUI : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField] private TMP_InputField playerNameInput;
    public void CreateLobbyButtonClicked()
    {
        if(string.IsNullOrEmpty(playerNameInput.text))
        {
            LobbyErrorMessages.Instance.SetError("Please enter a player name.");
        }
        else
        {
            LobbyHandler.Instance.CreateLobby(playerNameInput.text);
            MainMenuController.Instance.ShowPanel("LobbyPanel");
        }
        
    }
}
