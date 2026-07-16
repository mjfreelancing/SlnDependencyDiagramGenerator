using AllOverIt.DependencyInjection.Extensions;
using AllOverIt.ReactiveUI.Factories;
using AllOverIt.ReactiveUI.Wpf.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.EmptyState;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Settings;
using SlnDependencyStudio.Wpf.Features.Solution;

namespace SlnDependencyStudio.Wpf.Extensions;

/// <summary>Extension methods for registering WPF-specific Studio services with DI.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers WPF-specific services with the service collection.</summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddWpfDependencies()
        {
            // Auto-register all scoped classes implementing IStudioScopedDependency in this assembly.
            services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
            {
                config.Filter((serviceType, _) => serviceType != typeof(IStudioScopedDependency));
            });

            // Override the Source library's default (empty) provider with one that reads WPF settings.
            services.AddSingleton<ToolPathOverridesProvider>(provider =>
            {
                var settings = provider.GetRequiredService<IApplicationSettingsService>();
                return () => settings.CurrentSettings.ToolPathOverrides;
            });

            // AutoRegisterTransient is deferred until needed by a specific phase.

            // Auto-register all singleton classes implementing IStudioSingletonDependency in this assembly.
            services.AutoRegisterSingleton<DependencyRegistrar, IStudioSingletonDependency>(config =>
            {
                config.Filter((serviceType, _) => serviceType != typeof(IStudioSingletonDependency));
            });

            services.AddSingleton(typeof(IScopedOperationFactory<>), typeof(ScopedOperationFactory<>));

            services.AddSingleton<SlnDependencyWpfAppBootstrapper>();
            services.AddSingleton<IViewFactory, ViewFactory>();

            services.RegisterWindowSingleton<MainWindowViewModel, MainWindow>();
            services.RegisterWindowTransient<SettingsWindowViewModel, SettingsWindow>();

            services.RegisterUserControlTransient<SettingsEditorViewModel, SettingsEditor>();
            services.RegisterUserControlTransient<ProjectViewModel, ProjectView>();
            services.RegisterUserControlTransient<SolutionViewModel, SolutionView>();
            services.RegisterUserControlTransient<ExportViewModel, ExportView>();
            services.RegisterUserControlTransient<DiagramsViewModel, DiagramsView>();
            services.RegisterUserControlTransient<PipelineViewModel, PipelineView>();
            services.RegisterUserControlTransient<EmptyStateViewModel, EmptyStateView>();
            services.RegisterUserControlTransient<OutputPanelViewModel, OutputPanelView>();

            return services;
        }
    }
}
