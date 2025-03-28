using Unity.Netcode;
using UnityEngine;

public class GameAssets : NetworkBehaviour
{
    public static GameAssets Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    //REFERENCES TO THINGS

    public GameObject damageFloater;


}
