using Microsoft.Build.Locator;
using SlnDependencyDiagramGenerator.Utils;
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

    private static bool IsInitialized;
    private static string RegistrationSummary = "MSBuild registration has not yet been attempted.";
    private static string RegisteredInstanceName = "<none>";
    private static string RegisteredInstanceVersion = "<none>";
    private static string RegisteredInstancePath = "<none>";

    /// <summary>
    /// Ensures an MSBuild instance is registered for the current process before any
    /// SDK-style project evaluation occurs.
    /// </summary>
    /// <remarks>
    /// This method is idempotent and thread-safe. It can be called repeatedly from different
    /// parser entry points without re-registering MSBuild.
    /// <para/>
    /// MSBuildLocator.RegisterDefaults() and the loaded MSBuild assemblies can modify process-level
    /// environment variables (e.g. <c>MSBuildSDKPath</c>, <c>MSBUILD_EXE_PATH</c>). This method snapshots the
    /// environment before registration and restores any changes afterwards, so child processes
    /// launched later by the host application always inherit the original unmodified environment.
    /// <para/>
    /// References:
    /// <see href="https://learn.microsoft.com/dotnet/core/tools/sdk-errors/netsdk1045#path-environment-variable">NETSDK1045 — MSBuildSDKPath environment variable</see>
    /// and
    /// <see href="https://learn.microsoft.com/visualstudio/msbuild/errors/msb4193">MSB4193 — MSBUILD_EXE_PATH environment variable</see>
    /// </remarks>
    public static void EnsureInitialized()
    {
        // Fast path for repeat calls after registration has already happened.
        if (IsInitialized || MSBuildLocator.IsRegistered)
        {
            IsInitialized = true;

            return;
        }

        lock (SyncRoot)
        {
            if (IsInitialized || MSBuildLocator.IsRegistered)
            {
                IsInitialized = true;

                return;
            }

            // MSBuildLocator.RegisterDefaults() and the loaded MSBuild assemblies can modify or
            // introduce process-level environment variables (e.g. MSBuildSDKPath, MSBUILD_EXE_PATH).
            // Child processes inherit the parent's environment, so any such changes can cause
            // pre-generation commands like 'dotnet restore' to fail on subsequent invocations.
            // EnvironmentVariablesMemento captures the state before and restores it after.
            //
            // References:
            //   https://learn.microsoft.com/dotnet/core/tools/sdk-errors/netsdk1045#path-environment-variable
            //   https://learn.microsoft.com/visualstudio/msbuild/errors/msb4193
            using (new EnvironmentVariablesMemento())
            {
                RegisterMsBuildInstance();
            }

            IsInitialized = true;
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
            .FirstOrDefault(assembly => assembly.GetName().Name!.Equals("Microsoft.Build", StringComparison.OrdinalIgnoreCase));

        var loadedAssemblyName = loadedAssembly?.FullName ?? "<not loaded>";
        var loadedAssemblyPath = loadedAssembly?.Location ?? "<not loaded>";

        var builder = new StringBuilder();

        builder.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine($"MSBuildLocator.IsRegistered: {MSBuildLocator.IsRegistered}");
        builder.AppendLine($"Registration summary: {RegistrationSummary}");
        builder.AppendLine($"Registered instance name: {RegisteredInstanceName}");
        builder.AppendLine($"Registered instance version: {RegisteredInstanceVersion}");
        builder.AppendLine($"Registered instance path: {RegisteredInstancePath}");
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
            RegistrationSummary = "Registered using MSBuildLocator.RegisterDefaults().";
            RegisteredInstanceName = "RegisterDefaults";
            RegisteredInstanceVersion = "<auto>";
            RegisteredInstancePath = "<auto>";

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

                RegistrationSummary = $"Registered using discovered Visual Studio instance '{instance.Name}' ({instance.Version}) at '{instance.MSBuildPath}'.";
                RegisteredInstanceName = instance.Name;
                RegisteredInstanceVersion = instance.Version.ToString();
                RegisteredInstancePath = instance.MSBuildPath;

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
            RegistrationSummary = $"RegisterDefaults failed: {exception.GetType().Name}: {exception.Message}";

            return false;
        }
    }
}
