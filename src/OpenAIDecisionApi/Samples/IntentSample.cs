using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;
using Microsoft.Agents.AI;
using Shared;

namespace OpenAIDecisionApi.Samples;

public static class IntentSample
{
    public static async Task RunAsync(string apiKey, string input)
    {
        OpenAIAgentFactory agentFactory = new(apiKey);

        OpenAIDecisionFactory decisionFactory = new(apiKey, "gpt-6-luna");

        Intent agentChoice = await decisionFactory.ChooseAsync<Intent>(new ChoiceRequest
        {
            Question = "Is this a question about music, movies, or something else?",
            Input = input
        });
        Console.WriteLine($"Agent decision: {agentChoice}");

        switch (agentChoice)
        {
            case Intent.MusicQuestion:
                Utils.Green("Music Question");
                OpenAIAgent musicNerd = agentFactory.CreateAgent(new AgentOptions
                {
                    Model = "gpt-6-luna",
                    Instructions = "You are a music nerd. Answer music questions in at most 200 characters."
                });

                Console.WriteLine(await musicNerd.RunAsync(input));
                break;
            case Intent.MovieQuestion:
                Utils.Green("Movie Question");
                OpenAIAgent movieNerd = agentFactory.CreateAgent(new AgentOptions
                {
                    Model = "gpt-6-luna",
                    Instructions = "You are a movie nerd. Answer movie questions in at most 200 characters."
                });
                Console.WriteLine(await movieNerd.RunAsync(input));
                break;
            case Intent.Other:
                Utils.Green("Other Question");
                OpenAIAgent other = agentFactory.CreateAgent(new AgentOptions
                {
                    Model = "gpt-6-luna",
                    Instructions = "You are a general nerd."
                });
                Console.WriteLine(await other.RunAsync(input));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public enum Intent
    {
        MusicQuestion,
        MovieQuestion,
        Other
    }
}
