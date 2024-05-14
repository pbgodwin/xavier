using System.Diagnostics;
using System.Text.Json;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Xavier.Tests.TowerOfHanoi;

public class TowerOfHanoiTaskDecomposer : ITaskDecomposer<TowerOfHanoiState, TowerOfHanoiGoal>
{
    private OpenAIAPI _apiClient;

    public TowerOfHanoiTaskDecomposer(OpenAIAPI apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<TowerOfHanoiGoal>> DecomposeTask(TowerOfHanoiState state, TowerOfHanoiGoal goal)
    {
		Console.WriteLine("[TaskDecomposer] Planning subgoals");

        var subgoalPrompt = $@"Consider the following puzzle problem:
			
	        Problem description:
	        - There are three lists labeled A, B, and C.
	        - There is a set of numbers distributed among those three lists.
	        - You can only move numbers from the rightmost end of one list to the rightmost end of another list.
	        
	        Rule #1: You can only move a number if it is at the rightmost end of its current list.
	        Rule #2: You can only move a number to the rightmost end of a list if it is larger than the other numbers in that list.
	        
	        
	        A move is valid if it satisfies both Rule #1 and Rule #2.
	        A move is invalid if it violates either Rule #1 or Rule #2.

	        Goal: The goal is to generate a single subgoal from the current configuration, that helps in reaching the goal configuration using minimum number of moves.
	        
	        To generate subgoal use the goal recursion strategy. First if the smallest number isn't at the correct position in list C, then set the subgoal of moving the smallest number to its correct position in list C.
            But before that, the numbers larger than the smallest number and present in the same list as the smallest number must be moved to a list other than list C. 
            This subgoal is recursive because in order to move the next smallest number to the list other than list C, the numbers larger than the next smallest number and present in the same list as the next smallest number must be moved to a list different from the previous other list and so on.
            
	        Note in the subgoal configuration all numbers should always be in ascending order in all the three lists.

	        Here are two examples:
	        
	        Example 1:
	        
	        This is the current configuration:
	        A = [0,1]
	        B = [2]
	        C = []
	        
	        This is the goal configuration:
	        A = []
	        B = []
	        C = [0, 1, 2]

	        Answer:
	        I need to move 0 from list A to list C. 
	        Step 1. Find the numbers to the right of 0 in list A. There is 1 to the right of 0.
	        Step 2. Find the numbers larger than 0 in list C. There are none.
	        I will move the numbers found in Step 1 and Step 2 to list B. Hence I will move 1 from list A to list B. Also numbers should be in ascending order in list B.
	        Subgoal:
	        A = [0]
	        B = [1, 2]
	        C = []
	        
	        Example 2:
	        
	        This is the current configuration:
	        A = [1]
	        B = [0]
	        C = [2]
	        
	        This is the goal configuration:
	        A = []
	        B = []
	        C = [0, 1, 2]

	        Answer:
	        I need to move 0 from list B to list C. 
	        Step 1. Find the numbers to the right of 0 in list B. There are none.
	        Step 2. Find the numbers larger than 0 in list C. There is 2 which is larger than 0.
	        I will move the numbers found in Step 1 and Step 2 to list A. Hence, I will move 2 from list C to list A. Also numbers should be in ascending order in list A.
	        Subgoal:
	        A = [1, 2]
	        B = [0]
	        C = []    
	          

	        Here is the task:   
	        
	        This is the current configuration:
	        A = [{string.Join(", ", state.A)}]
	        B = [{string.Join(", ", state.B)}]
            C = [{string.Join(", ", state.C)}]
	        
	        
	        This is the goal configuration:
	        A = [{string.Join(", ", goal.A)}]
            B = [{string.Join(", ", goal.B)}]
            C = [{string.Join(", ", goal.C)}]
	        
	        Answer:
        ";

        var chatRequest = new ChatRequest()
        {
            Messages = new List<ChatMessage>
            {
                new(ChatMessageRole.System, "You are a helpful assistant."),
                new(ChatMessageRole.User, subgoalPrompt),
            },
            Temperature = 0.0,
            MaxTokens = 200,
            TopP = 0,
            Model = "gpt-4-32k"
        };

        var retryAttempt = 0;

        while (retryAttempt < 10)
        {
            var decomposerResponse = await _apiClient.Chat.CreateChatCompletionAsync(chatRequest);
            var decomposerContent = decomposerResponse.Choices[0].Message.Content;

            Console.WriteLine($"[TaskDecomposer] Subgoal generated: {decomposerContent}");

            var splits = decomposerContent.Split('=');
            var parsedSubgoal = new TowerOfHanoiGoal();
            int parsedArrayCount = 0; 
            foreach (var sp in splits)
            {
                if (sp.Contains('[') && sp.Contains(']'))
                {
                    var parsedSubgoalPart =
                        JsonSerializer.Deserialize<int[]>(sp[sp.IndexOf('[')..(sp.IndexOf(']') + 1)]);

                    if (parsedArrayCount == 0)
                    {
                        parsedSubgoal.A = parsedSubgoalPart.ToList();
                        parsedArrayCount++;
                    }
                    else if (parsedArrayCount == 1)
                    {
						parsedSubgoal.B = parsedSubgoalPart.ToList();
                        parsedArrayCount++;
                    }
                    else if (parsedArrayCount == 2)
					{ 
						parsedSubgoal.C = parsedSubgoalPart.ToList();
						return new List<TowerOfHanoiGoal> { parsedSubgoal };
                    }
                }

                retryAttempt++;
            }

        }

        throw new Exception("Failed to generate subgoal?");
    }
}