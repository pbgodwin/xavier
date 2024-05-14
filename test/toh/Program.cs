
using dotenv.net;

using OpenAI_API;

using Xavier;
using Xavier.Tests.TowerOfHanoi;

DotEnv.Load();

// load the API key from the environment variable
var envVars = DotEnv.Read();
var apiType = envVars["OPENAI_API_TYPE"];

OpenAIAPI openaiApi;

if (apiType == "azure")
{
    var azureEndpoint = envVars["OPENAI_AZURE_ENDPOINT"];
    var modelDeploymentName = envVars["OPENAI_AZURE_MODEL"];
    var apiKey = envVars["OPENAI_AZURE_API_KEY"];

    if (azureEndpoint == null || modelDeploymentName == null || apiKey == null)
    {
        throw new Exception("Azure OpenAI API configuration not found. Please set the OPENAI_AZURE_ENDPOINT, OPENAI_AZURE_MODEL, and OPENAI_AZURE_API_KEY environment variables.");
    }

    openaiApi = OpenAIAPI.ForAzure(azureEndpoint, modelDeploymentName, apiKey);
}
else
{
    var apiKey = envVars["OPENAI_API_KEY"];

    if (apiKey == null)
    {
        throw new Exception("OpenAI API key not found. Please set the OPENAI_API_KEY environment variable.");
    }

    openaiApi = new OpenAIAPI(apiKey);
}

var actor = new TowerOfHanoiActor(openaiApi);
var monitor = new TowerOfHanoiMonitor(openaiApi);
var predictor = new TowerOfHanoiPredictor(openaiApi);
var orchestrator = new TowerOfHanoiOrchestrator(openaiApi);
var taskDecomposer = new TowerOfHanoiTaskDecomposer(openaiApi);
var evaluator = new TowerOfHanoiEvaluator();

var agent = new XavierCortex<TowerOfHanoiGoal, TowerOfHanoiState, TowerOfHanoiAction, int>(
actor, monitor, predictor, orchestrator, taskDecomposer, evaluator);

// Define the initial state and goal
var initialState = new TowerOfHanoiState
{
    A = new List<int> { 0, 1, 2 },
    B = new List<int>(),
    C = new List<int>()
};

var goal = new TowerOfHanoiGoal
{
    A = new List<int>(),
    B = new List<int>(),
    C = new List<int>() { 0, 1, 2 }
};

// Generate the plan
var maxPlanLength = 10;
var maxLayerDepth = 3;
var proposedActionCount = 2;

var plan = await agent.GeneratePlan(initialState, goal, maxPlanLength, maxLayerDepth, proposedActionCount);

// Print the plan
foreach (var action in plan)
{
    Console.WriteLine($"Move disk {action.Disk} from {action.Source} to {action.Target}");
}