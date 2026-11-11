#pragma warning disable OPENAI001, SKEXP0001
using System.ClientModel;
using OpenAI.Responses;
using OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI.Chat;
using Shared;
using System.Text.Json;
using CommunityToolkit.VectorData.InMemory;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

#pragma warning disable OPENAI001
#pragma warning disable SKEXP0010

Secrets secrets = Shared.SecretsManager.GetSecrets();
string endpoint = secrets.AzureOpenAiEndpoint;
string apiKey = secrets.AzureOpenAiKey;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new MyKernelFactory(endpoint, apiKey));
builder.Services.AddAzureOpenAIEmbeddingGenerator("text-embedding-3-small", endpoint, apiKey);

WebApplication app = builder.Build();

app.MapPost("/chat", async Task<IResult> ([FromBody] ChatRequest chatRequest, MyKernelFactory kernelFactory) =>
{
    Kernel kernel = kernelFactory.GetKernel("gpt-6-luna");
    string? answer = await kernel.InvokePromptAsync<string>(chatRequest.Question);
    return Results.Ok(new ChatResponse(answer!));
});

app.MapPost("/chatWithStreaming", async Task<IResult> ([FromBody] ChatRequest chatRequest, MyKernelFactory kernelFactory) =>
{
    //Not really streaming, but simulating it
    Kernel kernel = kernelFactory.GetKernel("gpt-6-luna");

    ChatCompletionAgent agent = new()
    {
        Kernel = kernel
    };

    string answer = string.Empty;
    await foreach (AgentResponseItem<StreamingChatMessageContent> item in agent.InvokeStreamingAsync(chatRequest.Question))
    {
        answer += item.Message.Content!;
    }

    return Results.Ok(new ChatResponse(answer));
});

app.MapPost("/chatHistory", async Task<IResult> ([FromBody] ChatRequest chatRequest, MyKernelFactory kernelFactory) =>
{
    Kernel kernel = kernelFactory.GetKernel("gpt-6-luna");
    ChatCompletionAgent agent = new()
    {
        Kernel = kernel,
        Instructions = "You answer questions about People",
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
    };

    ChatHistory history = new();
    history.AddUserMessage(chatRequest.Question);
    await foreach (AgentResponseItem<ChatMessageContent> item in agent.InvokeAsync(history))
    {
        history.Add(item.Message);
    }

    history.AddUserMessage("How Tall is he?");

    await foreach (AgentResponseItem<ChatMessageContent> item in agent.InvokeAsync(history))
    {
        history.Add(item.Message);
    }

    return Results.Ok(history);
});

app.MapPost("/toolCalling", async Task<IResult> ([FromBody] ChatRequest chatRequest, MyKernelFactory kernelFactory) =>
{
    Kernel kernel = kernelFactory.GetKernel("gpt-6-luna");

    kernel.ImportPluginFromObject(new MyTools());

    ChatCompletionAgent agent = new()
    {
        Kernel = kernel,
        Instructions = "You answer questions about Weather and Time (use your tools to do so)",
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
    };

    string answer = string.Empty;
    await foreach (AgentResponseItem<ChatMessageContent> item in agent.InvokeAsync(chatRequest.Question))
    {
        answer = item.Message.Content!;
    }

    return Results.Ok(new ChatResponse(answer));
});

app.MapPost("/structuredOutput", async Task<IResult> ([FromBody] ChatRequest chatRequest, MyKernelFactory kernelFactory) =>
{
    Kernel kernel = kernelFactory.GetKernel("gpt-6-luna");

    ChatCompletionAgent agent = new()
    {
        Instructions = "You answer Movie Questions",
        Kernel = kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
        {
            ResponseFormat = typeof(MovieResult)
        })
    };

    string json = string.Empty;
    await foreach (AgentResponseItem<ChatMessageContent> item in agent.InvokeAsync(chatRequest.Question))
    {
        json = item.Message.ToString();
    }

    return Results.Ok(JsonSerializer.Deserialize<MovieResult>(json));
});

app.MapPost("/rag", async Task<IResult> ([FromBody] ChatRequest chatRequest, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, MyKernelFactory kernelFactory) =>
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

    Kernel kernel = kernelFactory.GetKernel("gpt-6-luna");

    ChatCompletionAgent agent = new ChatCompletionAgent
    {
        Instructions = "You answer Knowledge Base Questions",
        Kernel = kernel
    };

    string ragData = string.Empty;
    await foreach (VectorSearchResult<KnowledgeBaseVectorRecord> searchResult in collection.SearchAsync(
                       chatRequest.Question,
                       top: 2))
    {
        ragData += $"<knowledge>Q: {searchResult.Record.Question} - A: {searchResult.Record.Answer}</knowledge>";
    }

    string answer = string.Empty;
    await foreach (AgentResponseItem<ChatMessageContent> item in agent.InvokeAsync($"Knowledge: {ragData} - Question: {chatRequest.Question}"))
    {
        answer = item.Message.ToString();
    }

    return Results.Ok(new ChatResponse(answer));
});


app.UseHttpsRedirection();
app.Run();

record ChatRequest(string Question);

record ChatResponse(string Answer);

class MyKernelFactory(string endpoint, string apiKey)
{
    public Kernel GetKernel(string model)
    {
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(
                    new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
                    {
                        Endpoint = new Uri(endpoint.TrimEnd('/') + "/openai/v1/")
                    }).GetResponsesClient().AsIChatClient(model).AsChatCompletionService());

        //Middleware
        kernelBuilder.Services.AddSingleton<IAutoFunctionInvocationFilter, FunctionCallingFilter>();

        Kernel kernel = kernelBuilder.Build();
        return kernel;
    }
}

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

class FunctionCallingFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"Function '{context.Function.Name}' called");
        if (context.Arguments != null)
        {
            for (int i = 0; i < context.Arguments.Count; i++)
            {
                Console.WriteLine($"- Arg: {context.Arguments.ToList()[i]}");
            }
        }

        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error:" + e.Message);
            throw;
        }
    }
}

public class MyTools
{
    [KernelFunction("get_weather_for_city")]
    public WeatherResult GetWeatherForCity(string city)
    {
        //Real implementation of your tool
        return new WeatherResult(city, "Sunny", 19);
    }

    [KernelFunction("get_date_and_time_utc")]
    public DateTime GetDateAndTime()
    {
        return DateTime.UtcNow;
    }
}

public record WeatherResult(string City, string Condition, int Degrees);