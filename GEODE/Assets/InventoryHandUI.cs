using System.Threading;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class InventoryHandUI : MonoBehaviour
{

    //the image for the item we are currently holding
    [SerializeField] private Image handImageUI;
    [SerializeField] private TMP_Text handCountUI;
    [HideInInspector] public bool isHolding;

    private void OnEnable()
    {
        CursorStack.Instance.OnCursorChanged += Refresh;

    }

    private void OnDisable()
    {
        CursorStack.Instance.OnCursorChanged -= Refresh;
    }
    private void Awake()
    {
        SetHandData(null, 0);
    }

    private void Update()
    {
        transform.position = Input.mousePosition;
    }

    private void Refresh(ItemStack stack)
    {
        if (stack.Id == -1)
        {
            SetHandData(null, 0);
            return;
        }
        
        BaseItem item = ItemDatabase.Instance.GetItem(stack.Id);
        if (item != null)
        {
            SetHandData(item.Icon, stack.amount);
        }
    }
    public void SetHandData(Sprite sprite, int count = 0)
    {
        if (sprite == null)
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
            if (count > 1)
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
