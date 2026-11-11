#pragma warning disable OPENAI001
using OpenAI.Responses;

using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using Shared;
using System.ClientModel;
using System.Text;
using CommunityToolkit.VectorData.InMemory;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

#pragma warning disable OPENAI001
#pragma warning disable SKEXP0010

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(ClientHelper.GetAzureOpenAIClient());
builder.Services.AddEmbeddingGenerator(provider =>
{
    OpenAIClient openAIClient = provider.GetRequiredService<OpenAIClient>();
    return openAIClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();
});

WebApplication app = builder.Build();

app.MapPost("/chat", async Task<IResult> ([FromBody] ChatRequest chatRequest, OpenAIClient client) =>
{
    ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent();
    AgentResponse response = await agent.RunAsync(chatRequest.Question);
    return Results.Ok(new ChatResponse(response.Text));
});

app.MapPost("/chatWithStreaming", async Task<IResult> ([FromBody] ChatRequest chatRequest, OpenAIClient client) =>
{
    //Not really streaming, but simulating it
    ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
        instructions: "You are a nice AI");

    string answer = string.Empty;
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(chatRequest.Question))
    {
        answer += update;
    }

    return Results.Ok(new ChatResponse(answer));
});

app.MapPost("/chatHistory", async Task<IResult> ([FromBody] ChatRequest chatRequest, OpenAIClient client) =>
{
    ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
        instructions: "You answer questions about People");

    AgentSession session = await agent.CreateSessionAsync();

    AgentResponse response1 = await agent.RunAsync(chatRequest.Question, session);

    AgentResponse response2 = await agent.RunAsync("how tall is he?", session);

    IList<ChatMessage>? messagesInThread = session.GetService<IList<ChatMessage>>();
    return Results.Ok(messagesInThread);
});

app.MapPost("/toolCalling", async Task<IResult> ([FromBody] ChatRequest chatRequest, OpenAIClient client) =>
{
    MyTools myTools = new();

    AIAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
        instructions: "You answer questions about Weather and Time (use your tools to do so)",
        tools:
        [
            AIFunctionFactory.Create(myTools.GetWeatherForCity, "get_weather_for_city"),
            AIFunctionFactory.Create(myTools.GetDateAndTime, "get_date_and_time_utc"),
        ]).AsBuilder().Use(FunctionCallMiddleware).Build();

    AgentResponse response = await agent.RunAsync(chatRequest.Question);
    return Results.Ok(new ChatResponse(response.Text));
});

app.MapPost("/structuredOutput", async Task<IResult> ([FromBody] ChatRequest chatRequest, OpenAIClient client) =>
{
    ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
        instructions: "You answer Movie Questions"
    );
    AgentResponse<MovieResult> response = await agent.RunAsync<MovieResult>();
    return Results.Ok(response.Result);
});

app.MapPost("/rag", async Task<IResult> ([FromBody] ChatRequest chatRequest, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, OpenAIClient client) =>
{
    //RAG Ingest Would normally not happen in the call itself, but done for demo
    List<KnowledgeBaseEntry> knowledgeBase =
    [
        new("What is the guest Wifi Password?", "The guest wifi password is '123qwe' (all lowercase letters)"),
        new("Is Christmas Eve a full or half day off", "It is a full day off"),
        new("How do I register vacation?", "Go to the internal portal and under Vacation Registration (top right), enter your request. Your manager will be notified and will approve/reject the request"),
        new("What do I need to do if I'm sick?", "Inform you manager, and if you have any meetings remember to tell the affected colleagues/customers"),
        new("Where is the employee handbook?", "It is located [here](https://www.yourcompany.com/hr/handbook.pdf)"),
        new("Who is in charge of support?", "John Doe is in charge of support. His email is john@yourcompany.com"),
        new("I can't log in to my office account", "Take hold of Susan. She can reset your password"),
        new("When using the CRM System if get error 'index out of bounds'", "That is a known issue. Log out and back in to get it working again. The CRM team have been informed and status of ticket can be seen here: https://www.crm.com/tickets/12354"),
        new("What is the policy on buying books and online courses?", "Any training material under 20$ you can just buy.. anything higher need an approval from Richard"),
        new("Is there a bounty for find candidates for an open job position?", "Yes. 1000$ if we hire them... Have them send the application to jobs@yourcompany.com")
    ];

    InMemoryVectorStore vectorStore = new(new InMemoryVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    });

    InMemoryCollection<Guid, KnowledgeBaseVectorRecord> collection = vectorStore.GetCollection<Guid, KnowledgeBaseVectorRecord>("knowledge");
    await collection.EnsureCollectionExistsAsync();

    foreach (KnowledgeBaseEntry entry in knowledgeBase)
    {
        await collection.UpsertAsync(new KnowledgeBaseVectorRecord
        {
            Id = Guid.NewGuid(),
            Question = entry.Question,
            Answer = entry.Answer,
        });
    }

    ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
        instructions: "You answer Knowledge Base Questions"
    );

    string ragData = string.Empty;
    await foreach (VectorSearchResult<KnowledgeBaseVectorRecord> searchResult in collection.SearchAsync(
                       chatRequest.Question,
                       top: 2))
    {
        ragData += $"<knowledge>Q: {searchResult.Record.Question} - A: {searchResult.Record.Answer}</knowledge>";
    }

    AgentResponse response = await agent.RunAsync($"Knowledge: {ragData} - Question: {chatRequest.Question}");

    return Results.Ok(new ChatResponse(response.Text));
});


app.UseHttpsRedirection();
app.Run();

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

record ChatRequest(string Question);

record ChatResponse(string Answer);

public record MovieResult(List<Movie> Movies);

public record Movie(string Title, string Director, int YearOfRelease);

record KnowledgeBaseEntry(string Question, string Answer);

public class KnowledgeBaseVectorRecord
{
    [VectorStoreKey]
    public required Guid Id { get; set; }

    [VectorStoreData]
    public required string Question { get; set; }

    [VectorStoreData]
    public required string Answer { get; set; }

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Flat)]
    public string Vector => $"Q: {Question} - A: {Answer}";
}

public class MyTools
{
    public WeatherResult GetWeatherForCity(string city)
    {
        //Real implementation of your tool
        return new WeatherResult(city, "Sunny", 19);
    }

    public DateTime GetDateAndTime()
    {
        return DateTime.UtcNow;
    }
}

public record WeatherResult(string City, string Condition, int Degrees);