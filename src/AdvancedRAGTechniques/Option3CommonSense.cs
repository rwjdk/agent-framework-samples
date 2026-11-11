#pragma warning disable OPENAI001
using OpenAI.Responses;
using AdvancedRAGTechniques.EmbeddingOptions;
using AdvancedRAGTechniques.SearchOptions;

using CommunityToolkit.VectorData.SqlServer;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared.Extensions;
using UsingRAGInAgentFramework.Models;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AdvancedRAGTechniques;

public static class Option3CommonSense
{
    public static async Task Run(bool importData, Movie[] movieDataForRag, ChatMessage question, OpenAIClient client, SqlServerCollection<Guid, MovieVectorStoreRecord> collection)
    {
        if (importData)
        {
            await EnhanceDataEmbedding.Embed(client, collection, movieDataForRag);
        }

        ChatClientAgent intentAgent = client
            .GetResponsesClient().AsIChatClient("gpt-6-luna")
            .AsAIAgent(instructions: "You are good at inferring the intent of the user");

        AgentResponse<IntentResponse> intentResponse = await intentAgent.RunAsync<IntentResponse>(question);
        IntentResponse intent = intentResponse.Result;
        switch (intent.TypeOfQuestion)
        {
            case TypeOfQuestion.MovieGenreRanking:
            {
                List<MovieVectorStoreRecord> matchingMovies = [];
                await foreach (MovieVectorStoreRecord record in collection.GetAsync(record => record.Genre == intent.Genre, int.MaxValue))
                {
                    matchingMovies.Add(record);
                }

                MovieVectorStoreRecord[] topMovies = matchingMovies.OrderByDescending(x => x.Rating).Take(intent.NumberOfResults).ToArray();

                AIAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna")
                    .AsAIAgent(
                        instructions: $"""
                                       You are an expert a set of made up movies given to you (aka don't consider movies from your world-knowledge)
                                       You are given the data for the user's question '{question.Text}' and need to present it as if you did the answer
                                       """);

                AgentResponse response = await agent.RunAsync(string.Join(";", topMovies.Select(x => x.GetTitleAndDetails())));
                Console.WriteLine(response);
                response.Usage.OutputAsInformation();
                break;
            }
            case TypeOfQuestion.MovieGenreSearch:
            {
                EnhancedSearchTool searchTool = new(collection);
                AIAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna")
                    .AsAIAgent(
                        instructions: """
                                      You are an expert a set of made up movies given to you (aka don't consider movies from your world-knowledge)
                                      When using tools use keywords only based on the users question so it is better for similarity search
                                      When listing the movies (list their titles, plots and ratings)
                                      """,
                        tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
                    .AsBuilder()
                    .Use(Middleware.FunctionCallMiddleware)
                    .Build();

                AgentResponse response = await agent.RunAsync(question);
                Console.WriteLine(response);
                response.Usage.OutputAsInformation();
            }
                break;
            case TypeOfQuestion.GenericMovieQuestion:
            {
                OriginalSearchTool searchTool = new(collection);
                AIAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna")
                    .AsAIAgent(
                        instructions: """
                                      You are an expert a set of made up movies given to you (aka don't consider movies from your world-knowledge)
                                      When using tools use keywords only based on the users question so it is better for similarity search
                                      When listing the movies (list their titles, plots and ratings)
                                      """,
                        tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
                    .AsBuilder()
                    .Use(Middleware.FunctionCallMiddleware)
                    .Build();

                AgentResponse response = await agent.RunAsync(question);
                Console.WriteLine(response);
                response.Usage.OutputAsInformation();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public class IntentResponse
{
    public required TypeOfQuestion TypeOfQuestion { get; set; }
    public required string Genre { get; set; }
    public required int NumberOfResults { get; set; }
}

public enum TypeOfQuestion
{
    MovieGenreRanking,
    MovieGenreSearch,
    GenericMovieQuestion
}