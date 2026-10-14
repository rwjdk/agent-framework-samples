using System.ComponentModel;
using ModelContextProtocol.Server;

namespace RemoteMcpServer.Resources;

[McpServerToolType]
public class SampleTools
{
    [McpServerTool(Name = "get_customer"), Description("Get Customer by Id")]
    public static Customer GetCustomer(string customerId)
    {
        return CustomerStore.GetCustomer(customerId);
    }

    [McpServerTool(Name = "get_all_customers"), Description("Gets all customers.")]
    public static Customer[] GetAllCustomers()
    {
        return CustomerStore.GetAllCustomers();
    }
}