#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

namespace DependencyInjection.Components.Pages;

public partial class Home(OpenAIClient OpenAIClient, [FromKeyedServices("gpt-6-luna")] IChatClient chatClient, [FromKeyedServices("gpt-6-luna")] ChatClientAgent agentInjected)
{
    private string? _question;
    private string? _answer;

    private async Task AskAi()
    {
        if (string.IsNullOrWhiteSpace(_question))
        {
            return;
        }

        ChatClientAgent agent = OpenAIClient
            .GetResponsesClient().AsIChatClient("gpt-6-luna")
            .AsAIAgent();

        //Alternatives
        //agent = chatClient.AsAIAgent();
        //agent = agentInjected;

        AgentResponse response = await agent.RunAsync(_question);
        _answer = response.Text;
        StateHasChanged();
    }
}