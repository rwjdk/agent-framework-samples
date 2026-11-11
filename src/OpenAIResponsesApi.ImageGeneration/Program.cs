
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Images;
using OpenAI.Responses;
using ImageGenerationOptions = Microsoft.Extensions.AI.ImageGenerationOptions;

#pragma warning disable OPENAI001

Utils.Init("Responses API - Image Generation");

Secrets secrets = SecretsManager.GetSecrets();
string model = "gpt-6-luna";
string imageModel = "gpt-image-2";

#region OpenAI
Utils.Green("OpenAI");
OpenAIClient openAIClient = new OpenAIClient(new ApiKeyCredential(secrets.OpenAiApiKey));
await Execute(openAIClient.GetResponsesClient());
#endregion

#region AzureOpenAI
Utils.Green("Azure OpenAI");
using HttpClient httpClient = new();
httpClient.DefaultRequestHeaders.Add("x-ms-oai-image-generation-deployment", imageModel);
OpenAIClient client = new(
    new ApiKeyCredential(secrets.AzureOpenAiKey),
    new OpenAIClientOptions
    {
        Endpoint = new Uri($"{secrets.AzureOpenAiEndpoint.TrimEnd('/')}/openai/v1/"),
        Transport = new HttpClientPipelineTransport(httpClient)
    });

await Execute(client.GetResponsesClient());
#endregion

#region Microsoft Foundry
Utils.Green("Microsoft Foundry");
AIProjectClient foundryProjectClient = new AIProjectClient(
    new Uri(secrets.MicrosoftFoundryEndpoint),
    new AzureCliCredential(),
    new AIProjectClientOptions
    {
        Transport = new HttpClientPipelineTransport(httpClient)
    });

ProjectOpenAIClient foundryOpenAiClient = foundryProjectClient.GetProjectOpenAIClient();
//ImageClient imageClient = foundryOpenAiClient.GetImageClient(imageModel);
//ClientResult<GeneratedImage> image = await imageClient.GenerateImageAsync("A Tiger in a jungle with a party-hat"); //This does not work

await Execute(foundryOpenAiClient.GetResponsesClient());
#endregion

async Task Execute(ResponsesClient responsesClient)
{
    AIAgent agent = responsesClient
        .AsAIAgent(
            tools: [new HostedImageGenerationTool
            {
                Options = new ImageGenerationOptions
                {
                    MediaType = "image/png",
                    ModelId = imageModel,
                }
            }],
            model: model);

    //Text
    string questionPrompt = "What is the capital of France?";
    Console.WriteLine(questionPrompt);
    AgentResponse response1 = await agent.RunAsync(questionPrompt);
    Console.WriteLine(response1);

    //Image
    string imagePrompt = "Create an image of a cute dog";
    Console.WriteLine(imagePrompt);
    AgentResponse response2 = await agent.RunAsync(imagePrompt);
    foreach (ChatMessage message in response2.Messages)
    {
        foreach (AIContent content in message.Contents)
        {
            if (content is ImageGenerationToolResultContent imageGenerationToolResultContent)
            {
                foreach (AIContent aiContent in imageGenerationToolResultContent.Outputs ?? [])
                {
                    if (aiContent is DataContent dataContent)
                    {
                        string path = Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid():N}.png");
                        byte[] h = dataContent.Data.ToArray();
                        File.WriteAllBytes(path, h);

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
}