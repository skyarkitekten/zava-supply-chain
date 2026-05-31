using System.ComponentModel;
using System.Text.Json.Nodes;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ZavaShop.Workshop.Data;

// ── helpers ───────────────────────────────────────────────────────────────────

static void LoadEnv()
{
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
        string candidate = Path.Combine(dir.FullName, "workshop", ".env");
        if (File.Exists(candidate))
        {
            foreach (string raw in File.ReadAllLines(candidate))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
                    continue;
                int eq = line.IndexOf('=');
                string key   = line[..eq].Trim();
                string value = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
                if (key.Length > 0 && Environment.GetEnvironmentVariable(key) is null)
                    Environment.SetEnvironmentVariable(key, value);
            }
            return;
        }
        dir = dir.Parent;
    }
}

// ── function tools ─────────────────────────────────────────────────────────────

[Description("Get current on-hand stock of a SKU in a warehouse.")]
static string GetStock(
    [Description("SKU id, e.g. SKU-7421.")] string sku,
    [Description("Warehouse id, e.g. SEA-01.")] string warehouse = "SEA-01")
{
    JsonNode? row = ZavaData.FindStock(sku, warehouse);
    if (row is null)
        return $"{sku} is not tracked at warehouse {warehouse}.";
    return $"{sku} @ {warehouse}: on_hand={row["on_hand"]}, " +
           $"reserved={row["reserved"]}, reorder_point={row["reorder_point"]}";
}

[Description("Query the status of an inbound Purchase Order by its number.")]
static string GetPoStatus(
    [Description("PO number, e.g. PO-20260518-001.")] string poNumber)
{
    JsonNode? po = ZavaData.FindPo(poNumber);
    return po?.ToJsonString()
        ?? $"{{\"po_number\":\"{poNumber}\",\"status\":\"unknown\"}}";
}

[Description("List open (not-yet-delivered) Purchase Orders for a SKU, newest ETA first.")]
static string FindOpenPosBySku(
    [Description("SKU id, e.g. SKU-7421.")] string sku,
    [Description("Optional warehouse id; omit or pass empty string to search all warehouses.")]
    string warehouse = "")
{
    JsonArray pos = ZavaData.LoadPurchaseOrders();
    var open = pos
        .Where(p => p is not null
            && p["sku"]?.GetValue<string>() == sku
            && p["status"]?.GetValue<string>() != "delivered"
            && (string.IsNullOrEmpty(warehouse)
                || p["destination_warehouse"]?.GetValue<string>() == warehouse))
        .OrderByDescending(p => p!["eta"]?.GetValue<string>(), StringComparer.Ordinal)
        .Select(p => JsonNode.Parse(p!.ToJsonString())!)
        .ToArray();
    return new JsonArray(open).ToJsonString();
}

// ── main ──────────────────────────────────────────────────────────────────────

LoadEnv();

string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
string model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL")
    ?? throw new InvalidOperationException("FOUNDRY_MODEL is not set.");

AIProjectClient projectClient = new(new Uri(endpoint), new AzureCliCredential());

await using McpClient learnMcp = await McpClient.CreateAsync(
    new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint    = new Uri("https://learn.microsoft.com/api/mcp"),
        Name        = "Microsoft Learn MCP",
        TransportMode = HttpTransportMode.StreamableHttp,
    }));

IList<McpClientTool> mcpTools = await learnMcp.ListToolsAsync();

List<AITool> tools =
[
    AIFunctionFactory.Create(GetStock),
    AIFunctionFactory.Create(GetPoStatus),
    AIFunctionFactory.Create(FindOpenPosBySku),
    .. mcpTools.Cast<AITool>(),
];

AIAgent agent = projectClient.AsAIAgent(
    model,
    instructions:
        "You are Zara, the warehouse assistant for ZavaShop's Seattle fulfillment center. " +
        "Always answer using real data from the tools and never invent stock or PO numbers. " +
        "To find the latest unarrived PO for a SKU, call FindOpenPosBySku first, then " +
        "GetPoStatus for fuller detail.",
    name: "Zara",
    tools: tools);

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("=== Turn 1: stock query ===");
Console.WriteLine(await agent.RunAsync(
    "How many SKU-7421 do we have left at SEA-01?", session));

Console.WriteLine("\n=== Turn 2: open PO follow-up (no SKU repeated) ===");
Console.WriteLine(await agent.RunAsync(
    "What is the most recent PO for that SKU that hasn't arrived yet?", session));

Console.WriteLine("\n=== Turn 3: Microsoft Learn MCP ===");
Console.WriteLine(await agent.RunAsync(
    "Search the Microsoft Learn MCP for best practices on Azure AI Foundry.", session));
