# Context-Seven MCP Server

Context-Seven is a .NET implementation of the [Context7](https://github.com/upstash/context7) Model Context Protocol (MCP) server. It provides up-to-date code documentation for various libraries and packages directly within your AI coding assistant context.

## Features

- **EchoTool**: Simple echo and reverse echo functionality for testing
- **Context7Tools**:
  - `ResolveLibraryId`: Search for libraries and get Context7-compatible library IDs
  - `GetLibraryDocs`: Fetch up-to-date documentation for a specific library
- **Comprehensive Logging**: All server operations are logged for troubleshooting and monitoring

## Usage

### Prerequisites

- .NET 9.0 or later
- VS Code with GitHub Copilot or another MCP client

### Setup

1. Clone this repository
2. Build the project:

   ```console
   dotnet build
   ```

3. Configure in VS Code:
   - Create or update `.vscode/mcp.json` with:

   ```json
   {
       "inputs": [],
       "servers": {
           "context-seven": {
               "type": "stdio",
               "command": "dotnet",
               "args": [
                   "run",
                   "--project",
                   "PATH_TO_YOUR_PROJECT\\context-seven.csproj"
               ]
           }
       }
   }
   ```

4. Start using in your AI coding assistant by calling the available tools:
   - `resolve-library-id`: Find Context7-compatible library IDs
   - `get-library-docs`: Get documentation for a specific library

## Logging

The server includes comprehensive logging of all operations:

### Log Configuration

- **Log Location**: Logs are stored in the `logs` directory in the application's base directory
- **Log Format**: Files are named `context-seven-log-{Date}.txt` where `{Date}` is the current date (yyyyMMdd)
- **Log Levels**:
  - Information: Normal operations, tool calls, API interactions
  - Warning: Issues that don't prevent operation
  - Error: Exceptions and critical failures

### What Gets Logged

- Server startup and configuration
- All tool invocations with their parameters
- Tool responses (truncated if too large)
- Context7 API calls and responses
- Any errors or exceptions

### Implementation

The logging system uses a combination of:

1. **Microsoft.Extensions.Logging**: For the main application and service logging
2. **Custom McpLogging**: A static helper class that logs all MCP tool operations

This provides a complete picture of the server's operation for troubleshooting and auditing purposes.

## How It Works

Context-Seven connects to the Context7 API to fetch up-to-date documentation for various libraries and frameworks. It helps AI coding assistants provide more accurate and relevant documentation directly in your coding workflow.

## License

MIT
