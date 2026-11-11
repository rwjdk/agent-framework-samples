#pragma warning disable OPENAI001
using OpenAI.Responses;
using System.ClientModel;
using System.Text.Json;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using Shared.Extensions;
using UseToonToSaveTokens;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

string json = await File.ReadAllTextAsync("famous_people.json");
List<FamousPerson> list = JsonSerializer.Deserialize<List<FamousPerson>>(json)!;

string instructions = "You answer questions about famous people. Always use tool 'get_famous_people' to get data";
string question = "Tell me about Hula Johnson";

ChatClientAgent agentWithJsonTool = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: instructions,
        tools: [AIFunctionFactory.Create(GetFamousPeopleAsJson, name: "get_famous_people")]);

ChatClientAgent agentWithToonTool = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: instructions,
        tools: [AIFunctionFactory.Create(GetFamousPeopleAsToon, name: "get_famous_people")]);

Utils.Green("Ask using JSON Tool");

AgentResponse response1 = await agentWithJsonTool.RunAsync(question);
Console.WriteLine(response1);
response1.Usage.OutputAsInformation();

Utils.Separator();

Utils.Green("Ask using Toon Tool");
AgentResponse response2 = await agentWithToonTool.RunAsync(question);
Console.WriteLine(response2);
response2.Usage.OutputAsInformation();

return;

List<FamousPerson> GetFamousPeopleAsJson()
{
    string json = JsonSerializer.Serialize(list); //This is what the data is converted when given to AI
    return list;
}

string GetFamousPeopleAsToon()
{
    string toon = ToonNetSerializer.ToonNet.Encode(list);
    List<FamousPerson>? decodedAgain = ToonNetSerializer.ToonNet.Decode<List<FamousPerson>>(toon);
    return toon;
}