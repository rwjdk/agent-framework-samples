using System.ClientModel;
using System.Text;
using CommunityToolkit.VectorData.InMemory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using OpenAI.Responses;
#pragma warning disable OPENAI001

#region Step 1: Prepare your Source Data

List<MyDataEntry> data =
        [
            new("Is Christmas Eve a full or half day off", "It is a full day off"),
            new("How do I register vacation?", "Go to the internal portal and under Vacation Registration (top right), enter your request. Your manager will be notified and will approve/reject the request"),
            new("What do I need to do if I'm sick?", "Inform you manager, and if you have any meetings remember to tell the affected colleagues/customers"),
            new("Where is the employee handbook?", "It is located [here](https://www.yourcompany.com/hr/handbook.pdf)"),
            new("What is the WI-FI Password at the Office?", "The Password is 'Guest42'"),
            new("Who is in charge of support?", "John Doe is in charge of support. His email is john@yourcompany.com"),
            new("I can't log in to my office account", "Take hold of Susan. She can reset your password"),
            new("When using the CRM System if get error 'index out of bounds'", "That is a known issue. Log out and back in to get it working again. The CRM team have been informed and status of ticket can be seen here: https://www.crm.com/tickets/12354"),
            new("What is the policy on buying books and online courses?", "Any training material under 20$ you can just buy.. anything higher need an approval from Richard"),
            new("Is there a bounty for find candidates for an open job position?", "Yes. 1000$ if we hire them... Have them send the application to jobs@yourcompany.com")
        ];
#endregion

#region Step 2: Configure environment

string openAiApiKey = ""; //todo - store this securely and not in source code!
string embeddingModelName = "text-embedding-3-small";
string llmModelName = "gpt-6-luna";

#endregion

#region Step 3: Install needed NuGet Packages

//todo - Microsoft.Agents.AI.OpenAI (For the Agent)
//todo - CommunityToolkit.VectorData.InMemory (For VectorStore)

#endregion

#region Step 4: Create you Embedding Generator

OpenAIClient client = new OpenAIClient(openAiApiKey);
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client.GetEmbeddingClient(embeddingModelName).AsIEmbeddingGenerator();

#endregion

#region Step 5: Create your Vector Model class

//Create C# Class with Key, Data and Vector (Add relevant attributes)

#endregion

#region Step 6: Create you VectorStore (with Embedding Generator in options) and collection (and ensure it exist)

VectorStore store = new InMemoryVectorStore(new InMemoryVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});
VectorStoreCollection<string, VectorModel> collection = store.GetCollection<string, VectorModel>("myCollection");
await collection.EnsureCollectionExistsAsync();

#endregion

#region Step 7: Embed Data

foreach (MyDataEntry entry in data)
{
    Console.WriteLine($"Embedding Q: {entry.Question}");
    VectorModel vectorModel = new VectorModel
    {
        Id = Guid.NewGuid().ToString(),
        Question = entry.Question,
        Answer = entry.Answer
    };

    await collection.UpsertAsync(vectorModel);
}

#endregion

#region Step 8: Build RAG Search Tool

async Task<string> SearchRag(string input)
{
    StringBuilder searchResult = new StringBuilder();

    await foreach (VectorSearchResult<VectorModel> result in collection.SearchAsync(input, 3))
    {
        searchResult.AppendLine($"Q: {result.Record.Question} - A: {result.Record.Answer}");
    }

    return searchResult.ToString();
}

#endregion

#region Step 9: Build and use Agent

ChatClientAgent agent = client.GetResponsesClient().AsAIAgent(llmModelName, tools: [AIFunctionFactory.Create(SearchRag)]);

AgentResponse response = await agent.RunAsync("What is the Wifi password?");
Console.WriteLine(response);

#endregion

public record MyDataEntry(string Question, string Answer);

public class VectorModel
{
    [VectorStoreKey]
    public required string Id { get; set; }

    [VectorStoreData]
    public required string Question { get; set; }

    [VectorStoreData]
    public required string Answer { get; set; }

    [VectorStoreVector(1536)]
    public string? Vector => $"""
                              <knowledgeBaseEntry>
                                 <question>{Question}</question>
                                 <answer>{Answer}</answer>
                              </knowledgeBaseEntry>
                              """;
}