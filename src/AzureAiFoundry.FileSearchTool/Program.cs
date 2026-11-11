using Azure.AI.Projects;
using Azure.Identity;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using OpenAI.Responses;

#pragma warning disable OPENAI001
Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();
AIProjectClient projectClient = new(new Uri(secrets.MicrosoftFoundryEndpoint), new AzureCliCredential());
OpenAIClient client = projectClient.ProjectOpenAIClient;

OpenAIFileClient fileClient = client.GetOpenAIFileClient();
VectorStoreClient vectorStoreClient = client.GetVectorStoreClient();

string? fileId = null;
string? vectorStoreId = null;
try
{
    string filename = "secretData.pdf";
    byte[] fileBytes = await File.ReadAllBytesAsync(Path.Combine("Data", filename));
    ClientResult<OpenAIFile> uploadedFile = await fileClient.UploadFileAsync(new BinaryData(fileBytes), filename, FileUploadPurpose.UserData);
    fileId = uploadedFile.Value.Id;

    ClientResult<VectorStore> vectorStore = await vectorStoreClient.CreateVectorStoreAsync(options: new VectorStoreCreationOptions
    {
        Name = "MyVectorStore"
    });
    vectorStoreId = vectorStore.Value.Id;

    await vectorStoreClient.AddFileToVectorStoreAsync(vectorStore.Value.Id, uploadedFile.Value.Id);


    AIAgent agent = client
        .GetResponsesClient()
        .AsAIAgent(
            model: "gpt-6-luna",
            instructions: "Only use tools. Never your world-knowledge",
            tools:
            [
                new HostedFileSearchTool
                {
                    Inputs = [new HostedFileContent(uploadedFile.Value.Id), new HostedVectorStoreContent(vectorStore.Value.Id)]
                }
            ]);

    AgentResponse response = await agent.RunAsync("What is word of the day?");
    Console.Write(response);
    response.Usage.OutputAsInformation();
}
finally
{
    if (vectorStoreId != null)
    {
        await vectorStoreClient.DeleteVectorStoreAsync(vectorStoreId);
    }

    if (fileId != null)
    {
        await fileClient.DeleteFileAsync(fileId);
    }
}
