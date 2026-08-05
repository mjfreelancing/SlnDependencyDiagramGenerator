using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Tests.Integration.Support;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Integration;

[Collection("SettingsIntegration")]
/// <summary>Integration tests for <see cref="ApplicationSettingsService"/> persistence.
/// Exercises the full file I/O path that cannot be unit tested.</summary>
public class SettingsPersistenceFixture : IDisposable
{
    // Isolated scratch location — integration tests must never touch the real AppData settings/state files.
    private static readonly string SettingsDir = IntegrationTestHarness.SettingsDirectory;
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");
    private static readonly string StateFile = Path.Combine(SettingsDir, "state.json");

    public SettingsPersistenceFixture()
    {
        // Ensure clean state before each test
        CleanupFiles();
    }

    [Fact]
    public async Task Should_Return_Defaults_When_No_Files_Exist()
    {
        // Verify files don't exist
        File.Exists(SettingsFile).ShouldBeFalse();
        File.Exists(StateFile).ShouldBeFalse();

        var provider = IntegrationTestHarness.CreateServiceProvider();

        await using var scope = provider.CreateAsyncScope();

        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService.LoadAsync(TestContext.Current.CancellationToken);

        settingsService.CurrentSettings.DefaultProjectFolder.ShouldBe(string.Empty);
        settingsService.CurrentSettings.LogRetentionDays.ShouldBe(31); // ApplicationSettings.DefaultLogRetentionDays
        settingsService.CurrentSettings.Theme.ShouldBe(Models.StudioTheme.Light);
        settingsService.CurrentState.RecentProjects.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Persist_Settings_To_Disk()
    {
        var provider = IntegrationTestHarness.CreateServiceProvider();

        await using var scope = provider.CreateAsyncScope();

        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService.LoadAsync(TestContext.Current.CancellationToken);

        // Modify settings
        settingsService.CurrentSettings.DefaultProjectFolder = @"C:\TestProjects";
        settingsService.CurrentSettings.LogRetentionDays = 14;

        // Persist
        await settingsService.SaveSettingsAsync(TestContext.Current.CancellationToken);

        // Verify file was created
        File.Exists(SettingsFile).ShouldBeTrue();

        // Create a new service instance and verify load
        var provider2 = IntegrationTestHarness.CreateServiceProvider();

        await using var scope2 = provider2.CreateAsyncScope();

        var settingsService2 = scope2.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService2.LoadAsync(TestContext.Current.CancellationToken);

        settingsService2.CurrentSettings.DefaultProjectFolder.ShouldBe(@"C:\TestProjects");
        settingsService2.CurrentSettings.LogRetentionDays.ShouldBe(14);
    }

    [Fact]
    public async Task Should_Persist_State_To_Disk()
    {
        var provider = IntegrationTestHarness.CreateServiceProvider();

        await using var scope = provider.CreateAsyncScope();

        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService.LoadAsync(TestContext.Current.CancellationToken);

        // Modify state
        settingsService.CurrentState.RecentProjects.Add(@"C:\Recent\project1.sds");
        settingsService.CurrentState.RecentProjects.Add(@"C:\Recent\project2.sds");

        // Persist
        settingsService.SaveState();

        // Verify file was created
        File.Exists(StateFile).ShouldBeTrue();

        // Create a new service instance and verify load
        var provider2 = IntegrationTestHarness.CreateServiceProvider();

        await using var scope2 = provider2.CreateAsyncScope();

        var settingsService2 = scope2.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService2.LoadAsync(TestContext.Current.CancellationToken);

        settingsService2.CurrentState.RecentProjects.Count.ShouldBe(2);
        settingsService2.CurrentState.RecentProjects.ShouldContain(@"C:\Recent\project1.sds");
        settingsService2.CurrentState.RecentProjects.ShouldContain(@"C:\Recent\project2.sds");
    }

    public void Dispose()
    {
        CleanupFiles();
        GC.SuppressFinalize(this);
    }

    private static void CleanupFiles()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                File.Delete(SettingsFile);
            }

            if (File.Exists(StateFile))
            {
                File.Delete(StateFile);
            }

            if (Directory.Exists(SettingsDir))
            {
                Directory.Delete(SettingsDir, true);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
