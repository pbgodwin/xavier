namespace Xavier;

public interface IMonitor<TState, TAction>
{
    Task<(bool validity, string feedback)> AssessValidity(TState state, IEnumerable<TAction> action);
}