using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundSprite;
    [SerializeField] private Image itemSprite;
    [SerializeField] private TMP_Text itemCount; 

    private BaseItem item; //item this slot is holding
    private Sprite icon;
    private int count;
    private bool canInteract;

    public void SetItem(BaseItem newItem = null, int newCount = 1, bool interactable=true)
    {
        //set the internal item data
        if(newItem != null)
        {
            item = newItem;
            itemSprite.sprite = newItem.Icon;
            itemSprite.color = new Color(1, 1, 1, 1);
        }
        else
        {
            itemSprite.color = new Color(1, 1, 1, 0);
        }

        count = newCount;
        if(count > 1)
        {
            itemCount.text = count.ToString();
        }
        else
        {
            itemCount.text = "";
        }
        count = newCount;
        canInteract = interactable;

        //set the UI to match
        CheckItemDepleted();
    }

    public BaseItem GetItemInSlot()
    {
        return item;
    }

    public void UpdateCountUI()
    {
        itemCount.text = count.ToString();
    }

    public void AddCount(int newCount)
    {
        count += newCount;
        UpdateCountUI();
    }

    public void SubtractCount(int newCount)
    {
        count -= newCount;
        UpdateCountUI();
    }

    public void SetCount(int newCount)
    {
        count = newCount;
        UpdateCountUI();
    }

    private void CheckItemDepleted()
    {
        if(count < 1)
        {
            //this means we have no more of that item.
            EmptySlot();
        } 
    }

    private void EmptySlot()
    {
        item = null;
        itemSprite.sprite = null;
        itemSprite.color = new Color(1, 1, 1, 0);
        itemCount.text = "";
    }



    //todo later
    public void OnBeginDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}
