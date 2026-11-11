using BuildYourOwnVectorStore;
using BuildYourOwnVectorStore.VectorStoreImplementation;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using OpenAI.Responses;
using Shared;
using System.ClientModel;
using System.Text;
#pragma warning disable OPENAI001

Console.WriteLine("Hello, World!");

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
Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
    .GetEmbeddingClient("text-embedding-3-small")
    .AsIEmbeddingGenerator();

VectorStore store = new MyVectorStore(new MyVectorStoreOptions
{
    EmbeddingGenerator = embeddingGenerator
});

VectorStoreCollection<string, VectorModel> collection = store.GetCollection<string, VectorModel>("knowledge_base");

await collection.EnsureCollectionDeletedAsync(); //In real code this should off cause not be here
await collection.EnsureCollectionExistsAsync();

int counter = 0;
foreach (MyDataEntry entry in data)
{
    counter++;
    Console.Write($"\rEmbedding Data: {counter}/{data.Count}");
    await collection.UpsertAsync(new VectorModel
    {
        Id = Guid.NewGuid().ToString(),
        Question = entry.Question,
        Answer = entry.Answer,
    });
}

Utils.Separator();

async Task<string> SearchRag(string input)
{
    StringBuilder searchResult = new();

    await foreach (VectorSearchResult<VectorModel> result in collection.SearchAsync(input, 3))
    {
        searchResult.AppendLine($"Q: {result.Record.Question} - A: {result.Record.Answer}");
    }

    return searchResult.ToString();
}

ChatClientAgent agent = client.GetResponsesClient().AsAIAgent("gpt-6-luna", tools: [AIFunctionFactory.Create(SearchRag)]);

AgentResponse response = await agent.RunAsync("What is the Wifi password?");
Console.WriteLine(response);

namespace BuildYourOwnVectorStore
{
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
                                  <knowledgebase>
                                     <question>{Question}</question>
                                     <answer>{Answer}</answer>
                                  </knowledgebase>
                                  """;

    }
}