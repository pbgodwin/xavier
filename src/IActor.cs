namespace Xavier;

public interface IActor<TState, TGoal, TAction>
{
    Task<IEnumerable<TAction>> ProposeActions(TState state, TGoal subgoal, IEnumerable<string> feedback, int actionCount);
}