#pragma warning disable OPENAI001
using OpenAI.Responses;
using AgentSkills;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;

#pragma warning disable MAAI001
Utils.Init("AI Context Provider (AgentSkills)");
OpenAIClient client = ClientHelper.GetAzureOpenAIClient(showRawCall: false);

string skillPath = "TestData\\AgentSkills";

AIAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [new AgentSkillsProvider(skillPath, options: new AgentSkillsProviderOptions
    {
        DisableReadSkillResourceApproval = true,
        DisableRunSkillScriptApproval = true,
        DisableLoadSkillApproval = true,
    })],
    ChatOptions = new ChatOptions
    {
        Tools = [AIFunctionFactory.Create(PythonRunner.RunPhytonScript, name: "execute_python")]
    }
}).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();


await Utils.RunChatLoopWithSession(agent);