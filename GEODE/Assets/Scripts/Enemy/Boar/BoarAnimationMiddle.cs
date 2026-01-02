using Unity.VisualScripting;
using UnityEngine;



public class BoarAnimationMiddle : MonoBehaviour
{
    //This script is just a middleman between the animation events and the actual boar brain since they exist on different objects.

    private BoarEnemy boarEnemy;

    private void Awake()
    {
        boarEnemy = GetComponentInParent<BoarEnemy>();
    }

    public void DoBoarAttack()
    {
        if(boarEnemy != null) 
            boarEnemy.Attack();
    }
}
