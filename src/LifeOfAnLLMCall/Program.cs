#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/Gr3S1Q9eZrc


using LifeOfAnLLMCall;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Net;
using System.Text.Json;

Console.Clear();
using CustomClientHttpHandler handler = new CustomClientHttpHandler();
using HttpClient httpClient = new HttpClient(handler);

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = new(new ApiKeyCredential(secrets.AzureOpenAiKey), new OpenAIClientOptions
{
    Endpoint = new Uri($"{secrets.AzureOpenAiEndpoint.TrimEnd('/')}/openai/v1/"),
    Transport = new HttpClientPipelineTransport(httpClient)

});


ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: "Speak like a pirate!",
        tools: [AIFunctionFactory.Create(Tools.GetWeather)]
    );

AgentResponse<WeatherResponse> response = await agent.RunAsync<WeatherResponse>("What is the Weather like in Paris?");
WeatherResponse result = response.Result;
Console.WriteLine(response);

class WeatherResponse
{
    [Description("City (and country in parentheses)")]
    public required string City { get; set; }

    public required string Condition { get; set; }
    public required int DegreesFahrenheit { get; set; }
    public required int DegreesCelsius { get; set; }
}

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