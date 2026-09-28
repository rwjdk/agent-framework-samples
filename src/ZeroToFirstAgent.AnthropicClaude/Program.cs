/* Steps:
 * 1: Get an Anthropic API Key (https://docs.claude.com/en/api/admin-api/apikeys/get-api-key)
 * 2: Add Nuget Packages (Microsoft.Agents.AI.Anthropic)
 * 3: Create an AnthropicClient and use AsAIAgent
 * * 4: Call RunAsync or RunStreamingAsync (options needed for model select)
 */

using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

const string apiKey = "todo";
const string model = "todo";
AnthropicClient client = new AnthropicClient
{
    ApiKey = apiKey
};
ChatClientAgent agent = client.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        ModelId = model, 
        MaxOutputTokens = 10_000
    }
});


AgentResponse response = await agent.RunAsync("What is the capital of France?");
Console.WriteLine(response);

Console.WriteLine("---");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}