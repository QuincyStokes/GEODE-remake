using System.Threading;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class InventoryHandUI : NetworkBehaviour
{

    //the image for the item we are currently holding
    [SerializeField] private Image handImageUI;
    [SerializeField] private TMP_Text handCountUI;
    [HideInInspector] public bool isHolding;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
        SetHandData(null, 0);
    }

    private void Update()
    {
        transform.position = Input.mousePosition;
    }

    public void SetHandData(Sprite sprite, int count=0)
    {
        if(sprite == null)
        {
            handImageUI.enabled = false;
            handImageUI.sprite = null;
            isHolding = false;
            handCountUI.text = "";
            handImageUI.color = new Color(1, 1, 1, 0);
        }
        else
        {
            handImageUI.enabled = true;
            handImageUI.sprite = sprite;
            isHolding = true;
            if(count > 1)
            {
                handCountUI.text = count.ToString();
            }
            else
            {
                handCountUI.text = "";
            }
            
            handImageUI.color = new Color(1, 1, 1, 1);
        }
    }

}
