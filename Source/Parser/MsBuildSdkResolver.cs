using Microsoft.Build.Locator;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

#if NET10_0_OR_GREATER
using System.Threading;
#endif

namespace SlnDependencyDiagramGenerator.Parser;

/*
        Why this file exists:
        - We now read evaluated MSBuild items (for example, ProjectReference and FrameworkReference)
            instead of only raw project XML.
        - Evaluating SDK-style projects in-process requires MSBuild SDK/toolset discovery to be
            registered first.

        What this file does:
        - Performs one-time, thread-safe MSBuildLocator registration before project evaluation.
        - Prefers locator defaults first, then falls back to discovered Visual Studio instances.

        What this file is related to (and what it is NOT):
        - Related to parser infrastructure bootstrapping used before SolutionParser is created,
            and again defensively inside parsing.
        - Not related to parsing user project business content.
        - Not related to dependency inclusion/exclusion decisions.

        Why it matters:
        - Prevents runtime evaluation failures like:
            "The SDK 'Microsoft.NET.Sdk' specified could not be found".
*/
internal static class MsBuildSdkResolver
{
#if NET10_0_OR_GREATER
    private static readonly Lock SyncRoot = new();
#else
    private static readonly object SyncRoot = new();
#endif

    private static bool _isInitialized;
    private static string _registrationSummary = "MSBuild registration has not yet been attempted.";
    private static string _registeredInstanceName = "<none>";
    private static string _registeredInstanceVersion = "<none>";
    private static string _registeredInstancePath = "<none>";

    /// <summary>
    /// Ensures an MSBuild instance is registered for the current process before any
    /// SDK-style project evaluation occurs.
    /// </summary>
    /// <remarks>
    /// This method is idempotent and thread-safe. It can be called repeatedly from different
    /// parser entry points without re-registering MSBuild.
    /// </remarks>
    public static void EnsureInitialized()
    {
        // Fast path for repeat calls after registration has already happened.
        if (_isInitialized || MSBuildLocator.IsRegistered)
        {
            _isInitialized = true;

            return;
        }

        lock (SyncRoot)
        {
            if (_isInitialized || MSBuildLocator.IsRegistered)
            {
                _isInitialized = true;

                return;
            }

            RegisterMsBuildInstance();

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Returns a diagnostic snapshot of resolver registration and currently loaded
    /// <c>Microsoft.Build</c> assembly information.
    /// </summary>
    /// <returns>
    /// A multi-line diagnostic string describing runtime, registration state, and loaded assembly paths.
    /// </returns>
    internal static string GetDiagnostics()
    {
        var loadedAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name.Equals("Microsoft.Build", StringComparison.OrdinalIgnoreCase));

        var loadedAssemblyName = loadedAssembly?.FullName ?? "<not loaded>";
        var loadedAssemblyPath = loadedAssembly?.Location ?? "<not loaded>";

        var builder = new StringBuilder();

        builder.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine($"MSBuildLocator.IsRegistered: {MSBuildLocator.IsRegistered}");
        builder.AppendLine($"Registration summary: {_registrationSummary}");
        builder.AppendLine($"Registered instance name: {_registeredInstanceName}");
        builder.AppendLine($"Registered instance version: {_registeredInstanceVersion}");
        builder.AppendLine($"Registered instance path: {_registeredInstancePath}");
        builder.AppendLine($"Loaded Microsoft.Build assembly: {loadedAssemblyName}");
        builder.AppendLine($"Loaded Microsoft.Build assembly path: {loadedAssemblyPath}");

        return builder.ToString();
    }

    /// <summary>
    /// Registers an MSBuild instance using default locator behavior first, then Visual Studio
    /// instance fallback when required.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no compatible MSBuild instance can be registered.
    /// </exception>
    private static void RegisterMsBuildInstance()
    {
        // Let the locator pick the best local instance first (typically .NET SDK MSBuild).
        if (TryRegisterDefaults())
        {
            _registrationSummary = "Registered using MSBuildLocator.RegisterDefaults().";
            _registeredInstanceName = "RegisterDefaults";
            _registeredInstanceVersion = "<auto>";
            _registeredInstancePath = "<auto>";

            return;
        }

        // Fallback: walk discovered VS instances newest-first and use the first valid one.
        var instances = MSBuildLocator.QueryVisualStudioInstances()
            .OrderByDescending(item => item.Version)
            .Where(item => File.Exists(Path.Combine(item.MSBuildPath, "Microsoft.Build.dll")))
            .ToArray();

        foreach (var instance in instances)
        {
            try
            {
                MSBuildLocator.RegisterInstance(instance);

                _registrationSummary = $"Registered using discovered Visual Studio instance '{instance.Name}' ({instance.Version}) at '{instance.MSBuildPath}'.";
                _registeredInstanceName = instance.Name;
                _registeredInstanceVersion = instance.Version.ToString();
                _registeredInstancePath = instance.MSBuildPath;

                return;
            }
            catch (Exception exception) when (exception is InvalidOperationException or FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                // Try the next candidate if this instance cannot be loaded in this process.
            }
        }

        throw new InvalidOperationException("Failed to register an MSBuild instance for SDK-style project evaluation. Verify a compatible .NET SDK or Visual Studio Build Tools installation is available.");
    }

    /// <summary>
    /// Attempts to register the default MSBuild instance discovered by <see cref="MSBuildLocator"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when default registration succeeds; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryRegisterDefaults()
    {
        try
        {
            MSBuildLocator.RegisterDefaults();

            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            _registrationSummary = $"RegisterDefaults failed: {exception.GetType().Name}: {exception.Message}";

            return false;
        }
    }
}
