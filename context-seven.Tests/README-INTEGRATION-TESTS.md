# Context-Seven Integration Tests

This directory contains integration tests for the Context-Seven MCP server. These tests make real API calls to the Context7 service and verify that the tools function correctly with the actual external service.

## Test Structure

The integration tests are organized in the `Context7ToolsIntegrationTests.cs` file and focus on:

1. **Real API Calls**: Making actual HTTP requests to the Context7 API
2. **Response Handling**: Properly handling various response formats and errors
3. **Error Conditions**: Testing behavior with invalid inputs or service errors

## Running Integration Tests

### Prerequisites

- .NET 9.0 SDK
- Internet connection (to access the Context7 API)

### Running Tests

By default, all integration tests are skipped to avoid making unnecessary API calls during regular test runs. To run these tests:

```powershell
# Run all integration tests
dotnet test --filter "Category=Integration" /p:RunIntegrationTests=true

# Run a specific integration test
dotnet test --filter "FullyQualifiedName=context_seven.Tests.Context7ToolsIntegrationTests.ResolveLibraryId_WithRealApi_HandlesNonExistentLibrary"
```

> **Note**: Before running integration tests, you need to remove the `Skip` attribute from the test methods or change the test configuration.

### Test Categories

The integration tests include:

- **Library Search Tests**: Testing the ability to search for libraries using the Context7 API
- **Documentation Retrieval Tests**: Testing the ability to retrieve documentation for specific libraries
- **Error Handling Tests**: Verifying correct behavior when given invalid inputs

## Implementation Notes

### Handling API Changes

The integration tests use specialized models in the `IntegrationTestModels.cs` file that are designed to be more resilient to changes in the Context7 API's response format. This helps prevent test failures when minor changes occur in the API.

### Logging

The tests include comprehensive logging to help debug any issues:

- Console output for immediate feedback
- File logging in the `logs` directory for detailed inspection
- xUnit test output for test-specific information

## Future Improvements

1. Add authentication if/when Context7 API requires it
2. Expand testing to cover rate limiting and other API constraints
3. Add performance benchmarks for API calls

## Running in CI/CD

For CI/CD pipelines, these tests should be run only on specific branches or as a separate job to avoid unnecessary API calls.
