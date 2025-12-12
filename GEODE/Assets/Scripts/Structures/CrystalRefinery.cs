using UnityEngine;

public class CrystalRefinery : SimpleObject, IUniqueMenu
{
    /// <summary>
    /// Crystal Refinery! 
    /// This structure will process geode crystals into a tier higher, exact amounts and whatnot unknown at this time
    /// Basically, should function as follows:
    /// 
    /// Player adds in some amount of crystals.
    /// If requirements for a refinement are met, the refinement starts.
    /// The inventory locks, and then after a certain amount of time (Coroutine), a new item appears in the output slot!
    /// 
    /// Will need to include BaseContainer, as it'll need to hold things.
    ///     //Can have the main inventory of it be in the upper part of the menu, and the lower can be a second one with output
    /// </summary>


    [Header("UI References")]
    public GameObject uniqueUI;


    [Header("Animation")]
    private Animator _animator;

    //* ----------------- Internal ---------------- */
    public GameObject UniqueUI => uniqueUI;

    public void HideMenu()
    {
        UniqueUI.SetActive(false);
    }

    public void ShowMenu()
    {
        UniqueUI.SetActive(true);
    }

    public override void DoClickedThings()
    {
        base.DoClickedThings();
        
    }

    public override void DoUnclickedThings()
    {
        base.DoUnclickedThings();
        
    }
}
