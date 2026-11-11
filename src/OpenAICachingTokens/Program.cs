#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

Console.Clear();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

string text = "2+2 = 4, 10*10 = 100";
text = File.ReadAllText("Book.txt");
AIAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
    instructions: "Your only knowledge is the text: " + text
    );

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine(response);

    Utils.Yellow("Token Usages (All gpt-6-luna prices are per 1 Million tokens as of 16th of March 2026)");
    Utils.Gray("- Input       ($0.25)            : " + response.Usage!.InputTokenCount);
    Utils.Gray("  - Cached    ($0.025)           : " + response.Usage!.CachedInputTokenCount);
    Utils.Gray("- Output      ($2.00)            : " + response.Usage!.OutputTokenCount);
    Utils.Gray("  - Reasoning (billed as output) : " + response.Usage!.ReasoningTokenCount);
    Utils.Separator();
}