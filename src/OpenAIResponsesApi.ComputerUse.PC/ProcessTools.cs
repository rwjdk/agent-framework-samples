using System.ComponentModel;
using System.Diagnostics;

namespace ComputerUse;

internal static class ProcessTools
{
    [Description("Launch a Windows application using Process.Start. After launch, request a screenshot with the native computer tool before interacting with the application.")]
    public static async Task<string> StartProcess(
        [Description("Executable name or full path, such as calc.exe or notepad.exe.")] string fileName,
        [Description("Command-line arguments; use an empty array when none are needed.")] string[] arguments)
    {
        Console.WriteLine($"Starting process: {fileName}");
        ProcessStartInfo startInfo = new(fileName) { UseShellExecute = true };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using Process? process = Process.Start(startInfo);
        await Task.Delay(1000);
        return $"Started {fileName}.";
    }
}

