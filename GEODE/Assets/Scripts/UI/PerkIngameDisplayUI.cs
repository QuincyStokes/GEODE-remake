using UnityEngine;

public class PerkIngameDisplayUI : MonoBehaviour
{
    public GameObject perkDisplayPrefab;
    public Transform uiPerkParent;
    private void Start()
    {
        foreach (PerkData perk in RunSettings.Instance.chosenPerks)
        {
            GameObject perkDisplay = Instantiate(perkDisplayPrefab, uiPerkParent);
            PerkSkeleton p = perkDisplay.GetComponent<PerkSkeleton>();
            p.Initialize(perk);
        }
    }
}
