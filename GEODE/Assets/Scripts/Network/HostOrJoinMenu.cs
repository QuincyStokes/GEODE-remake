using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;

public class HostOrJoinMenu : MonoBehaviour
{
    [SerializeField] private GameObject HostOrJoinButtons;
    [SerializeField] private GameObject HostScreen;
    [SerializeField] private GameObject JoinScreen;

    private async void Start()
    {
        //since for now this is the first script to load, lets initialize the unityservice here
        await UnityServices.InitializeAsync();


        await AuthenticationService.Instance.SignInAnonymouslyAsync();


        HostOrJoinButtons.SetActive(true);
        HostScreen.SetActive(false);
        JoinScreen.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
