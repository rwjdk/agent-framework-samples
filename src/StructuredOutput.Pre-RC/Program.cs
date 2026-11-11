#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/BNB7zO3Uqwc


using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using StructuredOutput.Models;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

string question = "What are the top 10 Movies according to IMDB?";

//Without Structured Output
AIAgent agent1 = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "You are an expert in IMDB Lists");

AgentResponse response1 = await agent1.RunAsync(question);
Console.WriteLine(response1); //This response is not guaranteed to have a structure format; hence the need for this Structured Output feature

Utils.Separator();

//With Structured Output
ChatClientAgent agent2 = client //<--- Notice that this is not an AIAgent but have it as baseclass!
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "You are an expert in IMDB Lists");

AgentResponse<MovieResult> response2 = await agent2.RunAsync<MovieResult>(question);

MovieResult movieResult2 = response2.Result;

DisplayMovies(movieResult2);

Utils.Separator();

//More cumbersome but sometimes needed way
JsonSerializerOptions jsonSerializerOptions = new()
{
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    Converters = { new JsonStringEnumConverter() }
};

AIAgent agent3 = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "You are an expert in IMDB Lists");

ChatResponseFormatJson chatResponseFormatJson = ChatResponseFormat.ForJsonSchema<MovieResult>(jsonSerializerOptions);
AgentResponse response3 = await agent3.RunAsync(question, options: new ChatClientAgentRunOptions()
{
    ChatOptions = new ChatOptions
    {
        ResponseFormat = chatResponseFormatJson
    }
});
MovieResult movieResult3 = JsonSerializer.Deserialize<MovieResult>(response3.Text, jsonSerializerOptions)!;

DisplayMovies(movieResult3);

void DisplayMovies(MovieResult movieResult)
{
    int counter = 1;
    Console.WriteLine(movieResult.MessageBack);
    foreach (Movie movie in movieResult.Top10Movies)
    {
        Console.WriteLine($"{counter}: {movie.Title} ({movie.YearOfRelease}) - Genre: {movie.Genre} - Director: {movie.Director} - IMDB Score: {movie.ImdbScore}");
        counter++;
    }
}