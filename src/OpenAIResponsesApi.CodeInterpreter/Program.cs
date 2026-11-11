//YouTube video that cover this sample: https://youtu.be/Eh1D3VD-708

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

OpenAIClient client = new(secrets.OpenAiApiKey);
//NB: I was unable to get this to work with Azure OpenAI in regard to downloading files from Code Interpreter
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