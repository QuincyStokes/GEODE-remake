using UnityEngine;
using System; 
public class EnemyStateMachine
{
    public BaseEnemyState CurrentState { get; private set;}
    private BaseEnemy owner;
    public BaseEnemyState PreviousState {get; private set;}

    // Injected state references - allows hotswapping different state implementations
    public BaseEnemyState idleState { get; private set; }
    public BaseEnemyState attackState { get; private set; }
    public BaseEnemyState pathToCoreState { get; private set; }
    public BaseEnemyState pathToObstructingState { get; private set; }
    public BaseEnemyState deathState { get; private set; }
    public BaseEnemyState pathToPlayerState { get; private set; }


    public event Action<BaseEnemyState> OnStateChanged;

    public EnemyStateMachine(
        BaseEnemy owner,
        BaseEnemyState idleState,
        BaseEnemyState attackState,
        BaseEnemyState pathToCoreState,
        BaseEnemyState pathToObstructingState,
        BaseEnemyState deathState,
        BaseEnemyState pathToPlayerState)
    {
        this.owner = owner;
        this.idleState = idleState;
        this.attackState = attackState;
        this.pathToCoreState = pathToCoreState;
        this.pathToObstructingState = pathToObstructingState;
        this.deathState = deathState;
        this.pathToPlayerState = pathToPlayerState;

        CurrentState = idleState;
        CurrentState.EnterState(owner, this);
    }

    public void ChangeState(BaseEnemyState newState)
    {
        CurrentState.ExitState(owner, this);
        PreviousState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        CurrentState.EnterState(owner, this);
    }

    /// <summary>
    /// Factory method for creating custom/unique states that don't fit the standard state set.
    /// Useful for special enemy abilities like BoarChargeState, SpiderWebState, etc.
    /// </summary>
    public T GetOrCreateState<T>() where T : BaseEnemyState, new()
    {
        return new T();
    }
}
