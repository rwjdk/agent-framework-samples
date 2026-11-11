#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
using AIAsDataFilter.Models;

using Shared;
using System.ClientModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared.Extensions;

Console.Clear();
string json = File.ReadAllText("books.json");
List<Book> books = JsonSerializer.Deserialize<List<Book>>(json)!;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

string model = "gpt-6-luna";
//string model = "gpt-6-luna";

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient(model)
    .AsAIAgent(instructions: "You are librarian");

string bookPromptData = $"In these books <books>{string.Join("", books)}</books>: ";
string question1 = "How many of them was released in 1980";
string question2 = "Give Count + List the books whose author have an 's' as the last letter of their name";
string question3 = "Give Count + List the books whose author have an 's' as the last letter of their name, " +
                   "is a Genre other than 'Fantasy' and was released between 1800 and 1900";

Utils.Green($"Q1: {question1}");
AgentResponse response1 = await agent.RunAsync(bookPromptData + question1);
Console.WriteLine("A1: " + response1);
response1.Usage.OutputAsInformation();
int q1RealAnswer = books.Count(x => x.YearOfRelease == 1980);
Utils.Yellow($"Real Answer: {q1RealAnswer}");

Utils.Separator();

Utils.Green($"Q2: {question2}");
AgentResponse response2 = await agent.RunAsync(bookPromptData + question2);
Console.WriteLine("A2: " + response2);
response2.Usage.OutputAsInformation();
int q2RealAnswer = books.Count(x => x.Author.EndsWith("s"));
Utils.Yellow($"Real Answer: {q2RealAnswer}");

Utils.Separator();

Utils.Green($"Q3: {question3}");
AgentResponse response3 = await agent.RunAsync(bookPromptData + question3);
Console.WriteLine("A3: " + response3);
response3.Usage.OutputAsInformation();
int q3RealAnswer = books.Count(x => x.Author.EndsWith("s") &&
                                    x.YearOfRelease >= 1800 &&
                                    x.YearOfRelease <= 1900 &&
                                    x.Genre != Genre.Fantasy);
Utils.Yellow($"Real Answer: {q3RealAnswer}");

Utils.Separator();

//Let's do the work differently
AgentResponse<BookFilter[]> response1Filter = await agent
    .RunAsync<BookFilter[]>($"Make a filter for the following query: {question1}");
BookFilter[] filter1 = response1Filter.Result;
List<Book> filter1Books = GetBooksForFilter(filter1);

AgentResponse<BookFilter[]> response2Filter = await agent
    .RunAsync<BookFilter[]>($"Make a filter for the following query: {question2}");
BookFilter[] filter2 = response2Filter.Result;
List<Book> filter2Books = GetBooksForFilter(filter2);

AgentResponse<BookFilter[]> response3Filter = await agent
    .RunAsync<BookFilter[]>($"Make a filter for the following query: {question3}");
BookFilter[] filter3 = response3Filter.Result;
List<Book> filter3Books = GetBooksForFilter(filter3);


List<Book> GetBooksForFilter(BookFilter[] filters)
{
    IEnumerable<Book> filteredBooks = books;
    foreach (BookFilter filter in filters)
    {
        switch (filter.Operation)
        {
            case ComparisonOperation.Equal:
                filteredBooks = filter.Field switch
                {
                    BookField.Title => filteredBooks.Where(x => x.Title.Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease.ToString().Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Author => filteredBooks.Where(x => x.Author.Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Genre => filteredBooks.Where(x => x.Genre.ToString().Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Synopsis => filteredBooks.Where(x => x.Synopsis.Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.NotEqual:
                filteredBooks = filter.Field switch
                {
                    BookField.Title => filteredBooks.Where(x => !x.Title.Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.YearOfRelease => filteredBooks.Where(x => !x.YearOfRelease.ToString().Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Author => filteredBooks.Where(x => !x.Author.Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Genre => filteredBooks.Where(x => !x.Genre.ToString().Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Synopsis => filteredBooks.Where(x => !x.Synopsis.Equals(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.StartWith:
                filteredBooks = filter.Field switch
                {
                    BookField.Title => filteredBooks.Where(x => x.Title.StartsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease.ToString().StartsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Author => filteredBooks.Where(x => x.Author.StartsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Genre => filteredBooks.Where(x => x.Genre.ToString().StartsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Synopsis => filteredBooks.Where(x => x.Synopsis.StartsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.EndWith:
                filteredBooks = filter.Field switch
                {
                    BookField.Title => filteredBooks.Where(x => x.Title.EndsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease.ToString().EndsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Author => filteredBooks.Where(x => x.Author.EndsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Genre => filteredBooks.Where(x => x.Genre.ToString().EndsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Synopsis => filteredBooks.Where(x => x.Synopsis.EndsWith(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.Contains:
                filteredBooks = filter.Field switch
                {
                    BookField.Title => filteredBooks.Where(x => x.Title.Contains(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease.ToString().Contains(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Author => filteredBooks.Where(x => x.Author.Contains(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Genre => filteredBooks.Where(x => x.Genre.ToString().Contains(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    BookField.Synopsis => filteredBooks.Where(x => x.Synopsis.Contains(filter.Value, StringComparison.CurrentCultureIgnoreCase)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.GreaterThan:
                filteredBooks = filter.Field switch
                {
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease > Convert.ToInt32(filter.Value)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.GreaterThanOrEqual:
                filteredBooks = filter.Field switch
                {
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease >= Convert.ToInt32(filter.Value)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.LessThan:
                filteredBooks = filter.Field switch
                {
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease < Convert.ToInt32(filter.Value)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.LessThanOrEqual:
                filteredBooks = filter.Field switch
                {
                    BookField.YearOfRelease => filteredBooks.Where(x => x.YearOfRelease <= Convert.ToInt32(filter.Value)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case ComparisonOperation.RegEx:
                filteredBooks = filter.Field switch
                {
                    BookField.Title => filteredBooks.Where(x => Regex.IsMatch(x.Title, filter.Value, RegexOptions.IgnoreCase)),
                    BookField.Author => filteredBooks.Where(x => Regex.IsMatch(x.Author, filter.Value, RegexOptions.IgnoreCase)),
                    BookField.Genre => filteredBooks.Where(x => Regex.IsMatch(x.Genre.ToString(), filter.Value, RegexOptions.IgnoreCase)),
                    BookField.Synopsis => filteredBooks.Where(x => Regex.IsMatch(x.Synopsis, filter.Value, RegexOptions.IgnoreCase)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    return filteredBooks.ToList();
}

class BookFilter
{
    public required BookField Field { get; set; }
    public required ComparisonOperation Operation { get; set; }
    public required string Value { get; set; }
}

internal enum ComparisonOperation
{
    Equal,
    NotEqual,
    StartWith,
    EndWith,
    Contains,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    RegEx,
}

internal enum BookField
{
    Title,
    YearOfRelease,
    Author,
    Genre,
    Synopsis,
}