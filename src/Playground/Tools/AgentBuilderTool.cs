#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
using System.ComponentModel;

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;

namespace Playground.Tools;

public class AgentBuilderTool
{
    public async Task<string> RunSubAgent([Description("Give the agent a funny name")]string agentName, string systemInstructions, string prompt)
    {
        Utils.Green($"Sub-agent '{agentName}': Instructions: '{systemInstructions}' - Prompt: '{prompt}'");

        OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
        ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(instructions: systemInstructions);
        return (await agent.RunAsync(prompt)).ToString();
    }
}