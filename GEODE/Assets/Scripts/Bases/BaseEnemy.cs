using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{
    

    private void FixedUpdate()
    {
        Move();
    }


    public virtual void Move()
    {

    }
}
