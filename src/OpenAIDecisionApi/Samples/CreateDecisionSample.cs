using System.ComponentModel;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.OpenAI.Decisions;

namespace OpenAIDecisionApi.Samples;

public static class CreateDecisionSample
{
    public static async Task RunAsync(string apiKey, string input)
    {
        OpenAIDecisionFactory decisionFactory = new(apiKey, "gpt-6-luna");
        OpenAIDecisionResponse<CustomerSupportDecision> response =
            await decisionFactory.CreateDecisionAsync<CustomerSupportDecision>(new DecisionRequest
            {
                Input = input
            });

        CustomerSupportDecision result = response.Result;
        Choice<SupportDepartment> department = result.Department!;
        Probability technicalSupport = result.NeedsTechnicalSupport!;
        Score<SupportSeverity> severity = result.Severity!;

        Console.WriteLine($"Department (enum): {result.SelectedDepartment}");
        Console.WriteLine($"Needs technical support (bool): {result.IsTechnicalSupport}");
        Console.WriteLine($"Severity score (decimal): {result.SeverityScoreDecimal:F2} (0 = minor, 1 = degraded, 2 = blocking)");

        Console.WriteLine();
        Console.WriteLine("Department details:");
        Console.WriteLine($"  Choice<SupportDepartment> value: {department.Value}");
        Console.WriteLine($"  Confidence: {department.Confidence:P2}");
        foreach (KeyValuePair<SupportDepartment, double> option in department.Probabilities)
        {
            Console.WriteLine($"  {option.Key}: {option.Value:P2}");
        }

        Console.WriteLine();
        Console.WriteLine("Technical support details:");
        Console.WriteLine($"  Probability (double): {result.TechnicalSupportProbability:P2}");
        Console.WriteLine($"  Probability (decimal): {result.TechnicalSupportProbabilityDecimal:P2}");
        Console.WriteLine($"  Probability value: {technicalSupport.Value:P2}");
        Console.WriteLine($"  Probability.IsTrue: {technicalSupport.IsTrue}");
        Console.WriteLine($"  Threshold: {technicalSupport.Threshold:P2}");

        Console.WriteLine();
        Console.WriteLine("Severity details:");
        Console.WriteLine($"  Score (double): {result.SeverityScore:F2}");
        Console.WriteLine($"  Score<SupportSeverity> value: {severity.Value:F2}");
        Console.WriteLine($"  Confidence: {severity.Confidence:P2}");
        foreach (KeyValuePair<SupportSeverity, double> level in severity.Probabilities)
        {
            Console.WriteLine($"  {level.Key}: {level.Value:P2} - {severity.LevelDescriptions[level.Key]}");
        }

        Console.WriteLine();
        Console.WriteLine($"Model: {response.Model}");
        Console.WriteLine($"Tokens: {response.InputTokenCount} input, {response.OutputTokenCount} output, {response.TotalTokenCount} total");
    }

    public class CustomerSupportDecision
    {
        // Choice questions support a direct enum or Choice<TEnum> with probabilities.
        [ChoiceQuestion<SupportDepartment>("Which customer support department should handle this request?")]
        public SupportDepartment SelectedDepartment { get; set; }

        [ChoiceQuestion<SupportDepartment>("Which customer support department should handle this request?")]
        public Choice<SupportDepartment>? Department { get; set; }

        // Predicate questions support bool, double, decimal, or Probability.
        [ProbabilityQuestion("Does the customer need technical help with a product or service?", threshold: 0.7)]
        public bool IsTechnicalSupport { get; set; }

        [ProbabilityQuestion("Does the customer need technical help with a product or service?")]
        public double TechnicalSupportProbability { get; set; }

        [ProbabilityQuestion("Does the customer need technical help with a product or service?")]
        public decimal TechnicalSupportProbabilityDecimal { get; set; }

        [ProbabilityQuestion("Does the customer need technical help with a product or service?", threshold: 0.7)]
        public Probability? NeedsTechnicalSupport { get; set; }

        // Score questions support double, decimal, or Score<TEnum> with probabilities.
        [ScoreQuestion<SupportSeverity>("How severely is the reported problem affecting the customer's ability to use the product or service?")]
        public double SeverityScore { get; set; }

        [ScoreQuestion<SupportSeverity>("How severely is the reported problem affecting the customer's ability to use the product or service?")]
        public decimal SeverityScoreDecimal { get; set; }

        [ScoreQuestion<SupportSeverity>("How severely is the reported problem affecting the customer's ability to use the product or service?")]
        public Score<SupportSeverity>? Severity { get; set; }
    }

    public enum SupportDepartment
    {
        [Description("Product setup, troubleshooting, errors, and technical problems")]
        TechnicalSupport,
        [Description("Invoices, payments, refunds, and subscription charges")]
        Billing,
        [Description("General customer service questions unrelated to technical issues or billing")]
        CustomerService
    }

    public enum SupportSeverity
    {
        [Description("A minor issue with no meaningful impact on using the product or service")]
        Minor,
        [Description("Some functionality is impaired, but the product or service remains usable")]
        Degraded,
        [Description("The customer cannot use the product or service")]
        Blocking
    }
}
