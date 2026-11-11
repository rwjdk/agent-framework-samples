using Azure.AI.Projects;
using Azure.Identity;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Containers;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using OpenAI.Responses;

#pragma warning disable OPENAI001
Secrets secrets = SecretsManager.GetSecrets();

AIProjectClient projectClient = new(new Uri(secrets.MicrosoftFoundryEndpoint), new AzureCliCredential());
OpenAIClient client = projectClient.ProjectOpenAIClient;
AIAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(model: "gpt-6-luna", tools: [new HostedCodeInterpreterTool()]);

string question = "Find Top 10 Countries in the world and make a Bar chart should each countries population in millions";
AgentResponse response = await agent.RunAsync(question);
foreach (ChatMessage message in response.Messages)
{
    foreach (AIContent content in message.Contents)
    {
        foreach (AIAnnotation annotation in content.Annotations ?? [])
        {
            if (annotation.RawRepresentation is ContainerFileCitationMessageAnnotation containerFileCitation)
            {
                ContainerClient containerClient = client.GetContainerClient();
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

Console.Write(response);

response.Usage.OutputAsInformation();
