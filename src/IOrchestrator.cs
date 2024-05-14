namespace Xavier;

public interface IOrchestrator<TGoal, TState>
{
    Task<bool> HasGoalBeenAchieved(TState predictedState, TGoal currentGoal);
}