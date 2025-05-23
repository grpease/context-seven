using System.Text.Json;
using System.Text.Json.Serialization;

namespace context_seven.Tests;

/// <summary>
/// Models for integration tests with more flexible JSON deserialization.
/// These models are designed to be more tolerant of API changes.
/// </summary>
public class IntegrationTestModels
{
    public class SearchResponse
    {
        [JsonPropertyName("results")]
        public List<SearchResult>? Results { get; set; }
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
        public string? Branch { get; set; }
        
        [JsonPropertyName("lastUpdateDate")]
        public string? LastUpdateDate { get; set; }
        
        [JsonPropertyName("state")]
        public string? State { get; set; }
        
        [JsonPropertyName("totalTokens")]
        public int? TotalTokens { get; set; }
        
        [JsonPropertyName("totalSnippets")]
        public int? TotalSnippets { get; set; }
        
        [JsonPropertyName("totalPages")]
        public int? TotalPages { get; set; }
        
        [JsonPropertyName("stars")]
        public JsonElement? Stars { get; set; }
        
        [JsonPropertyName("trustScore")]
        public JsonElement? TrustScore { get; set; }
    }
}
