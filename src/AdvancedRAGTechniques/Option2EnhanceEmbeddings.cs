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

public static class Option2EnhanceEmbeddings
{
    public static async Task Run(bool importData, Movie[] movieDataForRag, ChatMessage question, OpenAIClient client, SqlServerCollection<Guid, MovieVectorStoreRecord> collection)
    {
        if (importData)
        {
            await EnhanceDataEmbedding.Embed(client, collection, movieDataForRag);
        }

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
}