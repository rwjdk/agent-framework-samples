namespace Speedtest;

public record AgentTestResult(string Provider, string Model, TestResult SimpleTest, TestResult ToolCallTest, TestResult StructuredOutputTest);

public record TestResult(string Question, List<TestRunResult> Runs)
{
    public long MinMilliseconds => Runs.Min(run => run.ElapsedMilliseconds);
    public long MaxMilliseconds => Runs.Max(run => run.ElapsedMilliseconds);
    public double AverageMilliseconds => Runs.Average(run => run.ElapsedMilliseconds);
}

public record TestRunResult(
    int Run,
    long ElapsedMilliseconds,
    string Response,
    int ToolCalls,
    long? InputTokens,
    long? OutputTokens);
