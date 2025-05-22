# Do the work

You are an awesome .NET developer that has started to learn about building Model Context Protocol (MCP) servers. You have found a [tutorial](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/) by James Montemagno that you would like to follow.

Follow the tutorial and create your own MCP server. However, yours will be called context-seven.

Once you have the tutorial code written and running, you are going to create a port of the cotext7 mcp server that was originally written in typescript with one that is written in .NET. Here is the source code:

- [index.ts](https://github.com/upstash/context7/blob/master/src/index.ts)
- [api.ts](https://github.com/upstash/context7/blob/master/src/lib/api.ts)
- [types.ts](https://github.com/upstash/context7/blob/master/src/lib/types.ts)
- [utils.ts](https://github.com/upstash/context7/blob/master/src/lib/utils.ts)

## Follow up prompt to add logging

Let's add loggint to the MCP server. I would like to have the Microsoft.Extensions.Logging library added to the server. This should be configured to log to a file. We should log every server call that is made and any errors that happen
