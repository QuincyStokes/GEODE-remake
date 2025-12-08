using Unity.Netcode;
using UnityEngine;

public interface ITracksHits
{
    public NetworkVariable<int> kills {get; set;}
    public void HitSomething(IDamageable damageable);
    public void KilledSomething(IDamageable damageable);
}
