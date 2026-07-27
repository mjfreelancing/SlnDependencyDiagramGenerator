using AllOverIt.Patterns.ResourceInitialization;
using System;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Utils;

/// <summary>
/// Captures the current process environment variables on construction and restores them on disposal
/// (RAII / scope-guard pattern via <see cref="Raii{T}"/>).
/// </summary>
internal class EnvironmentVariablesMemento : Raii<Dictionary<string, string?>>
{
    /// <summary>Initializes the snapshot by capturing the current process environment.</summary>
    public EnvironmentVariablesMemento()
        : base(CaptureEnvironment, RestoreEnvironment)
    {
    }

    /// <summary>Captures all process environment variables into a snapshot dictionary.</summary>
    private static Dictionary<string, string?> CaptureEnvironment()
    {
        var vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
        var snapshot = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in vars.Keys)
        {
            var name = (string)key;
            snapshot[name] = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        return snapshot;
    }

    /// <summary>
    /// Restores environment variables that differ between <paramref name="snapshot"/> and
    /// the current process environment. Variables that existed in the snapshot but are no
    /// longer present are removed. Variables that changed since the snapshot are reverted.
    /// </summary>
    private static void RestoreEnvironment(Dictionary<string, string?> snapshot)
    {
        foreach (var (name, originalValue) in snapshot)
        {
            var currentValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

            if (!string.Equals(currentValue, originalValue, StringComparison.Ordinal))
            {
                if (originalValue is null)
                {
                    Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.Process);
                }
                else
                {
                    Environment.SetEnvironmentVariable(name, originalValue, EnvironmentVariableTarget.Process);
                }
            }
        }

        // Remove any new environment variables that were introduced during the scope
        // (e.g. by MSBuild registration) that were not present in the original snapshot.
        var currentVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        foreach (var key in currentVars.Keys)
        {
            var name = (string)key;

            if (!snapshot.ContainsKey(name))
            {
                Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.Process);
            }
        }
    }
}