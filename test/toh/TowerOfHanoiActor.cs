using System.Diagnostics;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Xavier.Tests.TowerOfHanoi;

public class TowerOfHanoiActor : IActor<TowerOfHanoiState, TowerOfHanoiGoal, TowerOfHanoiAction>
{

    private OpenAIAPI _apiClient;

    public TowerOfHanoiActor(OpenAIAPI apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IEnumerable<TowerOfHanoiAction>> ProposeActions(TowerOfHanoiState state, TowerOfHanoiGoal subgoal, IEnumerable<string> feedback, int actionCount)
    {
        Console.WriteLine("[Actor] Proposing actions for current state: ");
        Console.WriteLine($"\tA: {string.Join(", ", state.A)}");
        Console.WriteLine($"\tB: {string.Join(", ", state.B)}");
        Console.WriteLine($"\tC: {string.Join(", ", state.C)}");

        Console.WriteLine("[Actor] Target Goal Configuration: ");
        Console.WriteLine($"\tA: {string.Join(", ", subgoal.A)}");
        Console.WriteLine($"\tB: {string.Join(", ", subgoal.B)}");
        Console.WriteLine($"\tC: {string.Join(", ", subgoal.C)}");

        // TODO: consider setting up a comparison with two-shot prompting in C#
        var twoShotPrompt = $@"
            Example 1:

            This is the starting configuration:
            A = [0, 1]
            B = [2]
            C = []
            This is the goal configuration:
            A = []
            B = []
            C = [0, 1, 2]
            
            Here is the sequence of minimum number of moves to reach the goal configuration from the starting configuration:

            Move 2 from B to C.
            A = [0, 1]
            B = []
            C = [2]

            Move 1 from A to B.
            A = [0]
            B = [1]
            C = [2]

            Move 2 from C to B.
            A = [0]
            B = [1, 2]
            C = []

            Move 0 from A to C.
            A = []
            B = [1, 2]
            C = [0]

            Move 2 from B to A.
            A = [2]
            B = [1]
            C = [0]

            Move 1 from B to C.
            A = [2]
            B = []
            C = [0, 1]

            Move 2 from A to C.
            A = []
            B = []
            C = [0, 1, 2]
            

            Example 2:

            This is the starting configuration::
            A = [1]
            B = [0]
            C = [2]
            This is the goal configuration:
            A = []
            B = []
            C = [0, 1, 2]

            Here is the sequence of minimum number of moves to reach the goal configuration from the starting configuration:

            Move 2 from C to A.
            A = [1, 2]
            B = [0]
            C = []

            Move 0 from B to C.
            A = [1, 2]
            B = []
            C = [0]

            Move 2 from A to B.
            A = [1]
            B = [2]
            C = [0]

            Move 1 from A to C.
            A = []
            B = [2]
            C = [0, 1]

            Move 2 from B to C.
            A = []
            B = []
            C = [0, 1, 2]
        ";

        var actorPrompt = $@"Consider the following puzzle problem:
            
            Problem description:
            - There are three lists labeled A, B, and C.
            - There is a set of numbers distributed among those three lists.
            - You can only move numbers from the rightmost end of one list to the rightmost end of another list.

            Rule #1: You can only move a number if it is at the rightmost end of its current list.
            Rule #2: You can only move a number to the rightmost end of a list if it is larger than the other numbers in that list.

            A move is valid if it satisfies both Rule #1 and Rule #2.
            A move is invalid if it violates either Rule #1 or Rule #2.
            
            Goal: The goal is to end up in the goal configuration using minimum number of moves.

            Here are two examples:

            {twoShotPrompt}

            Here is the task:

            This is the starting configuration:
            A = [{string.Join(", ", state.A)}]
            B = [{string.Join(", ", state.B)}]
            C = [{string.Join(", ", state.C)}]
            This is the goal configuration:
            A = [{string.Join(", ", subgoal.A)}]
            B = [{string.Join(", ", subgoal.B)}]
            C = [{string.Join(", ", subgoal.C)}]

            Give me only two different valid next moves possible from the starting configuration that would help in reaching the goal configuration using as few moves as possible. 
            Your answer should be in the format as below:
            1. Move <N> from <src> to <trg>.";

        var chatRequest = new ChatRequest()
        {
            Messages = new List<ChatMessage>
            {
                new(ChatMessageRole.System, "You are a helpful assistant."),
                new(ChatMessageRole.User, actorPrompt),
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
            var actorResponse = await _apiClient.Chat.CreateChatCompletionAsync(chatRequest);

            var proposedActions = new List<TowerOfHanoiAction>();

            var message = actorResponse.Choices[0].Message;

            // if the text content has multiple lines, we need to split them
            var proposedActionsRaw = message.TextContent.Split("\n");

            foreach (var action in proposedActionsRaw)
            {
                try
                {
                    // uses a regex to parse the action in the formet of "Move <N> from <src> to <trg>"
                    proposedActions.Add(new TowerOfHanoiAction(action));
                }
                catch
                {
                    break;
                }
            }

            if (proposedActions.Count == actionCount)
            {
                Console.WriteLine("[Actor] Proposed actions:");
                foreach (var action in proposedActions)
                {
                    Console.WriteLine($"\t{action}");
                }
                return proposedActions;
            }
            else
            {
                Debugger.Break();
                // todo: nudge the planner that it only proposed (C) actions instead of the requested (B) actions
            }
        }


        // TODO
        throw new Exception("Failed to generate valid actions.");
        return null;
    }
}