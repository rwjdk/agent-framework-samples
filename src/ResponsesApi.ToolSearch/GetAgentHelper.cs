using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable MEAI001
#pragma warning disable OPENAI001

namespace OpenAIResponsesApi.ToolSearch;

public static class GetAgentHelper
{
    private const string GitHubDescription =
        "GitHub tools for searching repositories and code, reading files, " +
        "managing issues and pull requests, reviewing changes, " +
        "and inspecting branches, commits, and workflow runs.";

    public static AIAgent WithoutSearchTool(OpenAIClient client, IEnumerable<AITool> aiTools)
    {
        return CreateAgent(client, aiTools);
    }

    public static AIAgent WithSearchTool(OpenAIClient client, IEnumerable<AITool> aiTools)
    {
        return CreateAgent(client, [.. aiTools, new HostedToolSearchTool()]);
    }

    public static AIAgent WithSearchToolsInOneNameSpace(OpenAIClient client, IEnumerable<AITool> aiTools)
    {
        return CreateAgent(client,
        [
            .. aiTools,
            new HostedToolSearchTool
            {
                Namespace = "github",
                NamespaceDescription = GitHubDescription
            }
        ]);
    }

    public static AIAgent WithSearchToolsInMultipleNamespaces(OpenAIClient client, IEnumerable<AITool> aiTools)
    {
        List<AITool> tools = aiTools.ToList();

        // Classify each tool once, so namespace memberships never overlap.
        // Adapt these GitHub-specific categories when adding tools from other services.
        List<IGrouping<(string Name, string Description), AITool>> groups = tools.GroupBy(tool => GetCategory(tool.Name)).ToList();
        foreach (IGrouping<(string Name, string Description), AITool> group in groups)
        {
            tools.Add(new HostedToolSearchTool
            {
                Namespace = group.Key.Name,
                NamespaceDescription = group.Key.Description,
                DeferredTools = group.Select(tool => tool.Name).ToList()
            });
        }

        return CreateAgent(client, tools);
    }

    private static AIAgent CreateAgent(OpenAIClient client, IEnumerable<AITool> tools)
    {
        return client.GetResponsesClient().AsAIAgent(
            instructions: "You are a GitHub Expert",
            tools: tools.ToList(),
            model: "gpt-6-luna");
    }
    
    private static (string Name, string Description) GetCategory(string toolName)
    {
        if (toolName.Contains("pull_request", StringComparison.OrdinalIgnoreCase)
            || toolName.Contains("review", StringComparison.OrdinalIgnoreCase))
        {
            return ("github_pull_requests", "GitHub pull requests: search, read, create, update, merge, inspect diffs, and manage reviews.");
        }

        if (toolName.Contains("issue", StringComparison.OrdinalIgnoreCase)
            || toolName.Contains("label", StringComparison.OrdinalIgnoreCase))
        {
            return ("github_issues", "GitHub issues: search, read, create, update, comment, and manage labels and assignments.");
        }

        if (toolName.Contains("workflow", StringComparison.OrdinalIgnoreCase)
            || toolName.Contains("actions", StringComparison.OrdinalIgnoreCase)
            || toolName.Contains("job", StringComparison.OrdinalIgnoreCase))
        {
            return ("github_actions", "GitHub Actions: inspect workflow runs, jobs, logs, and build results.");
        }

        return ("github_repositories", "Other GitHub operations: repositories, code search, files, branches, commits, tags, users, teams, and account information.");
    }
}