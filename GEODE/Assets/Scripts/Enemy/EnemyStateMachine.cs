using UnityEngine;

public class EnemyStateMachine
{
    public BaseEnemyState CurrentState { get; private set;}
    private BaseEnemy owner;
    public BaseEnemyState PreviousState {get; private set;}

    public EnemyStateMachine(BaseEnemy owner)
    {
        this.owner = owner;
        CurrentState = new IdleState();
        CurrentState.EnterState(owner, this);
    }

    public void ChangeState(BaseEnemyState newState)
    {
        CurrentState.ExitState(owner, this);
        PreviousState = CurrentState;
        CurrentState = newState;
        CurrentState.EnterState(owner, this);
    }
}
