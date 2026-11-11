#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/Gr3S1Q9eZrc


using OpenAI;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Agents.AI;
using OpenAI.Chat;

Console.Clear();
using CustomClientHttpHandler handler = new CustomClientHttpHandler();
using HttpClient httpClient = new HttpClient(handler);

Secrets secrets = SecretsManager.GetSecrets();

/*
OpenAIClient client = new(new ApiKeyCredential(secrets.OpenAiApiKey), new OpenAIClientOptions
{
    Transport = new HttpClientPipelineTransport(httpClient)
});
*/

OpenAIClient client = new(new ApiKeyCredential(secrets.AzureOpenAiKey), new OpenAIClientOptions
{
    Endpoint = new Uri($"{secrets.AzureOpenAiEndpoint.TrimEnd('/')}/openai/v1/"),
    Transport = new HttpClientPipelineTransport(httpClient)
});


ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
    instructions: "You are a Raw Agent"
);

AgentResponse response = await agent.RunAsync("Hello");
Utils.Green("The Answer");
Console.WriteLine(response);

class CustomClientHttpHandler() : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestString = await request.Content?.ReadAsStringAsync(cancellationToken)!;
        Utils.Green($"Raw Request ({request.RequestUri})");
        Utils.Gray(MakePretty(requestString));
        Utils.Separator();
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        Utils.Green("Raw Response");
        Utils.Gray(MakePretty(responseString));
        Utils.Separator();
        return response;
    }

    private string MakePretty(string input)
    {
        JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(input);
        return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
    }
}