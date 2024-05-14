using System;

namespace Xavier
{
    public interface IPredictor<TState, TAction>
    {
        Task<TState> PredictNextState(TState currentState, TAction action);
    }
}