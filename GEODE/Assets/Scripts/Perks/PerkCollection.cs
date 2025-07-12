using System.Collections.Generic;
using UnityEngine;

public class PerkCollection : MonoBehaviour
{
    public List<PerkData> perksDatas;
    private PerkManager pm;
    public List<Perk> perks;

    public void Initialize(PerkManager pm)
    {
        Debug.Log("Initializing Perk Collection");
        this.pm = pm;
        foreach (PerkData perk in perksDatas)
        {
            GameObject perkObj = Instantiate(pm.perkPrefab, transform);
            Perk p = perkObj.GetComponent<Perk>();
            p.Initialize(perk, this);
        }
    }

    public void UnselectAllPerks()
    {
        foreach (Perk perk in perks)
        {
            perk.DeselectPerk();
        }
    }
}
