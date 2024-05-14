using OpenAI_API;
using OpenAI_API.Chat;

namespace Xavier.Tests.TowerOfHanoi;

public class TowerOfHanoiOrchestrator : IOrchestrator<TowerOfHanoiGoal, TowerOfHanoiState>
{

    private OpenAIAPI _openaiApi;

    public TowerOfHanoiOrchestrator(OpenAIAPI openaiApi)
    {
        _openaiApi = openaiApi;
    }

    public async Task<bool> HasGoalBeenAchieved(TowerOfHanoiState predictedState, TowerOfHanoiGoal currentGoal)
    {
        Console.WriteLine("Checking whether the predicted state of the proposed mood matches the current goal.");

        var taskCoordinationPrompt = $@"Consider the following puzzle problem:
	
	        Problem description:
	        - There are three lists labeled A, B, and C.
	        - There is a set of numbers distributed among those three lists.
	        - You can only move numbers from the rightmost end of one list to the rightmost end of another list.
	        
	        Rule #1: You can only move a number if it is at the rightmost end of its current list.
	        Rule #2: You can only move a number to the rightmost end of a list if it is larger than the other numbers in that list.


            Goal: The goal is to predict whether the current configuration matches the goal configuration or not.
            
            Here are two examples:
            
            Example 1:
            
            This is the current configuration:
            A = []
            B = []
            C = [0, 1, 2]

            This is the goal configuration:
            A = []
            B = []
            C = [0, 1, 2]
            
            Answer: 
            The current configuration matches the goal configuration. Hence yes.
            
            
            Example 2:
            
            This is the current configuration:
            A = [0, 1]
            B = [2]
            C = []

            This is the goal configuration:
            A = []
            B = []
            C = [0, 1, 2]
            
            Answer: 
            The current configuration doesn't match the goal configuration. Hence no.
            
            
            Here is the task:
            
            This is the current configuration:
            A = [{string.Join(", ", predictedState.A)}]
            B = [{string.Join(", ", predictedState.B)}]
            C = [{string.Join(", ", predictedState.C)}]

            This is the goal configuration:
            A = [{string.Join(", ", currentGoal.A)}]
            B = [{string.Join(", ", currentGoal.B)}]
            C = [{string.Join(", ", currentGoal.C)}]
            
            Answer:
        ";

        var chatRequest = new ChatRequest()
        {
            Messages = new List<ChatMessage>
            {
                new(ChatMessageRole.System, "You are a helpful assistant."),
                new(ChatMessageRole.User, taskCoordinationPrompt)
            },
            Temperature = 0.0,
            MaxTokens = 200,
            TopP = 0,
            Model = "gpt-4-32k"
        };

        var retryAttempt = 0;

        while (retryAttempt < 10)
        {
            var chatResponse = await _openaiApi.Chat.CreateChatCompletionAsync(chatRequest);

            var answer = chatResponse.Choices[0].Message.TextContent;

            if (answer.Contains("yes") || answer.Contains("Yes"))
            {
                Console.WriteLine("The predicted state of the proposed move matches the current goal.");
                return true;
            }
            else if (answer.Contains("no") || answer.Contains("No"))
            {
                Console.WriteLine("The predicted state of the proposed move does not match the current goal.");
                return false;
            }

            retryAttempt++;
        }

        // If the goal cannot be determined, return false
        return false;
    }
}