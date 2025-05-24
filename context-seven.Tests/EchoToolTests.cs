using Microsoft.Extensions.Logging;
using Moq;

namespace context_seven.Tests;

public class EchoToolTests
{
    [Fact]
    public void Echo_ReturnsFormattedMessage()
    {
        // Arrange
        var message = "Hello World";
        
        // Act
        var result = EchoTool.Echo(message);
        
        // Assert
        Assert.Equal($"Hello from context-seven: {message}", result);
    }
    
    [Fact]
    public void Echo_WithEmptyString_ReturnsBaseMessage()
    {
        // Arrange
        var message = string.Empty;
        
        // Act
        var result = EchoTool.Echo(message);
        
        // Assert
        Assert.Equal("Hello from context-seven: ", result);
    }
    
    [Fact]
    public void ReverseEcho_ReturnsReversedMessage()
    {
        // Arrange
        var message = "Hello World";
        var expected = "dlroW olleH";
        
        // Act
        var result = EchoTool.ReverseEcho(message);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void ReverseEcho_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var message = string.Empty;
        
        // Act
        var result = EchoTool.ReverseEcho(message);
        
        // Assert
        Assert.Equal(string.Empty, result);
    }
    
    [Fact]
    public void ReverseEcho_WithPalindrome_ReturnsSameString()
    {
        // Arrange
        var message = "radar";
        
        // Act
        var result = EchoTool.ReverseEcho(message);
        
        // Assert
        Assert.Equal(message, result);
    }
}
