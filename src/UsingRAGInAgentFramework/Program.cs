#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/Vpi5aZJRJmA


using CommunityToolkit.VectorData.InMemory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using OpenAI.Chat;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using UsingRAGInAgentFramework.Models;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

//Prep
string jsonWithMovies = await File.ReadAllTextAsync("made_up_movies.json");
Movie[] movieDataForRag = JsonSerializer.Deserialize<Movie[]>(jsonWithMovies)!;

ChatMessage question = new(ChatRole.User, "What is the 3 highest rated adventure movies (list their titles, plots and ratings)");

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "You are an expert a set of made up movies given to you (aka don't consider movies from your world-knowledge)");

#region Let's give the model all data upfront

Utils.Green("Sample 1 - Preload all data");
List<ChatMessage> preloadEverythingChatMessages =
[
    new(ChatRole.Assistant, "Here are all the movies")
];
foreach (Movie movie in movieDataForRag)
{
    preloadEverythingChatMessages.Add(new ChatMessage(ChatRole.Assistant, movie.GetTitleAndDetails()));
}

preloadEverythingChatMessages.Add(question);

AgentResponse response1 = await agent.RunAsync(preloadEverythingChatMessages);
Console.WriteLine(response1);
response1.Usage.OutputAsInformation();

#endregion

Console.Clear();

#region That is too expensive!... Let's use RAG

Utils.Green("Sample 2 - Preload RAG Data");

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
    .GetEmbeddingClient("text-embedding-3-small")
    .AsIEmbeddingGenerator();

InMemoryVectorStore vectorStore =
    new(new InMemoryVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    });

//CommunityToolkit.VectorData.AzureAISearch.AzureAISearchVectorStore vectorStoreFromAzureAiSearch = new AzureAISearchVectorStore(
//    new SearchIndexClient(new Uri("azureAiSearchEndpoint"),
//        new AzureKeyCredential("azureAiSearchKey")
//    ));

//CommunityToolkit.VectorData.SqlServer.SqlServerVectorStore vectorStoreFromSqlServer2025 = new SqlServerVectorStore(
//    "connectionString");

//CommunityToolkit.VectorData.CosmosNoSql.CosmosNoSqlVectorStore vectorStoreFromCosmosDb = new CosmosNoSqlVectorStore(
//    "connectionString",
//    "databaseName",
//    new CosmosClientOptions
//    {
//        UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
//    });

InMemoryCollection<Guid, MovieVectorStoreRecord> collection = vectorStore.GetCollection<Guid, MovieVectorStoreRecord>("movies");
await collection.EnsureCollectionExistsAsync();

int counter = 0;
foreach (Movie movie in movieDataForRag)
{
    counter++;
    Console.Write($"\rEmbedding Movies: {counter}/{movieDataForRag.Length}");
    await collection.UpsertAsync(new MovieVectorStoreRecord
    {
        Id = Guid.NewGuid(),
        Title = movie.Title,
        Plot = movie.Plot,
        Rating = movie.Rating
    });
}

Console.WriteLine();
Console.WriteLine("\rEmbedding complete... Let's ask the question again using RAG");

List<ChatMessage> ragPreloadChatMessages =
[
    new(ChatRole.Assistant, "Here are the most relevant movies")
];
await foreach (VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question.Text, 10,
                   new VectorSearchOptions<MovieVectorStoreRecord>
                   {
                       IncludeVectors = false
                   }))
{
    MovieVectorStoreRecord record = searchResult.Record;
    ragPreloadChatMessages.Add(new ChatMessage(ChatRole.Assistant, record.GetTitleAndDetails()));
}

ragPreloadChatMessages.Add(question);

AgentResponse response2 = await agent.RunAsync(ragPreloadChatMessages);
Console.WriteLine(response2);
response2.Usage.OutputAsInformation();

#endregion

Console.Clear();

#region Let's do the same as above but as function calling [Smart if the use example just say 'Hello' we do not preload any movies]

Utils.Green("Sample 3 RAG via Tool Call");

SearchTool searchTool = new(collection);

AIAgent agentWithTools = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: "You are an expert a set of made up movies given to you (aka don't consider movies from your world-knowledge)",
        tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)]
    ).AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AgentResponse response3 = await agentWithTools.RunAsync(question);
Console.WriteLine(response3);
response3.Usage.OutputAsInformation();

async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    Utils.Gray(functionCallDetails.ToString());

    return await next(context, cancellationToken);
}

class SearchTool(InMemoryCollection<Guid, MovieVectorStoreRecord> collection)
{
    public async Task<List<string>> SearchVectorStore(string question)
    {
        List<string> result = [];
        await foreach (VectorSearchResult<MovieVectorStoreRecord> searchResult in collection.SearchAsync(question, 10,
                           new VectorSearchOptions<MovieVectorStoreRecord>
                           {
                               IncludeVectors = false
                           }))
        {
            MovieVectorStoreRecord record = searchResult.Record;
            result.Add(record.GetTitleAndDetails());
        }

        return result;
    }
}

#endregion