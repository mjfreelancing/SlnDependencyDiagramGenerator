using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>Detects and validates the availability of external CLI tools on PATH.</summary>
internal sealed class ToolDetectionService : IToolDetectionService
{
    private readonly Dictionary<DiagramFormat, Func<CancellationToken, Task<ToolStatus>>> _toolsAvailability;
    private readonly ILogger<ToolDetectionService> _logger;

    public ToolDetectionService(ILogger<ToolDetectionService> logger)
    {
        _toolsAvailability = new()
        {
            { DiagramFormat.D2, token => CheckToolAvailabilityAsync("d2", cancellationToken: token) },
            { DiagramFormat.Mermaid, token => CheckToolAvailabilityAsync("mmdc", cancellationToken: token) }
        };

        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public async Task<ToolStatus> CheckToolAvailabilityAsync(string toolName, string? explicitPath = null, CancellationToken cancellationToken = default)
    {
        if (explicitPath.IsNotNullOrEmpty())
        {
            _logger.LogInformation("Checking availability of {ToolName} at {ExplicitPath}", toolName, explicitPath);

            return await CheckExplicitPathAsync(toolName, explicitPath, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Checking availability of {ToolName} on PATH", toolName);

        return await CheckPathAsync(toolName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ToolReadinessResult> CheckConfiguredToolsAsync(DiagramFormat[] diagramFormats, CancellationToken cancellationToken)
    {
        if (diagramFormats.Length == 0)
        {
            _logger.LogInformation("No diagram formats configured, skipping tool availability checks.");

            return new ToolReadinessResult
            {
                ToolStatuses = []
            };
        }

        var statuses = await Task.WhenAll(
            _toolsAvailability
                .Where(kvp => diagramFormats.Contains(kvp.Key))
                .Select(kvp => kvp.Value.Invoke(cancellationToken)))
            .ConfigureAwait(false);

        return new ToolReadinessResult
        {
            ToolStatuses = statuses
        };
    }

    /// <summary>Throws if a required tool is not available, with a descriptive message.</summary>
    /// <param name="toolName">The tool name.</param>
    /// <param name="missingToolMessage">The error message if the tool is missing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="DependencyGeneratorException">Thrown when the tool is not available.</exception>
    internal static async Task EnsureToolAvailableAsync(string toolName, string missingToolMessage, CancellationToken cancellationToken)
    {
        if (!await IsToolOnPathAsync(toolName, cancellationToken).ConfigureAwait(false))
        {
            throw new DependencyGeneratorException(missingToolMessage);
        }
    }

    /// <summary>Checks if a tool is available on PATH using platform-appropriate lookup.</summary>
    internal static async Task<bool> IsToolOnPathAsync(string toolName, CancellationToken cancellationToken)
    {
        var locator = OperatingSystem.IsWindows() ? "where" : "which";

        try
        {
            using var executor = ProcessBuilder
                .For(locator)
                .WithNoWindow()
                .WithArguments(toolName)
                .BuildProcessExecutor();

            var result = await executor
                .ExecuteBufferedAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<ToolStatus> CheckExplicitPathAsync(string toolName, string explicitPath, CancellationToken cancellationToken)
    {
        if (explicitPath.IsNullOrEmpty())
        {
            return new ToolStatus
            {
                ToolName = toolName,
                IsAvailable = false,
                ErrorMessage = $"Explicit path for '{toolName}' is empty."
            };
        }

        var exists = File.Exists(explicitPath);

        return exists
            ? new ToolStatus
            {
                ToolName = toolName,
                IsAvailable = true,
                ResolvedPath = explicitPath
            }
            : new ToolStatus
            {
                ToolName = toolName,
                IsAvailable = false,
                ErrorMessage = $"The specified path for '{toolName}' was not found: {explicitPath}"
            };
    }

    private static async Task<ToolStatus> CheckPathAsync(string toolName, CancellationToken cancellationToken)
    {
        var available = await IsToolOnPathAsync(toolName, cancellationToken).ConfigureAwait(false);

        return available
            ? new ToolStatus
            {
                ToolName = toolName,
                IsAvailable = true
            }
            : new ToolStatus
            {
                ToolName = toolName,
                IsAvailable = false,
                ErrorMessage = $"'{toolName}' was not found on PATH."
            };
    }
}