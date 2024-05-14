namespace Xavier;
public interface IEvaluator<TState, TGoal, TValue>
{
    Task<TValue> EstimateValue(TState state, TGoal goal);
}