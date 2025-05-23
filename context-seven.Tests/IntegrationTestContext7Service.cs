using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace context_seven.Tests;

/// <summary>
/// A version of Context7Service specifically for integration tests that can handle 
/// varying API responses. This class replicates the main service but uses more 
/// flexible models for JSON deserialization.
/// </summary>
public class IntegrationTestContext7Service
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private const string Context7ApiBaseUrl = "https://context7.com/api";
    private const string DefaultType = "txt";

    public IntegrationTestContext7Service(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    /// <summary>
    /// Searches for libraries using the Context7 API, with improved error handling for integration tests
    /// </summary>
    public async Task<IntegrationTestModels.SearchResponse?> SearchLibraries(string query)
    {
        try
        {
            _logger.LogInformation("Searching libraries with query: {Query}", query);
            var url = new Uri($"{Context7ApiBaseUrl}/v1/search?query={Uri.EscapeDataString(query)}");
            
            _logger.LogInformation("Sending request to: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search libraries: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response: {Content}", 
                content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            
            // Use System.Text.Json directly for more control over deserialization
            var options = new JsonSerializerOptions 
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            var result = JsonSerializer.Deserialize<IntegrationTestModels.SearchResponse>(content, options);
            _logger.LogInformation("Search complete. Found {Count} results", result?.Results?.Count ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching libraries with query: {Query}", query);
            return null;
        }
    }

    /// <summary>
    /// Fetches library documentation using the Context7 API, with improved error handling for integration tests
    /// </summary>
    public async Task<string?> FetchLibraryDocumentation(
        string libraryId, int? tokens = null, string? topic = null, string? folders = null)
    {
        try
        {
            _logger.LogInformation("Fetching documentation for library: {LibraryId}, Topic: {Topic}, Folders: {Folders}", 
                libraryId, topic ?? "null", folders ?? "null");
                
            // Remove leading slash if present
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
            request.Headers.Add("X-Context7-Source", "mcp-server-integration-test");
            
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
