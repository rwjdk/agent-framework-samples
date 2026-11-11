using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.Decisions;
using Shared;

Utils.Init("OpenAI DecisionAPI in MS Foundry");

Secrets secrets = SecretsManager.GetSecrets();

IDecisionFactory factory = new AzureOpenAIDecisionFactory(
    endpoint: secrets.AzureOpenAiEndpoint,
    apiKey: secrets.AzureOpenAiKey,
    protocol: DecisionProtocol.OpenAI,
    deployment: "gpt-6-luna");

DecisionRequest request = new()
{
    Input = "The product arrived with a completely shattered screen and cannot be used."
};

DecisionResponse<SupportDecision> response = await factory.CreateDecisionAsync<SupportDecision>(request);

Console.WriteLine($"Damaged: {response.Result.Damaged}");
Console.WriteLine($"Category: {response.Result.Category?.Value}");
Console.WriteLine($"Severity: {response.Result.Severity?.Value}");
Console.WriteLine($"Model: {response.Model}");
Console.WriteLine($"Tokens: {response.TotalTokenCount}");

public enum Category { Damage, Billing }

public enum Severity { Cosmetic, Degraded, Blocking }

public class SupportDecision
{
    [ProbabilityQuestion("Does the customer explicitly report physical damage to the product?")]
    public bool Damaged { get; set; }

    [ChoiceQuestion<Category>("Is the report about physical damage or a billing question?")]
    public Choice<Category>? Category { get; set; }

    [ScoreQuestion<Severity>("Rate impact: Cosmetic is appearance only, Degraded is usable with limitations, Blocking is unusable.")]
    public Score<Severity>? Severity { get; set; }
}