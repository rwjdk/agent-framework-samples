using System.ComponentModel;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Mxc.Sdk.V1;
using Shared;

namespace MXC;

internal static class PowerShellTool
{
    [Description("Run a PowerShell script under the current MXC policy. Parameters are available as $toolParameters.")]
    public static async Task<string> RunAsync(string script, string parametersJson = "{}")
    {
        Utils.Yellow(script);

        string command = """
            $ErrorActionPreference = 'Stop'
            [Console]::OutputEncoding = [Text.Encoding]::UTF8
            $toolParameters = $env:MXC_TOOL_PARAMETERS | ConvertFrom-Json
            """ + Environment.NewLine + script;

        string requestJson = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "mxc.json"));
        JsonNode request = JsonNode.Parse(requestJson)!; //Request = What to run + Rules for the run (executed in a Windows Process Container)

        request["process"]!["commandLine"] = BuildPowerShellCommand(command);
        request["process"]!["env"] = new JsonArray($"MXC_TOOL_PARAMETERS={parametersJson}");

        ExecutionResult result = await Microsoft.Mxc.Sdk.V1.Dev.MxcContainer.RunJsonAsync(request.ToJsonString()); //Container != Docker Container

        return result.ExitCode == 0 ? result.Stdout : $"PowerShell failed (exit {result.ExitCode}). {result.Stderr}";
    }

    private static string BuildPowerShellCommand(string command)
    {
        string executable = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

        return $"\"{executable}\" -NoProfile -NonInteractive -EncodedCommand {encodedScript}";
    }
}
