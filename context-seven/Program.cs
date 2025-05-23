using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = Host.CreateApplicationBuilder(args);

// Create logs directory if it doesn't exist
var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logsDirectory))
{
    Directory.CreateDirectory(logsDirectory);
}

// Configure logging
builder.Logging
    // Console logging
    .AddConsole(consoleLogOptions =>
    {
        consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
    })
    // File logging
    .AddFile(Path.Combine(logsDirectory, "context-seven-log-{Date}.txt"), LogLevel.Information);

// Configure MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Register required services
builder.Services.AddSingleton<Context7Service>();
builder.Services.AddHttpClient();

// Build and run the host
var host = builder.Build();
LogMcpServerStartup(host.Services.GetRequiredService<ILogger<Program>>());
await host.RunAsync();

// Log MCP server startup
void LogMcpServerStartup(ILogger logger)
{
    logger.LogInformation("==================================================");
    logger.LogInformation("Context-Seven MCP Server started at {Time}", DateTime.Now);
    logger.LogInformation("Logs directory: {LogsDirectory}", logsDirectory);
    logger.LogInformation("==================================================");
}

await builder.Build().RunAsync();

/// <summary>
/// This tool provides basic echo functionality
/// </summary>
[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message)
    {
        McpLogging.LogToolCall("Echo", message);
        var response = $"Hello from context-seven: {message}";
        McpLogging.LogToolResult("Echo", message, response);
        return response;
    }

    [McpServerTool, Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message)
    {
        McpLogging.LogToolCall("ReverseEcho", message);
        var response = new string(message.Reverse().ToArray());
        McpLogging.LogToolResult("ReverseEcho", message, response);
        return response;
    }
}

/// <summary>
/// Context7 tools to search libraries and fetch documentation
/// </summary>
[McpServerToolType]
public static class Context7Tools
{
    [McpServerTool, Description("Searches for libraries matching the given query and returns Context7-compatible library IDs.")]
    public static async Task<string> ResolveLibraryId(
        Context7Service context7Service,
        [Description("Library name to search for")] string libraryName)
    {
        McpLogging.LogToolCall("ResolveLibraryId", libraryName);

        try
        {
            var searchResponse = await context7Service.SearchLibraries(libraryName);

            if (searchResponse == null || searchResponse.Results == null || searchResponse.Results.Count == 0)
            {
                var noResultsMsg = "No libraries found matching your query.";
                McpLogging.LogToolResult("ResolveLibraryId", libraryName, noResultsMsg);
                return noResultsMsg;
            }

            var formattedResults = FormatSearchResults(searchResponse);
            McpLogging.LogToolResult("ResolveLibraryId", libraryName,
                $"Found {searchResponse.Results.Count} libraries (response truncated)");
            return formattedResults;
        }
        catch (Exception ex)
        {
            McpLogging.LogError("ResolveLibraryId", libraryName, ex);
            return $"Error resolving library ID: {ex.Message}";
        }
    }

    [McpServerTool, Description("Fetches up-to-date documentation for a library using a Context7-compatible library ID.")]
    public static async Task<string> GetLibraryDocs(
        Context7Service context7Service,
        [Description("Context7-compatible library ID retrieved from resolve-library-id")] string context7CompatibleLibraryID,
        [Description("Optional topic to focus documentation on")] string? topic = null,
        [Description("Maximum number of tokens to retrieve (default: 10000)")] int? tokens = 10000)
    {
        var args = new { LibraryId = context7CompatibleLibraryID, Topic = topic, Tokens = tokens };
        McpLogging.LogToolCall("GetLibraryDocs", args);

        try
        {
            // Parse out folders parameter if present
            string? folders = null;
            string libraryId = context7CompatibleLibraryID;

            if (context7CompatibleLibraryID.Contains("?folders="))
            {
                var parts = context7CompatibleLibraryID.Split("?folders=");
                libraryId = parts[0];
                folders = parts[1];
            }

            var documentation = await context7Service.FetchLibraryDocumentation(libraryId, tokens, topic, folders);

            if (string.IsNullOrEmpty(documentation))
            {
                var notFoundMsg = "Documentation not found for this library. Please verify the library ID is correct.";
                McpLogging.LogToolResult("GetLibraryDocs", args, notFoundMsg);
                return notFoundMsg;
            }

            McpLogging.LogToolResult("GetLibraryDocs", args,
                $"Successfully fetched {documentation.Length} characters of documentation");
            return documentation;
        }
        catch (Exception ex)
        {
            McpLogging.LogError("GetLibraryDocs", args, ex);
            return $"Error fetching library documentation: {ex.Message}";
        }
    }

