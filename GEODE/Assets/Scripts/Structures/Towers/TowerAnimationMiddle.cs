using UnityEngine;

public class TowerAnimationMiddle : MonoBehaviour
{
    private BaseTower baseTower;
    private void Awake()
    {
        baseTower = GetComponentInParent<BaseTower>();
    }

    public void DoTowerFire()
    {
        if(baseTower==null) return;
        baseTower.Fire();
    }
}
