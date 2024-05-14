using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xavier;

namespace Xavier.Tests.TowerOfHanoi;


public class TowerOfHanoiEvaluator : IEvaluator<TowerOfHanoiState, TowerOfHanoiGoal, int>
{
    public async Task<int> EstimateValue(TowerOfHanoiState state, TowerOfHanoiGoal goal)
    {
        // Implement the logic to estimate the value of the current state relative to the goal
        // You can use the OpenAI API or any other method to estimate the value
        // Return the estimated value
        // ...
        return 0;
    }
}