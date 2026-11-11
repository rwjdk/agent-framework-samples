using System.ClientModel;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AzureAI;
using Shared;

#pragma warning disable OPENAI001
Console.Clear();

Secrets secrets = SecretsManager.GetSecrets();

AIProjectClient client = new Azure.AI.Projects.AIProjectClient(new Uri(secrets.MicrosoftFoundryEndpoint), new AzureCliCredential());

string? foundryAgentName = null;
try
{
    ClientResult<AgentVersion> foundryAgent = await client.Agents.CreateAgentVersionAsync( // <-- Reveal that version history is coming
        agentName: "MyNewTypeOfAgent",
        options: new AgentVersionCreationOptions(
            new PromptAgentDefinition("gpt-6-luna")
            {
                Instructions = "You are a nice Agent",
                /* <-- Not working for now, but the fact that this is here indicate either "bad design" or Agents will stay on OpenAI forever
                ReasoningOptions = new ResponseReasoningOptions //
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.Minimal
                }*/
            }
        )
    );
    foundryAgentName = foundryAgent.Value.Name;

    FoundryAgent agentFrameworkAgent = client.AsAIAgent(foundryAgent);

    AgentResponse response = await agentFrameworkAgent.RunAsync("What is the Capital of France");
    Console.WriteLine(response);
}
catch (Exception ex)
{
    Utils.Red("Error: " + ex);
}
finally
{
    if (foundryAgentName != null)
    {
        await client.Agents.DeleteAgentAsync(foundryAgentName);
    }
}