using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.Services;

/// <summary>
/// Detects and reports the availability of external CLI tools (d2, mmdc)
/// required by diagram renderers.
/// </summary>
public interface IToolStatusService : IStudioSingletonDependency
{
    /// <summary>An observable list of tool status entries, updated on each scan.</summary>
    IObservable<IReadOnlyList<ToolStatusEntry>> ToolStatuses { get; }

    /// <summary>Re-scans for all required tools and updates <see cref="ToolStatuses"/>.</summary>
    Task RescanAsync(CancellationToken cancellationToken);
}
