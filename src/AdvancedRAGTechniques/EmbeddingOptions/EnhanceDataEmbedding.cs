#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using CommunityToolkit.VectorData.SqlServer;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using UsingRAGInAgentFramework.Models;

namespace AdvancedRAGTechniques.EmbeddingOptions;

public static class EnhanceDataEmbedding
{
    public static async Task Embed(OpenAIClient client, SqlServerCollection<Guid, MovieVectorStoreRecord> collection, Movie[] movieDataForRag)
    {
        ChatClientAgent genreAgent = client
            .GetResponsesClient().AsIChatClient("gpt-6-luna") //You might get away with a cheaper model here as task is easy for AI
            .AsAIAgent("""
                           You are an expert in finding the Genre of a movie based on it's title and plot
                           Pick a single genre based on the following:
                           - Adventure
                           - Sci-Fi
                           - Comedy
                           - Horror
                           - Action
                           - Romance
                           """);

        //Delete and re-create (In real life, you most likely find delta instead)
        await collection.EnsureCollectionDeletedAsync();
        await collection.EnsureCollectionExistsAsync();
        int counter = 0;
        foreach (Movie movie in movieDataForRag)
        {
            AgentResponse<string> genreResponse = await genreAgent.RunAsync<string>($"What is the genre of this movie: {movie.GetTitleAndDetails()}?");
            string genre = genreResponse.Result;

            counter++;
            Console.Write($"\rEmbedding Movies: {counter}/{movieDataForRag.Length}");
            await collection.UpsertAsync(new MovieVectorStoreRecord
            {
                Id = Guid.NewGuid(),
                Title = movie.Title,
                Plot = movie.Plot,
                Rating = movie.Rating,
                Genre = genre,
            });
        }

        Console.WriteLine();
        Console.WriteLine("\rEmbedding complete... Let's as the question again using RAG");
    }
}