
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using CommunityToolkit.VectorData.InMemory;
using OpenAI;
using OpenAI.Responses;
using Shared;
#pragma warning disable OPENAI001

namespace ToolsVsMcpVsSkillsVsAIContextProviders;

public class RagQuestion
{
    public async Task Run()
    {
        using CustomClientHttpHandler handler = new CustomClientHttpHandler();
        using HttpClient httpClient = new HttpClient(handler);
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

        OpenAIClient client = ClientHelper.GetAzureOpenAIClient(false); //set to true to see raw calls

        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();

        VectorStore store = new InMemoryVectorStore(new InMemoryVectorStoreOptions
        {
            EmbeddingGenerator = embeddingGenerator
        });

        VectorStoreCollection<string, VectorModel> collection = store.GetCollection<string, VectorModel>("myCollection");
        await collection.EnsureCollectionExistsAsync();

        foreach (MyDataEntry dataEntry in data)
        {
            Console.WriteLine($"Embedding q: {dataEntry.Question}");

            VectorModel vectorModel = new VectorModel
            {
                Id = Guid.NewGuid().ToString(),
                Question = dataEntry.Question,
                Answer = dataEntry.Answer
            };

            await collection.UpsertAsync(vectorModel);
        }

        //Tool
        async Task<string> SearchRag(string input)
        {
            StringBuilder searchResult = new();

            await foreach (VectorSearchResult<VectorModel> result in collection.SearchAsync(input, 3))
            {
                searchResult.AppendLine($"Q: {result.Record.Question} - A: {result.Record.Answer}");
            }

            return searchResult.ToString();
        }

        AIAgent agentWithTool = client
            .GetResponsesClient()
            .AsAIAgent("gpt-6-luna",
                instructions: "Always your 'SearchRag' tool to answer question",
                tools: [AIFunctionFactory.Create(SearchRag)]) //Add Tool
            .AsBuilder()
            .Use(ToolCallingMiddleware).Build();

        AgentResponse responseWithTool = await agentWithTool.RunAsync("What is the Wifi password?");
        Console.WriteLine(responseWithTool);

        AIAgent agentWithTextSearch = client
            .GetResponsesClient()
            .AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Instructions = "Always your 'SearchRag' tool to answer question"
                },
                AIContextProviders = [new TextSearchProvider(SearchAsync, new TextSearchProviderOptions
                {
                    //Additional settings
                    FunctionToolName = "SearchRag",
                    SearchTime = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling

                })]
            }, model: "gpt-6-luna")
            .AsBuilder()
            .Use(ToolCallingMiddleware).Build(); ;

        AgentResponse responseWithTextSearch = await agentWithTextSearch.RunAsync("What is the Wifi password?");
        Console.WriteLine(responseWithTextSearch);

        async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(string input, CancellationToken cancellationToken)
        {
            List<TextSearchProvider.TextSearchResult> results = [];
            await foreach (VectorSearchResult<VectorModel> result in collection.SearchAsync(input, 3, cancellationToken: cancellationToken))
            {
                results.Add(new TextSearchProvider.TextSearchResult
                {
                    Text = $"Q: {result.Record.Question} - A: {result.Record.Answer}"
                });
            }
            return results;
        }

        async ValueTask<object?> ToolCallingMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
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
    }

    public record MyDataEntry(string Question, string Answer);
    public class VectorModel
    {
        [VectorStoreKey] public required string Id { get; set; }

        [VectorStoreData] public required string Question { get; set; }

        [VectorStoreData] public required string Answer { get; set; }

        [VectorStoreVector(1536)]
        public string? Vector => $"""
                                  <knowledgebase>
                                     <question>{Question}</question>
                                     <answer>{Answer}</answer>
                                  </knowledgebase>
                                  """;
    }

}