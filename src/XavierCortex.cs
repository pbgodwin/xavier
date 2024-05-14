namespace Xavier;

public struct PlanSearchResult<TAction, TState, TValue>
{
    public TAction NextAction;
    public TState PredictedState;
    // terminology may be wrong here?
    public TValue EstimatedValue;
}
public class XavierCortex<TGoal, TState, TAction, TValue>
{
    private IActor<TState, TGoal, TAction> actor;
    private IMonitor<TState, TAction> monitor;
    private IPredictor<TState, TAction> predictor;
    private IOrchestrator<TGoal, TState> orchestrator;
    private ITaskDecomposer<TState, TGoal> taskDecomposer;
    private IEvaluator<TState, TGoal, TValue> evaluator;

    public XavierCortex(IActor<TState, TGoal, TAction> actor,
                       IMonitor<TState, TAction> monitor,
                       IPredictor<TState, TAction> predictor,
                       IOrchestrator<TGoal, TState> orchestrator,
                       ITaskDecomposer<TState, TGoal> taskDecomposer,
                       IEvaluator<TState, TGoal, TValue> evaluator)
    {
        this.orchestrator = orchestrator;
        this.taskDecomposer = taskDecomposer;
        this.predictor = predictor;
        this.actor = actor;
        this.monitor = monitor;
        this.evaluator = evaluator;
    }

    // Arxiv 2310.00194 - Algorithm 1, Action Proposal Loop
    // ProposeAction takes a state x and a goal y and generates B potential actions A = ab=1 . . . ab=B.
    // This is implemented via a loop, in which the Actor first proposes potential actions, and the Monitor then assesses those actions according
    // to certain constraints (e.g., task rules), providing feedback if any of the actions are deemed to be invalid. This continues until the proposed
    // actions are considered valid.
    private async Task<IEnumerable<TAction>> ProposeAction(TState state, TGoal goal, int proposedActionCount)
    {
        // initialize validity 
        var isValid = false;
        var accumulatedFeedback = new List<string>(); // initialize feedback
        IEnumerable<TAction> proposedActions = Array.Empty<TAction>();

        while (!isValid)
        {
            // Sample B actions
            proposedActions = await actor.ProposeActions(state, goal, accumulatedFeedback, proposedActionCount);
            
            // Determine validity and provide feedback
            var (newValidity, newFeedback) = await monitor.AssessValidity(state, proposedActions);
            
            // Accumulate feedback
            accumulatedFeedback.Add(newFeedback);
            isValid = newValidity;
        }

        return proposedActions;
    }

    private async Task<PlanSearchResult<TAction, TState, TValue>> Search(int currentLayer, int maxDepth,
                                                                        int proposedActionCount, TState currentState,
                                                                        TGoal currentGoal)
    {
        // Initialize value record
        var layerValues = new List<TValue>(); // V_l, where l == currentLayer
        
        // Initialize next-state record
        var layerNextStates = new List<TState>(); // X_l, where l == currentLayer

        // Propose B actions
        var proposedActions = await ProposeAction(currentState, currentGoal, proposedActionCount);

        // todo: this relies on proposedActions.Count() == proposedActionCount, maybe assert this?
        for (int i = 0; i < proposedActions.Count(); i++)
        {
            var currentAction = proposedActions.ElementAt(i);
            // Predict next state       
            var predictedStateForAction = await predictor.PredictNextState(currentState, currentAction);
            // Update next-state record
            layerNextStates.Add(predictedStateForAction);

            // Terminate search if goal is achieved
            // NOTE: algorithm in paper seems to me missing early termination if goal is achieved, need to confirm
            var goalAchieved = await orchestrator.HasGoalBeenAchieved(predictedStateForAction, currentGoal);
            if (!goalAchieved && currentLayer < maxDepth)
            {
                // Advance search depth
                var result = await Search(currentLayer + 1, maxDepth, proposedActionCount, predictedStateForAction, currentGoal);
                // Update value record
                layerValues.Add(result.EstimatedValue);
            }
            else
            {
                // Evaluate predicted state
                var estimatedValue = await evaluator.EstimateValue(predictedStateForAction, currentGoal);
                // Update value record
                layerValues.Add(estimatedValue);
            }
        }

        // Maximum value (randomly sample if equal value)
        var maxValue = layerValues.Max();
        // a_l ← A_l argmax(V_l) (Select action)
        var bestAction = proposedActions.ElementAt(layerValues.IndexOf(maxValue));
        // x_l ← X_l argmax(V_l) (Select next state)
        var bestNextState = layerNextStates.ElementAt(layerValues.IndexOf(maxValue));

        return new PlanSearchResult<TAction, TState, TValue>
        {
            NextAction = bestAction,
            PredictedState = bestNextState,
            EstimatedValue = maxValue
        };
    }

    public async Task<IEnumerable<TAction>> GeneratePlan(TState state,
                                                         TGoal goal,
                                                         int maxPlanLength,
                                                         int maxLayerDepth,
                                                         int proposedActionCount)
    {
        // Initialize plan
        var plan = new List<TAction>();

        // Generate subgoals
        var subgoals = await taskDecomposer.DecomposeTask(state, goal);

        // We need to iterate through all subgoals _and_ the final goal
        for (int i = 0; i < subgoals.Count() + 1; i++)
        {
            // Determine current goal
            var currentGoal = i == subgoals.Count() ? goal : subgoals.ElementAt(i);

            var goalAchieved = await orchestrator.HasGoalBeenAchieved(state, currentGoal);
            while (!goalAchieved && plan.Count < maxPlanLength)
            {
                // Search for plan
                var result = await Search(0, maxLayerDepth, proposedActionCount, state, currentGoal);
                // Update plan
                plan.Add(result.NextAction);
                // Update state
                state = result.PredictedState;
                // Check if goal is achieved
                goalAchieved = await orchestrator.HasGoalBeenAchieved(state, currentGoal);
            }
        }

        return plan;
    }
}