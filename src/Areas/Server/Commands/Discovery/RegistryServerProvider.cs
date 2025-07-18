// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Server.Models;
using ModelContextProtocol.Client;

namespace AzureMcp.Areas.Server.Commands.Discovery;

/// <summary>
/// Provides an MCP server implementation based on registry configuration.
/// Supports stdio transport mechanism.
/// </summary>
/// <param name="id">The unique identifier for the server.</param>
/// <param name="serverInfo">Configuration information for the server.</param>
public sealed class RegistryServerProvider(string id, RegistryServerInfo serverInfo) : IMcpServerProvider
{
    private readonly string _id = id;
    private readonly RegistryServerInfo _serverInfo = serverInfo;

    /// <summary>
    /// Creates metadata that describes this registry-based server.
    /// </summary>
    /// <returns>A metadata object containing the server's identity and description.</returns>
    public McpServerMetadata CreateMetadata()
    {
        return new McpServerMetadata
        {
            Id = _id,
            Name = _id,
            Description = _serverInfo.Description ?? string.Empty
        };
    }

    /// <summary>
    /// Creates an MCP client for this registry-based server.
    /// </summary>
    /// <param name="clientOptions">Options to configure the client behavior.</param>
    /// <returns>A configured MCP client ready for use.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server configuration doesn't specify a valid transport mechanism.</exception>
    public async Task<IMcpClient> CreateClientAsync(McpClientOptions clientOptions)
    {
        if (!string.IsNullOrWhiteSpace(_serverInfo.Type) && _serverInfo.Type.Equals("stdio", StringComparison.OrdinalIgnoreCase))
        {
            return await CreateStdioClientAsync(clientOptions);
        }
        else
        {
            throw new InvalidOperationException($"Registry server '{_id}' does not have a valid transport type. Only 'stdio' is supported.");
        }
    }

    /// <summary>
    /// Creates an MCP client that communicates with the server using stdio (standard input/output).
    /// </summary>
    /// <param name="clientOptions">Options to configure the client behavior.</param>
    /// <returns>A configured MCP client using stdio transport.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server configuration doesn't specify a valid command for stdio transport.</exception>
    private async Task<IMcpClient> CreateStdioClientAsync(McpClientOptions clientOptions)
    {
        if (string.IsNullOrWhiteSpace(_serverInfo.Command))
        {
            throw new InvalidOperationException($"Registry server '{_id}' does not have a valid command for stdio transport.");
        }

        // Merge current system environment variables with serverInfo.Env (serverInfo.Env overrides system)
        var env = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string?)e.Value);

        if (_serverInfo.Env != null)
        {
            foreach (var kvp in _serverInfo.Env)
            {
                env[kvp.Key] = kvp.Value;
            }
        }

        var transportOptions = new StdioClientTransportOptions
        {
            Name = _id,
            Command = _serverInfo.Command,
            Arguments = _serverInfo.Args,
            EnvironmentVariables = env
        };

        var clientTransport = new StdioClientTransport(transportOptions);
        return await McpClientFactory.CreateAsync(clientTransport, clientOptions);
    }
}
