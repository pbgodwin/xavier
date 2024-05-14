using OpenAI_API;
using OpenAI_API.Chat;

namespace Xavier.Tests.TowerOfHanoi;

public class TowerOfHanoiMonitor : IMonitor<TowerOfHanoiState, TowerOfHanoiAction>
{

    private OpenAIAPI _apiClient;

    public TowerOfHanoiMonitor(OpenAIAPI apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<(bool validity, string feedback)> AssessValidity(TowerOfHanoiState state, IEnumerable<TowerOfHanoiAction> action)
    {
        Console.WriteLine("[Monitor] Assessing the validity of the Actor's proposed move...");

        var moveValidityPrompt = $@"Consider the following puzzle problem:
            
            Problem description:
		    - There are three lists labeled A, B, and C.
		    - There is a set of numbers distributed among those three lists.
		    - You can only move numbers from the rightmost end of one list to the rightmost end of another list.
		    Rule #1: You can only move a number if it is at the rightmost end of its current list.
		    Rule #2: You can only move a number to the rightmost end of a list if it is larger than the other numbers in that list.
		    A move is valid if it satisfies both Rule #1 and Rule #2.
		    A move is invalid if it violates either Rule #1 or Rule #2.

		    Goal: The goal is to check if the proposed move satisfies or violates Rule #1 and Rule #2 and based on that if it is a valid or invalid move.

		    Here are two examples:
		    
		    Example 1:

		    This is the initial configuration:
		    A = []
		    B = [1]
		    C = [0, 2]

		    Proposed move:
		    Move 0 from C to B.

		    Answer:
		    First check whether the move satisfies or violates Rule #1. Index of 0 in list C is 0. Length of list C is 2. The difference in length of list C and index of 0 in list C is 2, which is not equal to 1. Hence 0 is not at the rightmost end of list C, and the move violates Rule #1.
		    Next check whether the move satisfies or violates Rule #2. For that compute the maximum of list B, to which 0 is moved. Maximum of list B is 1. 0 is not larger than 1. Hence the move violates Rule #2.
		    Since the Move 0 from list C to list B violates both Rule #1 and Rule #2, it is invalid.

		    Example 2:

		    This is the initial configuration:
		    A = []
		    B = [1]
		    C = [0, 2]

		    Proposed move:
		    Move 2 from C to B.

		    Answer:
		    First check whether the move satisfies or violates Rule #1. Index of 2 in list C is 1. Length of list C is 2. The difference in length of list C and index of 2 in list C is 1. Hence 2 is at the rightmost end of list C, and the move satisfies Rule #1.
		    Next check whether the move satisfies or violates Rule #2. For that compute the maximum of list B, to which 2 is moved. Maximum of list B is 1. 2 is larger than 1. Hence the move satisfies Rule #2.
		    Since the Move 2 from list C to list B satisfies both Rule #1 and Rule #2, it is valid.

		    
		    Here is the task:

		    This is the initial configuration:    
            A = [{string.Join(", ", state.A)}]
            B = [{string.Join(", ", state.B)}]
            C = [{string.Join(", ", state.C)}]

            Proposed move:
            {action}
        ";

        var chatRequest = new ChatRequest()
        {
            Messages = new List<ChatMessage>
            {
                new(ChatMessageRole.System, "You are a helpful assistant."),
                new(ChatMessageRole.User, moveValidityPrompt),
            },
            Temperature = 0.0,
            MaxTokens = 500,
            TopP = 0,
            Model = "gpt-4-32k"
        };

        var retryAttempt = 0;

        while (retryAttempt < 10)
        {
            retryAttempt++;
            var monitorCompletionResponse = await _apiClient.Chat.CreateChatCompletionAsync(chatRequest);
			var charListResponse = monitorCompletionResponse.Choices[0].Message.TextContent.Split('\n').Last().Replace(".", "").Split(" ");
            if (charListResponse.Contains("invalid"))
            {
                var feedback = monitorCompletionResponse.Choices[0].Message.TextContent;
                Console.WriteLine("[Monitor] The proposed move is invalid. Retrying with proposed feedback: ");
                Console.WriteLine($"[Monitor] {feedback}");
                return (false, feedback);
            }
            else
            {
                Console.WriteLine("[Monitor] The proposed move is valid. Continuing.");
                return (true, "");
            }
        }

		// todo: have the Monitor give feedback to the Actor
        // note: it might be sufficient to take the last GPT response as feedback...?
        // that may be how the python code is doing it

        return (false, "");
    }
}