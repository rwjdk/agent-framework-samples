using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;

namespace OpenAIDecisionApi.Samples;

public static class IsTrueSample
{
    public static async Task RunAsync(string apiKey, string input)
    {
        OpenAIDecisionFactory decisionFactory = new(apiKey, "gpt-6-luna");
        
        ProbabilityRequest request = new ProbabilityRequest
        {
            Question = "Is this a question about a movie?",
            Input = input,
            Threshold = 0.7
        };
        bool isMovieQuestion = await decisionFactory.IsTrueAsync(request);

        Console.WriteLine($"Movie question (at least 70% probability): {isMovieQuestion}");

        Probability probability = await decisionFactory.ProbabilityAsync(request);
        Console.WriteLine($"Movie question probability: {probability.Value}");
    }
}
