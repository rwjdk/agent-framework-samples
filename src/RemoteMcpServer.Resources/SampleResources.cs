using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace RemoteMcpServer.Resources;

[McpServerResourceType]
public class SampleResources
{
    [McpServerResource(UriTemplate = "customers://{customerId}", Name = "get_customer_resource", MimeType = "application/json")]
    [Description("Gets customer by Id")]
    public static string GetCustomer(string customerId)
    {
        return JsonSerializer.Serialize(CustomerStore.GetCustomer(customerId));
    }

    [McpServerResource(UriTemplate = "customers://all", Name = "get_all_customers_resource", MimeType = "application/json")]
    [Description("Gets all customers")]
    public static string GetAllCustomers()
    {
        return JsonSerializer.Serialize(CustomerStore.GetAllCustomers());
    }

    [McpServerResource(UriTemplate = "sample://logo", Name = "get_logo", MimeType = "image/png")]
    [Description("Get the Logo")]
    public static async Task<BlobResourceContents> Logo(CancellationToken cancellationToken)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        return BlobResourceContents.FromBytes(bytes, "sample://logo", "image/png");
    }
}