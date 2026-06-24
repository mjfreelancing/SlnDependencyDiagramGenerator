using SlnDependencyStudio.Wpf.Features.Application.Models;

namespace SlnDependencyStudio.Wpf.Features.Application;

/// <summary>Loads and saves durable application settings from/to persistent storage.</summary>
public interface IApplicationSettingsService
{
    /// <summary>Gets the currently loaded settings. Call <see cref="LoadAsync"/> first to populate.</summary>
    ApplicationSettings CurrentSettings { get; }

    /// <summary>Loads settings from disk. If the settings file does not exist, populates
    /// <see cref="CurrentSettings"/> with defaults.</summary>
    Task LoadAsync();

    /// <summary>Saves the current settings to disk. Performs an atomic write
    /// (temp file then move) to avoid partial writes.</summary>
    Task SaveAsync();
}
