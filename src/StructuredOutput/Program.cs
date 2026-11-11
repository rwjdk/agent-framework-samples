#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using Microsoft.Agents.AI;
using OpenAI.Chat;
using Shared;
using StructuredOutput.Models;
using System.ClientModel;
using OpenAI;

Utils.Init("Structured Output");
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

const string question = "What are the top 10 Movies according to IMDB?";
const string instructions = "You are an expert in IMDB Lists";
const string model = "gpt-6-luna";

AIAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent(instructions);

Utils.Red("Without Structured Output 👎");
AgentResponse unstructuredResponse = await agent.RunAsync(question);
Console.WriteLine(unstructuredResponse);

Utils.Separator();

Utils.Green("With Structured Output 👍");
AgentResponse<List<Movie>> structuredResponse = await agent.RunAsync<List<Movie>>(question);

List<Movie> movies = structuredResponse.Result;

int counter = 1;
foreach (Movie movie in movies)
{
    Console.WriteLine($"{counter}: " +
                      $"{movie.Title} ({movie.YearOfRelease}) - " +
                      $"Genre: {movie.Genre} - " +
                      $"Director: {movie.Director} - " +
                      $"IMDB Score: {movie.ImdbScore}");
    counter++;
}