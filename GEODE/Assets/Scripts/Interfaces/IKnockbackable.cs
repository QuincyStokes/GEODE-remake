using UnityEngine;

public interface IKnockbackable 
{
    public void TakeKnockback(Vector2 direction, float force); //since only the server would call this, doesn't need to be a ServerRpc
}
