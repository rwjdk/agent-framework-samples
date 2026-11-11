#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/AJZhHHnsFXY
// ReSharper disable UnreachableSwitchCaseDueToIntegerAnalysis


using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient azureOpenAiClient = ClientHelper.GetAzureOpenAIClient();
OpenAIClient openAiClient = new(new ApiKeyCredential(secrets.OpenAiApiKey));

ChatClientAgent azureOpenAiAgent = azureOpenAiClient.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent();
ChatClientAgent openAiAgent = openAiClient.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent();

Scenario scenario = Scenario.All3;

AgentResponse response;
switch (scenario)
{
    case Scenario.Text:
        {
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User, "What is the capital of France?"));
            ShowResponse(response);
        }
        break;

    case Scenario.Image:
        {
            //---------------------------------------------------------------------------------
            //Image via URI
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
            [
                new TextContent("What is in this image?"),
            new UriContent("https://cdn.pixabay.com/photo/2019/09/21/14/57/catan-4494043_1280.jpg", "image/jpeg")
            ]));
            ShowResponse(response);
            //---------------------------------------------------------------------------------
            //Local File
            string path = Path.Combine("SampleData", "Image.jpg");

            //Image via Base64
            string base64Image = Convert.ToBase64String(File.ReadAllBytes(path));
            string dataUri = $"data:image/jpeg;base64,{base64Image}";
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
            [
                new TextContent("What is in this image?"),
            new DataContent(dataUri, "image/jpeg")
            ]));
            ShowResponse(response);
            //---------------------------------------------------------------------------------
            //Image via Memory
            ReadOnlyMemory<byte> data = File.ReadAllBytes(path).AsMemory();
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
            [
                new TextContent("What is in this image?"),
            new DataContent(data, "image/jpeg")
            ]));
            ShowResponse(response);
            //---------------------------------------------------------------------------------
        }
        break;
    case Scenario.Pdf:
        {
            //Notes
            //- This Scenario only work on OpenAI. Not AzureOpenAI!
            //- PDFs can't be read via URI; Only 'local' data
            //---------------------------------------------------------------------------------
            string path = Path.Combine("SampleData", "catan_rules.pdf");
            //---------------------------------------------------------------------------------
            //PDF as Base64
            string base64Pdf = Convert.ToBase64String(File.ReadAllBytes(path));
            string dataUri = $"data:application/pdf;base64,{base64Pdf}";
            response = await openAiAgent.RunAsync(new ChatMessage(ChatRole.User,
            [
                new TextContent("What is the winning condition in attached PDF"),
            new DataContent(dataUri, "application/pdf")
            ]));
            ShowResponse(response);
            //---------------------------------------------------------------------------------
            //PDF as Memory
            ReadOnlyMemory<byte> data = File.ReadAllBytes(path).AsMemory();
            response = await openAiAgent.RunAsync(new ChatMessage(ChatRole.User,
            [
                new TextContent("What is the winning condition in attached PDF"),
            new DataContent(data, "application/pdf"),
        ]));
            ShowResponse(response);
            //---------------------------------------------------------------------------------
        }
        break;
    case Scenario.All3:
        {
            string pdfPath = Path.Combine("SampleData", "catan_rules.pdf");
            string imagePath = Path.Combine("SampleData", "Image.jpg");
            //---------------------------------------------------------------------------------
            //PDF as Base64
            string pdfDataUri = $"data:application/pdf;base64,{Convert.ToBase64String(File.ReadAllBytes(pdfPath))}";

            //Image via Base64
            string imageDataUri = $"data:image/jpeg;base64,{Convert.ToBase64String(File.ReadAllBytes(imagePath))}";

            response = await openAiAgent.RunAsync(new ChatMessage(ChatRole.User,
            [
                new TextContent("What is shown in the imagen and what is the winning condition in attached PDF"),
                new DataContent(pdfDataUri, "application/pdf"),
                new DataContent(imageDataUri, "image/jpeg")
            ]));
            ShowResponse(response);
        }
        break;
    default:
        throw new ArgumentOutOfRangeException();
}


void ShowResponse(AgentResponse AgentResponse)
{
    Console.WriteLine(AgentResponse);
    AgentResponse.Usage.OutputAsInformation();
}

enum Scenario
{
    Text,
    Pdf,
    Image,
    All3,
}
