using ModelContextProtocol;

namespace RemoteMcpServer.Resources;

public static class CustomerStore
{
    private static readonly Dictionary<string, Customer> Customers = new()
    {
        ["123"] = new("123", "Alice Jensen", "alice@example.com"),
        ["456"] = new("456", "Bob Nielsen", "bob@example.com"),
        ["789"] = new("789", "Clara Hansen", "clara@example.com"),
        ["101"] = new("101", "David Larsen", "david@example.com"),
        ["202"] = new("202", "Emma Pedersen", "emma@example.com")
    };

    public static Customer[] GetAllCustomers()
    {
        return Customers.Values.ToArray();
    }

    public static Customer GetCustomer(string customerId)
    {
        return Customers.TryGetValue(customerId, out Customer? customer) ? 
            customer : 
            throw new McpException($"Customer '{customerId}' was not found.");
    }
}