using OpenAIDecisionApi.Samples;
using Shared;

Secrets secrets = SecretsManager.GetSecrets();

Utils.Init("OpenAI Decision API");
while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        return;
    }

    //await IntentSample.RunAsync(secrets.OpenAiApiKey, input);
    //await IsTrueSample.RunAsync(secrets.OpenAiApiKey, input);
    //await ScoreSample.RunAsync(secrets.OpenAiApiKey, input);
    //await CreateDecisionSample.RunAsync(secrets.OpenAiApiKey, input);
    await ImageSample.RunAsync(secrets.OpenAiApiKey, input);


    Utils.LineSeparator();
}