    private static string FormatSearchResults(SearchResponse searchResponse)
    {
        var resultsList = new List<string>
        {
            "Available Libraries (top matches):\n",
            "Each result includes:",
            "- Library ID: Context7-compatible identifier (format: /org/repo)",
            "- Name: Library or package name",
            "- Description: Short summary",
            "- Code Snippets: Number of available code examples",
            "- Trust Score: Authority indicator\n"
        };

        foreach (var result in searchResponse.Results)
        {
            var formattedResult = new List<string>
            {
                $"- Title: {result.Title}",
                $"- Context7-compatible library ID: {result.Id}",
                $"- Description: {result.Description}"
            };

            if (result.TotalSnippets >= 0)
            {
                formattedResult.Add($"- Code Snippets: {result.TotalSnippets}");
            }

            if (result.TrustScore.HasValue && result.TrustScore.Value >= 0)
            {
                formattedResult.Add($"- Trust Score: {result.TrustScore}");
            }

            resultsList.Add(string.Join("\n", formattedResult));
            resultsList.Add("----------");
        }

        return string.Join("\n", resultsList);
    }
}

/// <summary>
/// Service to communicate with Context7 API
/// </summary>
public class Context7Service
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Context7Service> _logger;
    private const string Context7ApiBaseUrl = "https://context7.com/api";
    private const string DefaultType = "txt";

    public Context7Service(IHttpClientFactory httpClientFactory, ILogger<Context7Service> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<SearchResponse?> SearchLibraries(string query)
    {
        try
        {
            _logger.LogInformation("Searching libraries with query: {Query}", query);
            var url = new Uri($"{Context7ApiBaseUrl}/v1/search?query={Uri.EscapeDataString(query)}");
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search libraries: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
            _logger.LogInformation("Search complete. Found {Count} results", result?.Results.Count ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching libraries with query: {Query}", query);
            return null;
        }
    }

    public async Task<string?> FetchLibraryDocumentation(string libraryId, int? tokens = null, string? topic = null, string? folders = null)
    {
        try
        {
            _logger.LogInformation("Fetching documentation for library: {LibraryId}, Topic: {Topic}, Folders: {Folders}",
                libraryId, topic ?? "null", folders ?? "null");

            if (libraryId.StartsWith("/"))
            {
                libraryId = libraryId.Substring(1);
            }
            var uriBuilder = new UriBuilder($"{Context7ApiBaseUrl}/v1/{libraryId}");
            var queryParameters = new List<string>
            {
                $"type={DefaultType}"
            };

            if (tokens.HasValue)
            {
                queryParameters.Add($"tokens={tokens.Value}");
            }

            if (!string.IsNullOrEmpty(topic))
            {
                queryParameters.Add($"topic={Uri.EscapeDataString(topic)}");
            }

            if (!string.IsNullOrEmpty(folders))
            {
                queryParameters.Add($"folders={Uri.EscapeDataString(folders)}");
            }

            uriBuilder.Query = string.Join("&", queryParameters);


            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            request.Headers.Add("X-Context7-Source", "mcp-server");

            _logger.LogInformation("Sending request to: {Uri}", uriBuilder.Uri);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch documentation: {StatusCode}", response.StatusCode);
                return null;
            }

            var text = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(text) || text == "No content available" || text == "No context data available")
            {
                _logger.LogWarning("No documentation content available for library: {LibraryId}", libraryId);
                return null;
            }

            _logger.LogInformation("Successfully fetched documentation for library: {LibraryId} ({Length} characters)",
                libraryId, text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching library documentation for: {LibraryId}", libraryId);
            return null;
        }
    }
}

public class SearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = [];
}

public class SearchResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("branch")]
    public string Branch { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdateDate")]
    [JsonConverter(typeof(JsonDateTimeConverter))]
    public DateTime LastUpdateDate { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("totalSnippets")]
    public int TotalSnippets { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("stars")]
    public int? Stars { get; set; }

    [JsonPropertyName("trustScore")]
    public float? TrustScore { get; set; }
}

/// <summary>
/// Custom JSON converter for handling DateTime fields in different formats
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
            return DateTime.MinValue;

        // Try standard ISO format first
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result;

        // Fallback to default value if parsing fails
        return DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("o")); // Use ISO 8601 format
    }
}

// Log helper class for MCP tools
public static class McpLogging
{
    private static readonly ILogger _logger;

    static McpLogging()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            builder.AddFile(Path.Combine(logsDirectory, "context-seven-log-{Date}.txt"), LogLevel.Information);
        });

        _logger = loggerFactory.CreateLogger("McpTools");
    }

    public static void LogToolCall(string toolName, object? args)
    {
        _logger.LogInformation("Tool Call: {ToolName}, Arguments: {Args}",
            toolName,
            JsonSerializer.Serialize(args));
    }

    public static void LogToolResult(string toolName, object? args, object? result)
    {
        var resultString = result?.ToString() ?? string.Empty;

        // Truncate very long results to avoid overwhelming logs
        if (resultString.Length > 500)
        {
            resultString = resultString.Substring(0, 500) + "... [truncated]";
        }

        _logger.LogInformation("Tool Result: {ToolName}, Result: {Result}",
            toolName,
            resultString);
    }

    public static void LogError(string toolName, object? args, Exception exception)
    {
        _logger.LogError(exception, "Tool Error: {ToolName}, Arguments: {Args}",
            toolName,
            JsonSerializer.Serialize(args));
    }
}





