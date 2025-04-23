using UnityEngine;

public interface IKnockbackable 
{
    public void TakeKnockbackServerRpc(Vector2 direction, float force);
}
