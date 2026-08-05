using AllOverIt.DependencyInjection.Extensions;
using AllOverIt.ReactiveUI.Factories;
using AllOverIt.ReactiveUI.Wpf.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Editors;
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

// =====================================================================================
// DI AUTO-REGISTRATION RULES (AllOverIt.DependencyInjection)
// =====================================================================================
//
// AutoRegisterScoped<TRegistrar, TMarker>() / AutoRegisterSingleton<TRegistrar, TMarker>() scan the
// assembly that contains TRegistrar (here: DependencyRegistrar) for concrete classes that implement
// TMarker (directly or transitively). Each discovered class is then registered against EVERY interface
// the class implements that "is derived from" the marker, i.e. where:
//
//     interface == TMarker  ||  interface.IsDerivedFrom(TMarker)
//
// The Filter() lambda below is what stops the marker interface itself from being registered as a
// service - without it, every concrete class would also be registered against the marker, which is
// never resolved.
//
// -----------------------------------------------------------------------------------
// WHEN AN INTERFACE IS REGISTERED
// -----------------------------------------------------------------------------------
// The interface (or one of its base interfaces) inherits the marker:
//
//     public interface IRecentProjectsStore : IStudioSingletonDependency { ... }
//     internal sealed class RecentProjectsStore : IRecentProjectsStore { ... }
//
// -> IRecentProjectsStore derives from the marker, so it IS registered and can be resolved:
//    provider.GetRequiredService<IRecentProjectsStore>() succeeds.
//
// -----------------------------------------------------------------------------------
// WHEN AN INTERFACE IS NOT REGISTERED
// -----------------------------------------------------------------------------------
// 1. The interface does NOT inherit the marker (directly or transitively). A pure constraint marker
//    such as IStudioEditor is deliberately kept free of DI lifetime semantics:
//
//        public interface IStudioEditor { ... }                                   // NOT registered
//        public interface IProjectMetadataEditor : IStudioEditor, IStudioSingletonDependency { ... }
//
//    -> IProjectMetadataEditor IS registered (it derives from the marker); IStudioEditor is NOT
//       registered (it does not derive from the marker), so it needs no filter.
//
// 2. The marker interface itself (IStudioSingletonDependency / IStudioScopedDependency). Excluded by
//    the Filter() so it never becomes a service type.
//
// 3. An interface whose marker is implemented by the CLASS instead of the interface. This is the
//    "compose at the class level" trap - it looks clean but silently breaks registration:
//
//        internal sealed class WidgetState : IWidgetState, IStudioSingletonDependency { ... }
//
//    -> The class IS discovered, but IWidgetState does NOT derive from the marker, so it is NEVER
//       registered. Resolving IWidgetState would throw. The marker must live on the interface
//       (or on a base interface it inherits) for the interface to be auto-registered.
//
// -----------------------------------------------------------------------------------
// COMPOSITION OVER INHERITANCE - GUIDING RULE
// -----------------------------------------------------------------------------------
// Keep constraint/category markers (markers used in generic `where T :` constraints, e.g. IStudioEditor)
// free of DI lifetime semantics. Compose the lifetime marker onto each concrete service interface:
//
//     public interface ISomeEditor : IStudioEditor, IStudioSingletonDependency { ... }
//
// This gives you a pure, never-registered constraint marker (no filter required) while each service
// interface is still auto-registered. Do NOT make the constraint marker inherit the lifetime marker -
// that would register the marker against every implementation (forcing a filter) and would leak DI
// lifetime semantics into the constraint's meaning.
//
// The same rules apply to the scoped (and any future transient) variant of the marker.
// =====================================================================================

/// <summary>Extension methods for registering WPF-specific Studio services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Defines extension methods for registering WPF-specific Studio services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers WPF-specific services with the service collection.</summary>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddWpfDependencies()
        {
            // Auto-register all scoped classes implementing IStudioScopedDependency in this assembly.
            services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
            {
                // Do not register the marker interface itself as a service type, only the concrete classes that implement it.
                config.Filter((serviceType, _) => serviceType != typeof(IStudioScopedDependency));
            });

            // Auto-register all singleton classes implementing IStudioSingletonDependency in this assembly.
            services.AutoRegisterSingleton<DependencyRegistrar, IStudioSingletonDependency>(config =>
            {
                // Do not register the marker interface itself as a service type, only the concrete classes that implement it.
                config.Filter((serviceType, _) => serviceType != typeof(IStudioSingletonDependency));
            });

            // Override the Source library's default (empty) provider with one that reads WPF settings.
            services.AddSingleton<ToolPathOverridesProvider>(provider =>
            {
                var settings = provider.GetRequiredService<IApplicationSettingsService>();
                return () => settings.CurrentSettings.ToolPathOverrides;
            });

            services.AddSingleton<IFileSystem, SystemFileSystem>();
            services.AddSingleton(typeof(IScopedOperationFactory<>), typeof(ScopedOperationFactory<>));
            services.AddSingleton<IStudioEditorFactory, StudioEditorFactory>();

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
