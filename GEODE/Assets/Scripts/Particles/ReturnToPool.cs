using UnityEngine;

public class ReturnToPool : MonoBehaviour
{
    private EffectType _type;
    private float _delay;

    public void ScheduleReturn(EffectType type, float delay)
    {
        _type = type;
        _delay = delay;
        Invoke(nameof(DoReturn), _delay);
    }

    private void DoReturn()
    {
        gameObject.SetActive(false);
        ParticleService._pools[_type].Enqueue(GetComponent<ParticleSystem>());
    }
}
