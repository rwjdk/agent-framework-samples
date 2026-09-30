using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Containers;
using OpenAI.Responses;
using Shared;
using System.ClientModel;
using Azure.AI.Projects.Agents;
using Microsoft.Agents.AI.Foundry;

#pragma warning disable OPENAI001

Console.Clear();

Secrets secrets = SecretsManager.GetSecrets();

AIProjectClient client = new AIProjectClient(new Uri(secrets.MicrosoftFoundryEndpoint), new AzureCliCredential());

string modelDeploymentName = "gpt-4.1-mini";
string myAgentName = "myAgent4";
string myInstructions = "You are a nice AI";

//Step 0 (Optional): Ensure Model for Agent is Deployed
try
{
    await client.Deployments.GetDeploymentAsync(modelDeploymentName);
}
catch (ClientResultException e)
{
    if (e.Status == 404)
    {
        Console.WriteLine($"Model Deployment '{modelDeploymentName}' was not found");
        return;
    }
    else
    {
        throw;
    }
}

AIFunction localTool = AIFunctionFactory.Create(GetWeather);
//Step 1: Create/Update Agent if it does not exist
try
{
    ClientResult<ProjectsAgentRecord> clientResult = await client.AgentAdministrationClient.GetAgentAsync(agentName: myAgentName);

    //Let's ensure Agent is as we have defined by making a new version (if definition is the same nothing will happen)
    //await CreateAgent(myInstructions);
}
catch (ClientResultException e)
{
    if (e.Status == 404)
    {
        Console.WriteLine("Agent not found: Creating it");
        await CreateAgent(myInstructions);
    }
    else
    {
        throw;
    }
}

FoundryAgent agentByName = client.AsAIAgent(myAgentName, tools: [localTool]);

AgentResponse response = await agentByName.RunAsync("Hi there");
Console.WriteLine(response);

response = await agentByName.RunAsync("What options do the AddCardAsync method in 'TrelloDotNet' (use tools)");
Console.WriteLine(response);

response = await agentByName.RunAsync("What is 23434343*3434343/2323232 (use tools to calculate)");
Console.WriteLine(response);

response = await agentByName.RunAsync("Make a jpg image with graph listing population of the top 10 US States in year 2000");
await GetAndLaunchCodeInterpreterGeneratedFile(response, client);

response = await agentByName.RunAsync("What is the biggest news story today?");
Console.WriteLine(response);

//Let's make a V2 with new instructions
await CreateAgent("Speak like a pirate");

FoundryAgent agentV2 = client.AsAIAgent(myAgentName, tools: [localTool]);
response = await agentV2.RunAsync("Hi there");
Console.WriteLine(response);

ProjectsAgentVersion agentV1 = (await client.AgentAdministrationClient.GetAgentVersionAsync(myAgentName, "1")).Value;
FoundryAgent agentByVersion = client.AsAIAgent(agentV1, tools: [localTool]);

response = await agentByVersion.RunAsync("Hi Agent 1");
Console.WriteLine(response);

return;

async Task CreateAgent(string instructions)
{
    await client.AgentAdministrationClient.CreateAgentVersionAsync(
        agentName: myAgentName,
        options: new ProjectsAgentVersionCreationOptions(
            new DeclarativeAgentDefinition(modelDeploymentName)
            {
                Tools =
                {
                    new CodeInterpreterTool(new CodeInterpreterToolContainer(new AutomaticCodeInterpreterToolContainerConfiguration())),
                    new WebSearchTool(),
                    localTool.AsOpenAIResponseTool(),
                    new McpTool("TrelloDotNetToolAssistant", new Uri("https://trellodotnetassistantbackend.azurewebsites.net/runtime/webhooks/mcp?code=Tools"))
                    {
                        ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(new DefaultMcpToolCallApprovalPolicy("never"))
                    },
                },
                Instructions = instructions,

                ReasoningOptions = new ResponseReasoningOptions
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
                }
            }
        )
    );
}

async Task GetAndLaunchCodeInterpreterGeneratedFile(AgentResponse agentResponse, AIProjectClient aiProjectClient)
{
    foreach (ChatMessage message in agentResponse.Messages)
    {
        foreach (AIContent content in message.Contents)
        {
            foreach (AIAnnotation annotation in content.Annotations ?? [])
            {
                if (annotation.RawRepresentation is ContainerFileCitationMessageAnnotation containerFileCitation)
                {
                    ContainerClient containerClient = aiProjectClient.ProjectOpenAIClient.GetContainerClient();
                    ClientResult<BinaryData> fileContent = await containerClient.DownloadContainerFileAsync(containerFileCitation.ContainerId, containerFileCitation.FileId);
                    string path = Path.Combine(Path.GetTempPath(), containerFileCitation.Filename);
                    await File.WriteAllBytesAsync(path, fileContent.Value.ToArray());
                    await Task.Factory.StartNew(() =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                    });
                }
            }
        }
    }
}

static string GetWeather(string city)
{
    return "It is Sunny and 19 Degrees";
}