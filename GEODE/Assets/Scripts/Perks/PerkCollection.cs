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
        foreach (PerkData data in perksDatas)
        {
        
            GameObject perk = Instantiate(pm.perkPrefab);
            Perk p = perk.GetComponent<Perk>();
            perks.Add(p);

            p.Initialize(data, this);
            perk.transform.SetParent(transform);
            
            
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
