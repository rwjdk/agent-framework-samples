#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
using System.ComponentModel;

using Microsoft.Agents.AI;
using ModelContextProtocol.Server;
using OpenAI;
using OpenAI.Chat;

namespace ExposeYourAgentsAsRemoteMcp;

[McpServerToolType]
public class Tools(OpenAIClient OpenAIClient)
{
    [McpServerTool(Name = "get_the_secret_word", ReadOnly = true)]
    [Description("Get the Top Secret Word")]
    public string GetTheSecretWord()
    {
        return "BananaCake";
    }

    [McpServerTool(Name = "ask_john_the_pirate", ReadOnly = true)]
    [Description("Ask John the Pirate about everything Pirate life [Kids-friendly]")]
    public async Task<string> AskJohnThePirate(string question)
    {
        ChatClientAgent agent = OpenAIClient
            .GetResponsesClient().AsIChatClient("gpt-6-luna")
            .AsAIAgent(instructions: "You are John the Pirate, answering children's questions about Pirates");

        AgentResponse response = await agent.RunAsync(question);
        return response.Text;
    }

    [McpServerTool(Name = "add_order", ReadOnly = false)]
    [Description("Add a Sales order to the ERP System")]
    public async Task<int> AddOrder(string customer, string itemToBuy, int quantity)
    {
        //Add the order to the system
        await Task.CompletedTask;
        return 42;
    }
}