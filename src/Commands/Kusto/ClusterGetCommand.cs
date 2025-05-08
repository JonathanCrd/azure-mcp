// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine.Parsing;
using AzureMcp.Arguments.Kusto;
using AzureMcp.Models.Command;
using AzureMcp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AzureMcp.Commands.Kusto;

public sealed class ClusterGetCommand : BaseClusterCommand<ClusterGetArguments>
{
    private readonly ILogger<ClusterGetCommand> _logger;

    public ClusterGetCommand(ILogger<ClusterGetCommand> logger)
    {
        _logger = logger;
    }

    protected override string GetCommandName() => "get";

    protected override string GetCommandDescription() =>
        """
        Get details for a specific Kusto cluster. Requires `cluster-name` and `subscription`.
        The response includes the `clusterUri` property for use in subsequent commands.
        """;

    [McpServerTool(Destructive = false, ReadOnly = true)]
    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var args = BindArguments(parseResult);

        try
        {
            if (!await ProcessArguments(context, args))
                return context.Response;

            var kusto = context.GetService<IKustoService>();
            var cluster = await kusto.GetCluster(
                args.Subscription!,
                args.ClusterName!,
                args.Tenant,
                args.RetryPolicy);

            context.Response.Results = cluster is null ?
            null : ResponseResult.Create(new ClusterGetCommandResult(cluster), KustoJsonContext.Default.ClusterGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred getting Kusto cluster details. Cluster: {Cluster}.", args.ClusterName);
            HandleException(context.Response, ex);
        }

        return context.Response;
    }

    internal record ClusterGetCommandResult(KustoClusterResourceProxy Cluster);
}
