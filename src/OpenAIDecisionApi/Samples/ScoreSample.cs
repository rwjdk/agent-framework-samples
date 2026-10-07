using System.ComponentModel;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;

namespace OpenAIDecisionApi.Samples;

public static class ScoreSample
{
    public static async Task RunAsync(string apiKey, string input)
    {
        OpenAIDecisionFactory decisionFactory = new(apiKey, "gpt-6-luna");
        Score<MovieRelevance> score = await decisionFactory.ScoreAsync<MovieRelevance>(new ScoreRequest
        {
            Question = "How strongly is this question about movies rather than music?",
            Input = input
        });

        Console.WriteLine($"Movie relevance: {score.Value:F2} (0 = music, 1 = both, 2 = movies)");
        Console.WriteLine($"Confidence: {score.Confidence:P0}");
        foreach (KeyValuePair<MovieRelevance, double> level in score.Probabilities)
        {
            Console.WriteLine($"{level.Key}: {level.Value:P0}");
        }
    }

    public enum MovieRelevance
    {
        [Description("About music, not movies")]
        Music,
        [Description("About both music and movies, such as a soundtrack")]
        Both,
        [Description("About movies, not music")]
        Movies
    }
}
