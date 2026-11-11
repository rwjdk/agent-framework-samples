using DoItYourselfAiCalling;
using DoItYourselfAiCalling.Helpers;
using DoItYourselfAiCalling.Models;

Console.Clear();
Console.WriteLine("Sample: Azure OpenAI with HTTP Client");

string endpoint = EnvironmentVariableHelper.GetValueOrAsk("myEndpoint");
string apiKey = EnvironmentVariableHelper.GetValueOrAsk("myApiKey", secret: true);

Provider provider = new(endpoint, apiKey, "gpt-6-luna");
MyAgent agent = provider.AsAgent(instructions: "You are a nice AI!");

Console.WriteLine("----------");

//Part 1: Normal Call
string question1 = "What is the capital of France?";
Console.WriteLine($"Q1: {question1}");
MyResponse response1 = await agent.RunAsync(question1);
Console.WriteLine($"A1: {response1}");

Console.WriteLine("----------");

//Part 2: Structured Output
string question2 = "What is the top movie according to IMDB?";
Console.WriteLine($"Q2: {question2}");
MyResponse<Movie> response2 = await agent.RunAsync<Movie>(question2);
Console.WriteLine($"A2: Name = {response2.Result.Name} - Year: {response2.Result.YearOfRelease} - Raw: {response2.Text}");

Console.WriteLine("----------");

//Part 3: Tool Calling
agent.Tools = [new MyTool("get_weather", "Get Weather for City", Delegate: Tools.GetWeather)];

string question3 = "What is the Weather like in Paris?";
Console.WriteLine($"Q3: {question3}");
MyResponse response3 = await agent.RunAsync(question3);
Console.WriteLine($"A3: {response3}");