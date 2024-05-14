using System.Text.Json;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Xavier.Tests.TowerOfHanoi;

public class TowerOfHanoiPredictor : IPredictor<TowerOfHanoiState, TowerOfHanoiAction>
{
    private OpenAIAPI _apiClient;

    public TowerOfHanoiPredictor(OpenAIAPI apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<TowerOfHanoiState> PredictNextState(TowerOfHanoiState currentState, TowerOfHanoiAction action)
    {
        var predictorPrompt = $@"Consider the following puzzle problem:

			Problem description:
			- There are three lists labeled A, B, and C.
			- There is a set of numbers distributed among those three lists.
			- You can only move numbers from the rightmost end of one list to the rightmost end of another list.
			Rule #1: You can only move a number if it is at the rightmost end of its current list.
			Rule #2: You can only move a number to the rightmost end of a list if it is larger than the other numbers in that list.
			

			Goal: The goal is to predict the configuration of the three lists, if the proposed move is applied to the current configuration.


			Here are two examples:
			
			Example 1:

			
			This is the current configuration:
			A = []
			B = [1]
			C = [0, 2]
			
			Proposed move:
			Move 2 from list C to list B.

			Answer:
			A = []
			B = [1, 2]
			C = [0]


			Example 2:

			
			This is the current configuration:
			A = []
			B = [1]
			C = [0, 2]
			
			Proposed move:
			Move 1 from list B to list A.

			Answer:
			A = [1]
			B = []
			C = [0, 2]
			


			Here is the task:

			
			This is the current configuration:
			A = [{string.Join(", ", currentState.A)}]
			B = [{string.Join(", ", currentState.B)}]
			C = [{string.Join(", ", currentState.C)}]
			Proposed move:
			{action}.

			Answer:";

        var predictorMessage = new ChatRequest()
        {
            Messages = new List<ChatMessage>
            {
                new(ChatMessageRole.System, "You are a helpful assistant."),
                new(ChatMessageRole.User, predictorPrompt),
            },
            Temperature = 0.0,
            MaxTokens = 200,
            TopP = 0,
            Model = "gpt-4-32k"
        };

        var retryAttempt = 0;

        while (retryAttempt < 10)
        {
            try
            {
                var predictorResponse = await _apiClient.Chat.CreateChatCompletionAsync(predictorMessage);
                var splits = predictorResponse.Choices[0].Message.TextContent.Split('=');
                var predictedState = new TowerOfHanoiState();
                int parsedArrayCount = 0;
                foreach (var sp in splits)
                {
                    if (sp.Contains('[') && sp.Contains(']'))
                    {
                        var parsedSubgoalPart =
                            JsonSerializer.Deserialize<int[]>(sp[sp.IndexOf('[')..(sp.IndexOf(']') + 1)]);

                        if (parsedArrayCount == 0)
                        {
                            predictedState.A = parsedSubgoalPart.ToList();
                            parsedArrayCount++;
                        }
                        else if (parsedArrayCount == 1)
                        {
                            predictedState.B = parsedSubgoalPart.ToList();
                            parsedArrayCount++;
                        }
                        else if (parsedArrayCount == 2)
                        {
                            predictedState.C = parsedSubgoalPart.ToList();
                            return predictedState;
                        }
                    }
                }
            }
            catch
            {
				// rate limits and other errors
                retryAttempt++;
            }
        }

        throw new Exception("Failed to predict the next state.");
    }
}