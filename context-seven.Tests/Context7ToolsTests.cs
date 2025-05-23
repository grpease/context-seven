using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace context_seven.Tests;

public class Context7ToolsTests
{
    private readonly Mock<ILogger<Context7Service>> _loggerMock;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly Context7Service _context7Service;

    public Context7ToolsTests()
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
    public async Task ResolveLibraryId_ReturnsFormattedResults_WhenLibrariesFound()
    {
        // Arrange
        var libraryName = "dotnet";
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
        
        _mockHttp.When($"https://context7.com/api/v1/search?query={Uri.EscapeDataString(libraryName)}")
            .Respond("application/json", responseJson);

        // Act
        var result = await Context7Tools.ResolveLibraryId(_context7Service, libraryName);

        // Assert
        Assert.Contains("Available Libraries (top matches):", result);
        Assert.Contains(".NET Runtime", result);
        Assert.Contains("/dotnet/runtime", result);
        Assert.Contains("- Trust Score: 95", result);
    }

    [Fact]
    public async Task ResolveLibraryId_ReturnsErrorMessage_WhenNoLibrariesFound()
    {
        // Arrange
        var libraryName = "nonexistentlibrary";
        var emptyResponse = new SearchResponse { Results = new List<SearchResult>() };
        var responseJson = JsonSerializer.Serialize(emptyResponse);
        
        _mockHttp.When($"https://context7.com/api/v1/search?query={Uri.EscapeDataString(libraryName)}")
            .Respond("application/json", responseJson);

        // Act
        var result = await Context7Tools.ResolveLibraryId(_context7Service, libraryName);

        // Assert
        Assert.Equal("No libraries found matching your query.", result);
    }

    [Fact]
    public async Task ResolveLibraryId_ReturnsErrorMessage_WhenApiCallFails()
    {
        // Arrange
        var libraryName = "failedrequest";
        
        _mockHttp.When($"https://context7.com/api/v1/search?query={Uri.EscapeDataString(libraryName)}")
            .Respond(HttpStatusCode.InternalServerError);        // Act
        var result = await Context7Tools.ResolveLibraryId(_context7Service, libraryName);

        // Assert
        Assert.Equal("No libraries found matching your query.", result);
    }

    [Fact]
    public async Task GetLibraryDocs_ReturnsDocumentation_WhenFound()
    {
        // Arrange
        var libraryId = "/dotnet/runtime";
        var expectedDocumentation = "This is the documentation for .NET Runtime";
        
        _mockHttp.When($"https://context7.com/api/v1/dotnet/runtime?type=txt&tokens=10000")
            .Respond("text/plain", expectedDocumentation);

        // Act
        var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId);

        // Assert
        Assert.Equal(expectedDocumentation, result);
    }

    [Fact]
    public async Task GetLibraryDocs_HandlesTopicAndTokens_WhenProvided()
    {
        // Arrange
        var libraryId = "/dotnet/runtime";
        var topic = "gc";
        var tokens = 5000;
        var expectedDocumentation = "Documentation about GC in .NET Runtime";
        
        _mockHttp.When($"https://context7.com/api/v1/dotnet/runtime?type=txt&tokens={tokens}&topic={topic}")
            .Respond("text/plain", expectedDocumentation);

        // Act
        var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId, topic, tokens);

        // Assert
        Assert.Equal(expectedDocumentation, result);
    }

    [Fact]
    public async Task GetLibraryDocs_HandlesFolders_WhenProvided()
    {
        // Arrange
        var folderPath = "src/libraries";
        var libraryId = $"/dotnet/runtime?folders={folderPath}";
        var expectedDocumentation = "Documentation for specific folders in .NET Runtime";
        
        _mockHttp.When($"https://context7.com/api/v1/dotnet/runtime?type=txt&tokens=10000&folders={Uri.EscapeDataString(folderPath)}")
            .Respond("text/plain", expectedDocumentation);

        // Act
        var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId);

        // Assert
        Assert.Equal(expectedDocumentation, result);
    }

    [Fact]
    public async Task GetLibraryDocs_ReturnsErrorMessage_WhenDocumentationNotFound()
    {
        // Arrange
        var libraryId = "/nonexistent/library";
        
        _mockHttp.When($"https://context7.com/api/v1/nonexistent/library?type=txt&tokens=10000")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId);

        // Assert
        Assert.Contains("Documentation not found for this library", result);
    }

    [Fact]
    public async Task GetLibraryDocs_ReturnsErrorMessage_WhenApiCallFails()
    {
        // Arrange
        var libraryId = "/error/library";
        
        _mockHttp.When($"https://context7.com/api/v1/error/library?type=txt&tokens=10000")
            .Respond(HttpStatusCode.InternalServerError);        // Act
        var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId);

        // Assert
        Assert.Contains("Documentation not found for this library", result);
    }
}
