using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace context_seven.Tests;

public class Context7ServiceTests
{
    private readonly Mock<ILogger<Context7Service>> _loggerMock;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly Context7Service _context7Service;

    public Context7ServiceTests()
    {
        _loggerMock = new Mock<ILogger<Context7Service>>();
        _mockHttp = new MockHttpMessageHandler();
        
        // Setup the mock HttpClientFactory
        var httpClient = _mockHttp.ToHttpClient();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        _context7Service = new Context7Service(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchLibraries_ReturnsSearchResponse_WhenApiCallSucceeds()
    {
        // Arrange
        var query = "dotnet";
        var searchResponse = new SearchResponse
        {
            Results = new List<SearchResult>
            {
                new SearchResult
                {
                    Id = "/dotnet/runtime",
                    Title = ".NET Runtime",
                    Description = ".NET Runtime and Libraries",
                    TotalSnippets = 100,
                    TrustScore = 95
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(searchResponse);
        
        _mockHttp.When($"https://context7.com/api/v1/search?query={Uri.EscapeDataString(query)}")
            .Respond("application/json", responseJson);

        // Act
        var result = await _context7Service.SearchLibraries(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Single(result.Results);
        Assert.Equal("/dotnet/runtime", result.Results[0].Id);
        Assert.Equal(".NET Runtime", result.Results[0].Title);
        Assert.Equal(95, result.Results[0].TrustScore);
    }

    [Fact]
    public async Task SearchLibraries_ReturnsNull_WhenApiCallFails()
    {
        // Arrange
        var query = "failedrequest";
        
        _mockHttp.When($"https://context7.com/api/v1/search?query={Uri.EscapeDataString(query)}")
            .Respond(HttpStatusCode.InternalServerError);

        // Act
        var result = await _context7Service.SearchLibraries(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLibraryDocumentation_ReturnsDocumentation_WhenFound()
    {
        // Arrange
        var libraryId = "/dotnet/runtime";
        var expectedDocumentation = "This is the documentation for .NET Runtime";
        
        _mockHttp.When($"https://context7.com/api/v1/dotnet/runtime?type=txt")
            .Respond("text/plain", expectedDocumentation);

        // Act
        var result = await _context7Service.FetchLibraryDocumentation(libraryId);

        // Assert
        Assert.Equal(expectedDocumentation, result);
    }

    [Fact]
    public async Task FetchLibraryDocumentation_ProcessesLeadingSlash_WhenPresent()
    {
        // Arrange
        var libraryId = "/dotnet/runtime"; // With leading slash
        var expectedDocumentation = "This is the documentation for .NET Runtime";
        
        _mockHttp.When($"https://context7.com/api/v1/dotnet/runtime?type=txt")
            .Respond("text/plain", expectedDocumentation);

        // Act
        var result = await _context7Service.FetchLibraryDocumentation(libraryId);

        // Assert
        Assert.Equal(expectedDocumentation, result);
    }

    [Fact]
    public async Task FetchLibraryDocumentation_HandlesAllParameters_WhenProvided()
    {
        // Arrange
        var libraryId = "dotnet/runtime";
        var tokens = 5000;
        var topic = "gc";
        var folders = "src/libraries";
        var expectedDocumentation = "Documentation with all parameters";
        
        _mockHttp.When($"https://context7.com/api/v1/{libraryId}?type=txt&tokens={tokens}&topic={topic}&folders={Uri.EscapeDataString(folders)}")
            .Respond("text/plain", expectedDocumentation);

        // Act
        var result = await _context7Service.FetchLibraryDocumentation(libraryId, tokens, topic, folders);

        // Assert
        Assert.Equal(expectedDocumentation, result);
    }

    [Fact]
    public async Task FetchLibraryDocumentation_ReturnsNull_WhenApiCallFails()
    {
        // Arrange
        var libraryId = "error/library";
        
        _mockHttp.When($"https://context7.com/api/v1/{libraryId}?type=txt")
            .Respond(HttpStatusCode.InternalServerError);

        // Act
        var result = await _context7Service.FetchLibraryDocumentation(libraryId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLibraryDocumentation_ReturnsNull_WhenContentIsEmpty()
    {
        // Arrange
        var libraryId = "empty/library";
        
        _mockHttp.When($"https://context7.com/api/v1/{libraryId}?type=txt")
            .Respond("text/plain", "");

        // Act
        var result = await _context7Service.FetchLibraryDocumentation(libraryId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLibraryDocumentation_ReturnsNull_WhenContentIsNoContentAvailable()
    {
        // Arrange
        var libraryId = "nocontent/library";
        
        _mockHttp.When($"https://context7.com/api/v1/{libraryId}?type=txt")
            .Respond("text/plain", "No content available");

        // Act
        var result = await _context7Service.FetchLibraryDocumentation(libraryId);

        // Assert
        Assert.Null(result);
    }
}
