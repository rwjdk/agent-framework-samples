using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;
#pragma warning disable OPENAI001
Utils.Init("AI Context Provider (InlineSkills)");
OpenAIClient client = ClientHelper.GetAzureOpenAIClient(showRawCall: true);

AgentInlineSkill employeeHandBookSkill = new AgentInlineSkill(
    name: "employee-handbook",
    description: "Use when user ask HR Questions",
    instructions: """
                  # Various Human Relationship things

                  - leave resource
                  - holidays resource
                  - remaining-vacation-in-days
                  """ //This data could come from anywhere (Database, API, etc.)
);

employeeHandBookSkill.AddResource("leave", "Leave always need to be asked for 2 weeks in advance");
employeeHandBookSkill.AddResource("holidays", "We use normal US Public Holidays + Christmas Eve is a full holiday: #HoHoHo");
employeeHandBookSkill.AddScript("remaining-vacation-in-days", method: () =>
{
    Utils.Gray("Todo: Get some values in system and calculate");
    return 42;
});

AgentSkillsProvider skillsProvider = new AgentSkillsProvider([employeeHandBookSkill], new AgentSkillsProviderOptions
{
    DisableLoadSkillApproval = true,
    DisableReadSkillResourceApproval = true,
    DisableRunSkillScriptApproval = true
});

AIAgent agent = client.GetResponsesClient().AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        /*
        Tools =
        [
            AIFunctionFactory.Create(Leave, "leave-info", "HR Info about Leave"),
            AIFunctionFactory.Create(Holidays, "holiday-info", "HR Info about Holidays"),
            AIFunctionFactory.Create(VacationLeft, "vacation-days-left", "HR Info Vacation Days Left"),
        ]*/
    },
    AIContextProviders = [skillsProvider],
}, model: "gpt-6-luna").AsBuilder().Use(Utils.ToolCallingMiddleware).Build();

await Utils.RunChatLoopWithSession(agent);

//Tools test comparison
static string Leave()
{
    return "Leave always need to be asked for 2 weeks in advance";
}

static string Holidays()
{
    return "We use normal US Public Holidays + Christmas Eve is a full holiday: #HoHoHo";
}

static int VacationLeft()
{
    return 42;
}