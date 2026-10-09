using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Toolcalling.ParallelSearch;

public static class ParallelSearch
{
    public static Task<McpClient> ConnectAsync(CancellationToken cancellationToken = default) =>
        McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("https://search.parallel.ai/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["User-Agent"] = "agent-framework-samples/ParallelSearch"
            }
        }), cancellationToken: cancellationToken);

    public static AIAgent CreateAgent(IChatClient chatClient, IEnumerable<McpClientTool> tools) =>
        chatClient.AsAIAgent(
            instructions: "Search the web for current information with web_search. " +
                          "Use web_fetch to read relevant pages. Cite source URLs in your answers.",
            tools: tools.Cast<AITool>().ToList());
}
