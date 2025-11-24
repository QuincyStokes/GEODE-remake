using UnityEngine;
using UnityEngine.UI;
public class ClockUI : MonoBehaviour
{

    [SerializeField] private Transform clockHand;
    [SerializeField] private float spriteUpOffset;
    [SerializeField] private Image clockBackground;

    [SerializeField] private Sprite normalClockBackground;
    private DayCycleManager manager;

    private void Start()
    {
        manager = DayCycleManager.Instance;
        manager.OnDay1Finished += SwitchBackground;
    }

    private void Update()
    {
        if (manager == null) return;
        //need to subtract sunrise percent so "start" is at 0, not offset
        float tSinceSunrise = Mathf.Repeat(manager.NormalizedTime, 1f);

        float zRotation = Mathf.Lerp(360f, 0f, tSinceSunrise) + spriteUpOffset;

        clockHand.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private void SwitchBackground()
    {
        clockBackground.sprite = normalClockBackground;
    }

    private void OnDestroy()
    {
        if(manager != null)
            manager.OnDay1Finished -= SwitchBackground;
    }
}
