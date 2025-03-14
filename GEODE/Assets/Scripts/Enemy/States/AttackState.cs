using System.Runtime.CompilerServices;
using UnityEngine;

public class AttackState : BaseEnemyState
{
    public bool isInAnimation = true;
    public bool doAttack = false;
    public bool readyToLeave = false;
    //public float attackTimer;
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //start attack animation
            //trigger attack through animation event
        //FOR NOW FAKE ANIMATINO TIME
        
        doAttack = true;
        readyToLeave = true;
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Exiting AttackState");
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Fixed Update for AttackState");
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
       
        if(doAttack)
        {
            Debug.Log("Attacking from AttackState!");
            owner.animator.SetTrigger("Attack");
            owner.Attack();
            
            readyToLeave = true;

        }
        if(readyToLeave)
        {
            stateMachine.ChangeState(new IdleState());
        }

        
        

    }

    
}
