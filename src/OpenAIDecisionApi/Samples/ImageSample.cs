using System.ComponentModel;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;
using Microsoft.Extensions.AI;

namespace OpenAIDecisionApi.Samples;

public static class ImageSample
{
    public static async Task RunAsync(string apiKey, string input)
    {
        OpenAIDecisionFactory decisionFactory = new(apiKey, "gpt-6-luna");
        DataContent image = new(
            await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "animals.png")),
            "image/png");

        // Examples: "Where is the elephant?" or "Is there a rabbit in the image?"
        bool asksLocation = await decisionFactory.IsTrueAsync(new ProbabilityImageRequest
        {
            Images = [image],
            Input = input,
            Question = "Does the user's text ask for the position of an animal? " +
                       "Treat the attached image as the context even when the text does not mention an image. " +
                       "Questions like 'where is the elephant', 'where is the rabbit?', and " +
                       "'is the cat at the top or bottom?' are location questions. " +
                       "Questions like 'is there an elephant?' are existence questions, not location questions."
        });

        if (asksLocation)
        {
            AnimalPosition position = await decisionFactory.ChooseAsync<AnimalPosition>(new ChoiceImageRequest
            {
                Images = [image],
                Input = input,
                Question = "Is the animal identified in the user's question in the top half, " +
                           "the bottom half, or not present in the image?"
            });
            Console.WriteLine($"Animal position: {position}");
        }
        else
        {
            bool isTrue = await decisionFactory.IsTrueAsync(new ProbabilityImageRequest
            {
                Images = [image],
                Input = input,
                Question = "Based on the image, is the answer to the user's yes/no question yes, or is their statement true?"
            });
            Console.WriteLine($"Image answer: {isTrue}");
        }
    }

    public enum AnimalPosition
    {
        [Description("The animal is in the top half of the image")]
        Top,
        [Description("The animal is in the bottom half of the image")]
        Bottom,
        [Description("The animal is not present in the image")]
        NotInImage
    }
}
