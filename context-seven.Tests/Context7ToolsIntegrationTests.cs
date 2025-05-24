using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;
using Xunit.Abstractions;

namespace context_seven.Tests;

/// <summary>
/// Integration tests for Context7Tools that make real API calls to Microsoft's Semantic Kernel API.
/// </summary>
[Trait("Category", "Integration")]
public class Context7ToolsIntegrationTests : IDisposable
{    private readonly Context7Service _context7Service;
    private readonly ILogger<Context7Service> _logger;
    private readonly ITestOutputHelper? _output;
    private readonly ILoggerFactory _loggerFactory;

    public Context7ToolsIntegrationTests(ITestOutputHelper? output = null)
    {
        _output = output;
        
        // Create a logger factory with console and file logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            builder.AddFile(Path.Combine(logsDirectory, "integration-test-log-{Date}.txt"), LogLevel.Information);
            
            // Add xunit test output logger if available
            if (_output != null)
            {
                builder.AddProvider(new XunitLoggerProvider(_output));
            }
        });

        _logger = _loggerFactory.CreateLogger<Context7Service>();

        // Create an actual HttpClient without mocks
        var httpClientFactory = new HttpClientFactory();
        _context7Service = new Context7Service(httpClientFactory, _logger);
    }
    
    public void Dispose()
    {
        _loggerFactory?.Dispose();
    }

    /// <summary>
    /// This test attempts to search for the Semantic Kernel library through the real API
    /// </summary>
    [Fact]
    public async Task ResolveLibraryId_WithRealApi_ReturnsResults()
    {
        // Arrange
        string libraryName = "Semantic Kernel";

        try
        {
            // Act
            var result = await Context7Tools.ResolveLibraryId(_context7Service, libraryName);

            // Assert
            Assert.NotNull(result);
            LogOutput($"Result: {result}");
        }
        catch (Exception ex)
        {
            LogOutput($"Test failed with exception: {ex}");
            throw;
        }
    }

    /// <summary>
    /// This test attempts to get documentation for Semantic Kernel through the real API
    /// </summary>
    [Fact]
    public async Task GetLibraryDocs_WithRealApi_ReturnsDocumentation()
    {
        // This test uses a known library ID directly, rather than searching first
        // Arrange - This ID might need to be updated if the API structure changes
        string libraryId = "/microsoft/semantic-kernel";

        try
        {
            // Act
            var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            LogOutput($"Documentation sample (first 200 chars): {result.Substring(0, Math.Min(200, result.Length))}");
        }
        catch (Exception ex)
        {
            LogOutput($"Test failed with exception: {ex}");
            throw;
        }
    }

    /// <summary>
    /// This test checks that the API correctly handles requests with topic filters
    /// </summary>
    [Fact]
    public async Task GetLibraryDocs_WithTopic_ReturnsFilteredDocumentation()
    {
        // Arrange
        string libraryId = "/microsoft/semantic-kernel";
        string topic = "plugins";
        int tokens = 5000;

        try
        {
            // Act
            var result = await Context7Tools.GetLibraryDocs(_context7Service, libraryId, topic, tokens);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            LogOutput($"Documentation with topic '{topic}' sample (first 200 chars): {result.Substring(0, Math.Min(200, result.Length))}");
        }
        catch (Exception ex)
        {
            LogOutput($"Test failed with exception: {ex}");
            throw;
        }
    }    /// <summary>
    /// This test verifies the API's behavior with a non-existent library
    /// </summary>
    [Fact]
    public async Task ResolveLibraryId_WithRealApi_HandlesNonExistentLibrary()
    {
        // Arrange
        string nonExistentLibrary = "ThisLibraryDefinitelyDoesNotExist12345XYZ";

        try
        {
            // Act
            var result = await Context7Tools.ResolveLibraryId(_context7Service, nonExistentLibrary);

            // Assert
            Assert.NotNull(result);
            LogOutput($"Result for non-existent library: {result}");
        }
        catch (Exception ex)
        {
            LogOutput($"Test failed with exception: {ex}");
            throw;
        }
    }

    /// <summary>
    /// This test verifies the API's behavior with an invalid library ID
    /// </summary>
    [Fact]
    public async Task GetLibraryDocs_WithRealApi_HandlesInvalidLibraryId()
    {
        // Arrange
        string invalidLibraryId = "/invalid/library/id/that/does/not/exist";

        try
        {
            // Act
            var result = await Context7Tools.GetLibraryDocs(_context7Service, invalidLibraryId);

            // Assert
            Assert.NotNull(result);
            LogOutput($"Result for invalid library ID: {result}");
        }
        catch (Exception ex)
        {
            LogOutput($"Test failed with exception: {ex}");
            throw;
        }
    }
    
    private void LogOutput(string message)
    {
        _logger.LogInformation(message);
        _output?.WriteLine(message);
    }

    /// <summary>
    /// Xunit logger provider to capture logs in test output
    /// </summary>
    private class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_testOutputHelper, categoryName);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Xunit logger to output logs to test output
    /// </summary>
    private class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;        public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }
        
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                _testOutputHelper.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName}: {formatter(state, exception)}");
                if (exception != null)
                {
                    _testOutputHelper.WriteLine($"Exception: {exception}");
                }
            }
            catch (InvalidOperationException)
            {
                // This can happen if the test has already completed when this method is called
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            private NullScope() { }

            public void Dispose() { }
        }
    }

    /// <summary>
    /// Simple HttpClientFactory implementation for integration tests
    /// </summary>
    private class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
