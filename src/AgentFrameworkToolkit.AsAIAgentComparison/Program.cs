using AgentFrameworkToolkit.AsAIAgentComparison;
using Shared;

const string prompt = "What is the capital of France? Answer in one sentence.";

Console.Clear();
Utils.Green("1. Agent Framework alone");
await AgentFrameworkAlone.RunAsync(prompt);

Utils.Separator();
Utils.Green("2. Agent Framework Toolkit factory");
await AgentFrameworkToolkitFactory.RunAsync(prompt);

Utils.Separator();
Utils.Green("3. Agent Framework Toolkit AsAIAgent extension");
await AgentFrameworkToolkitExtensionMethod.RunAsync(prompt);
