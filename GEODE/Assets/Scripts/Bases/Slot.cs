using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] protected Image backgroundSprite;
    [SerializeField] protected Image itemSprite;
    [SerializeField] protected TMP_Text itemCount;

    [Header("Background Images")]
    [SerializeField] protected Sprite selectedBackgroundImage;
    [SerializeField] protected Sprite deselectedBackgroundImage;
    [SerializeField] protected Sprite highlightedBackgroundImage;

    [Header("Settings")]

    //protected int count;
    public bool canInteract = true;
    public int SlotIndex { get; set; }
    public BaseContainer container;
    public ItemStack displayedStack { get; private set; }
    public float scaleUpTime;
    public float goalScaleAmount;

    public bool HasTooltip => !displayedStack.Equals(ItemStack.Empty);

    public void InitializeContainer(BaseContainer newContainer, int index)
    {
        container = newContainer;
        SlotIndex = index;
        //container.OnInventoryToggled += ToggleCanInteract;
        ToggleCanInteract(true);
    }

    private void ToggleCanInteract(bool active)
    {
        canInteract = active;
        Debug.Log($"Slot canInteract set to {active}");
    }

    public virtual void SetItem(ItemStack itemStack)
    {
        SetItem(itemStack.Id, itemStack.amount, interactable:true);
    }

    public virtual void SetItem(int id = -1, int newCount = 1, float quality=0f, bool interactable = false)
    {
        //set the internal item data
        itemSprite.preserveAspect = true;
        if (id == -1)
        {
            itemSprite.color = new Color(1, 1, 1, 0);
        }
        else
        {
            //item = ItemDatabase.Instance.GetItem(id);
            itemSprite.sprite = ItemDatabase.Instance.GetItem(id).Icon;
            itemSprite.color = new Color(1, 1, 1, 1);
        }
        if (newCount > 1)
        {
            itemCount.text = newCount.ToString();
        }
        else
        {
            itemCount.text = "";
        }
        canInteract = interactable;
        displayedStack = new ItemStack { Id = id, amount = newCount};
        //StartCoroutine(ScaleUp());
        //set the UI to match
        //CheckItemDepleted();
    }


    //todo

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Slot entered.");
        if (container == null)
        {
            TooltipService.Instance.RequestShow(this);
            return;
        }
        if (container.isOpen)
        {
            TooltipService.Instance.RequestShow(this);
            return;
        }
    
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        TooltipService.Instance.Hide();
    }


    internal void Deselect()
    {
        backgroundSprite.sprite = deselectedBackgroundImage;
    }

    internal void Select()
    {
        backgroundSprite.sprite = selectedBackgroundImage;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log("Slot clicked.");
            HandleLeftClick();

        }
    }

    public virtual void HandleLeftClick()
    {
        Debug.Log($"In HandleLeftClick, Container={container == null}, Owner={container.IsOwner}");
        if(container == null)
        //if (container == null || !container.IsOwner)
        {
            return;
        }
        container.ProcessSlotClick(this);
    }

    public void SetSlotHighlight(bool highlight)
    {
        if (highlight)
        {
            backgroundSprite.sprite = highlightedBackgroundImage;
        }
        else
        {
            backgroundSprite.sprite = deselectedBackgroundImage;
        }
    }

    //! not a bad idea, but the way everything depends on the same ApplyMove function, having it *always* happen is kinda weird
    // public void DoScaleup()
    // {
    //     if (gameObject.activeSelf)
    //     {
    //         StartCoroutine(ScaleUp());
    //     }
    // }

    // private IEnumerator ScaleUp()
    // {
    //     float elapsed = 0f;
    //     while (elapsed < scaleUpTime)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = elapsed / scaleUpTime;
    //         if (t < .5)
    //         {
    //             //first half, scale up
    //             float scale = t * 2 * (goalScaleAmount - 1);
    //             itemSprite.transform.localScale = new Vector3(scale + 1, scale + 1, 0);
    //         }
    //         else
    //         {
    //             float scale = (1 - t) * 2 * (goalScaleAmount - 1);
    //             itemSprite.transform.localScale = new Vector3(scale + 1, scale + 1, 0);
    //         }
    //         yield return null;
    //     }
    // }

    private void OnDestroy()
    {
        if (container != null)
        {
            //TODO
            //container.OnInventoryToggled -= ToggleCanInteract;
        }

    }

}


