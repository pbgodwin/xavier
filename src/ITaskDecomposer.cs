using System;

namespace Xavier
{
    public interface ITaskDecomposer<TState, TGoal>
    {
        Task<List<TGoal>> DecomposeTask(TState state, TGoal goal);
    }
}