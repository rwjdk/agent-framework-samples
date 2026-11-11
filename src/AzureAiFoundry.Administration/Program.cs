using Azure.AI.Projects.Agents;
using Microsoft.Agents.AI.Foundry;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using Shared;

#pragma warning disable OPENAI001

AIProjectClient projectClient = new(new Uri(SecretsManager.GetSecrets().MicrosoftFoundryEndpoint), new AzureCliCredential());

// Create a versioned agent definition; invocations use the Responses API.
string agentName = "ResponsesSample-" + Guid.NewGuid().ToString("N");
DeclarativeAgentDefinition definition = new("gpt-6-luna")
{
    Instructions = "You are a helpful assistant"
};
definition.Tools.Add(new CodeInterpreterTool(new CodeInterpreterToolContainer(new AutomaticCodeInterpreterToolContainerConfiguration())));
ProjectsAgentVersion version = projectClient.AgentAdministrationClient.CreateAgentVersion(
    agentName, new ProjectsAgentVersionCreationOptions(definition)).Value;
try
{
    FoundryAgent agent = projectClient.AsAIAgent(version);
    Console.WriteLine(await agent.RunAsync("What is the capital of France?"));
    ProjectsAgentRecord existing = projectClient.AgentAdministrationClient.GetAgent(agentName).Value;
    FoundryAgent existingAgent = projectClient.AsAIAgent(existing);
    Console.WriteLine(await existingAgent.RunAsync("What model family are you configured to use?"));
}
finally
{
    // Only remove the agent created by this sample.
    projectClient.AgentAdministrationClient.DeleteAgent(agentName);
}
