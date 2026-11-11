using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentSkills;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;
using AgentSkillsDotNet;
using Shared.Extensions;

Utils.Init("AgentSkills");

Secrets secrets = SecretsManager.GetSecrets();
AzureOpenAIAgentFactory agentFactory = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);
agentFactory.Connection.DefaultClientType = global::AgentFrameworkToolkit.OpenAI.ClientType.ResponsesApi;

AgentSkillsDotNet.AgentSkills agentSkills = new AgentSkillsFactory().GetAgentSkills("TestData\\AgentSkills");

string skillsInstructions = agentSkills.GetInstructions();

AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
{
    Model = OpenAIChatModels.Gpt6Luna,
    Instructions = $"""
                    You are an nice a AI with various skills
                    ## Skills available:
                    {skillsInstructions}

                    Only call '{AgentSkillsAsToolsOptions.DefaultReadSkillFileContentToolName}' tool
                    once you have used '{AgentSkillsAsToolsOptions.DefaultGetSpecificSkillToolName}' tool
                    """,
    Tools = [..agentSkills.GetAsTools(), AIFunctionFactory.Create(PythonRunner.RunPhytonScript, name: "execute_python")],
    RawToolCallDetails = Utils.Yellow
});

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? "";
    AgentResponse response = await agent.RunAsync(message, session);
    Console.WriteLine(response);
    Console.WriteLine();
    response.Usage.OutputAsInformation();
    Utils.Separator();
}