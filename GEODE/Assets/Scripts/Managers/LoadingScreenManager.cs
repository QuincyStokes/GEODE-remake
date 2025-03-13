using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    private void Start()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            //load the gameplay scene in the background
            NetworkManager.Singleton.SceneManager.LoadScene("GameplayTest", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }   
    }
}
