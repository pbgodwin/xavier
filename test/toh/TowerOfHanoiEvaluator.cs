using OpenAI_API;
using OpenAI_API.Chat;

namespace Xavier.Tests.TowerOfHanoi;


public class TowerOfHanoiEvaluator : IEvaluator<TowerOfHanoiState, TowerOfHanoiGoal, int>
{

    private OpenAIAPI _apiClient;

    public TowerOfHanoiEvaluator(OpenAIAPI apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<int> EstimateValue(TowerOfHanoiState state, TowerOfHanoiGoal goal)
    {
        Console.WriteLine("[Evaluator] Estimating the minimum number of valid moves required to reach the goal configuration from the current configuration.");

        Console.WriteLine("[Evaluator] Current Configuration:");
        Console.WriteLine($"\tA: {string.Join(", ", state.A)}");
        Console.WriteLine($"\tB: {string.Join(", ", state.B)}");
        Console.WriteLine($"\tC: {string.Join(", ", state.C)}");

        Console.WriteLine("[Evaluator] Goal Configuration:");
        Console.WriteLine($"\tA: {string.Join(", ", goal.A)}");
        Console.WriteLine($"\tB: {string.Join(", ", goal.B)}");
        Console.WriteLine($"\tC: {string.Join(", ", goal.C)}");

        var stateEvaluatorSystemPrompt = @"Consider the following puzzle problem:

            Problem description:
            - There are three lists labeled A, B, and C.
            - There is a set of numbers distributed among those three lists.
            - You can only move numbers from the rightmost end of one list to the rightmost end of another list.
            Rule #1: You can only move a number if it is at the rightmost end of its current list.
            Rule #2: You can only move a number to the rightmost end of a list if it is larger than the other numbers in that list.
            A move is valid if it satisfies both Rule #1 and Rule #2.
            A move is invalid if it violates either Rule #1 or Rule #2.

            Goal: The goal is to predict the minimum number of valid moves required to reach the goal configuration from the current configuration.

            Here are two examples:

            Example 1:

            This is the current configuration:
            A = [0, 1, 2]
            B = []
            C = []

            This is the goal configuration:
            A = []
            B = []
            C = [0, 1, 2]

            Answer:
            The minimum number of valid moves required to reach the goal configuration from the current configuration is 7.

            Example 2:

            This is the current configuration:
            A = [1, 2]
            B = [0]
            C = []

            This is the goal configuration:
            A = []
            B = []
            C = [0, 1, 2]

            Answer:
            The minimum number of valid moves required to reach the goal configuration from the current configuration is 4.

            What heuristic function can be used to estimate the minimum number of valid moves required to reach the goal configuration from a given current configuration?";

        var heuristicExplanation = @"A suitable heuristic function for this problem is the 'sum of the distances' heuristic. This heuristic estimates the minimum number of valid moves required to reach the goal configuration by calculating the sum of the distances each number needs to travel to reach its final position in the goal configuration.

                Here's how the heuristic function works:

                1. For each number in the current configuration, determine its current position (list and index) and its goal position (list and index) in the goal configuration.
                2. Calculate the distance between the current position and the goal position for each number. The distance can be calculated as the absolute difference between the indices of the current and goal positions, plus a penalty if the number needs to move to a different list.
                3. Sum the distances calculated in step 2 for all numbers.

                The heuristic function will return the sum of the distances, which is an estimate of the minimum number of valid moves required to reach the goal configuration from the current configuration.

                This heuristic is admissible because it never overestimates the cost of reaching the goal configuration. It considers the minimum number of moves required for each number to reach its goal position, without taking into account the constraints imposed by the rules of the puzzle. Therefore, the actual number of moves required to reach the goal configuration will always be greater than or equal to the heuristic value.";


        var internalConfigurationMsg = $@"
        This is the current configuration:
        A = [{string.Join(", ", state.A)}]
        B = [{string.Join(", ", state.B)}]
        C = [{string.Join(", ", state.C)}]
        This is the goal configuration:
        A = [{string.Join(", ", goal.A)}]
        B = [{string.Join(", ", goal.B)}]
        C = [{string.Join(", ", goal.C)}]

        Use the heuristic function to predict the minimum number of valid moves required to reach the goal configuration from the current configuration.

        Please provide your answer according to the heuristic function in the format as below:
        The minimum number of valid moves required to reach the goal configuration from the current configuration is <N>.";

        var chatRequest = new ChatRequest()
        {
            Messages = new List<ChatMessage>
            {
                new(ChatMessageRole.System, "You are a helpful assistant."),
                new(ChatMessageRole.User, stateEvaluatorSystemPrompt),
                new(ChatMessageRole.Assistant, heuristicExplanation),
                new(ChatMessageRole.User, internalConfigurationMsg)
            },
            Temperature = 0.0,
            MaxTokens = 500,
            TopP = 0,
            Model = "gpt-4-32k"
        };

        ChatResult response = null;
        int curTry = 0;

        while (curTry < 10)
        {
            try
            {
                response = await _apiClient.Chat.CreateChatCompletionAsync(chatRequest);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await Task.Delay(120000); // Wait for 120 seconds
                curTry++;
            }
        }

        if (response != null)
        {
            var content = response.Choices[0].Message.Content;
            var index = content.IndexOf("The minimum number of valid moves required to reach the goal configuration from the current configuration is");

            if (index != -1)
            {
                var resultString = content.Substring(index).Split(' ')[11];
                if (int.TryParse(resultString, out int result))
                {
                    Console.WriteLine($"[Evaluator] Estimated minimum number of valid moves required to reach the goal configuration from the current configuration: {result}");
                    return result;
                }
            }
        }

        // Fallback random value if no proper response is received
        if (state.A.Count + state.B.Count + state.C.Count == 3)
        {
            return new Random().Next(1, 8);
        }
        else
        {
            return new Random().Next(1, 16);
        }
    }
}