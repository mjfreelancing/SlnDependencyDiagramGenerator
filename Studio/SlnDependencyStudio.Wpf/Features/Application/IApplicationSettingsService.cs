using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Application.Models;

namespace SlnDependencyStudio.Wpf.Features.Application;

/// <summary>Loads and saves durable application settings from/to persistent storage.</summary>
public interface IApplicationSettingsService : IStudioSingletonDependency
{
    /// <summary>Gets the currently loaded settings. Call <see cref="LoadAsync"/> first to populate.</summary>
    ApplicationSettings CurrentSettings { get; }

    /// <summary>Gets the currently loaded application state. Call <see cref="LoadAsync"/> first to populate.</summary>
    ApplicationState CurrentState { get; }

    /// <summary>Loads settings and state from disk. If files do not exist, populates
    /// <see cref="CurrentSettings"/> and <see cref="CurrentState"/> with defaults.</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the current settings to disk. Performs an atomic write
    /// (temp file then move) to avoid partial writes.</summary>
    Task SaveSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the current application state to disk. Performs an atomic write
    /// (temp file then move) to avoid partial writes.</summary>
    void SaveState();
}
