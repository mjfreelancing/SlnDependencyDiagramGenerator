using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Tests.Integration.Support;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Integration;

[Collection("SettingsIntegration")]
/// <summary>Tests that settings from a previous schema version can be loaded,
/// new fields receive documented defaults, and the file is upgraded on save.</summary>
public class SettingsMigrationFixture : IDisposable
{
    // Isolated scratch location — integration tests must never touch the real AppData settings files.
    private static readonly string SettingsDir = IntegrationTestHarness.SettingsDirectory;
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public SettingsMigrationFixture()
    {
        Cleanup();
    }

    [Fact]
    public async Task Should_Load_Legacy_Settings_With_Defaults()
    {
        // Write a minimal "legacy" settings file that's missing newer fields
        var legacyJson = """{"defaultProjectFolder": "C:\\Legacy","theme": "Dark"}""";

        Directory.CreateDirectory(SettingsDir);

        await File.WriteAllTextAsync(SettingsFile, legacyJson);

        var provider = IntegrationTestHarness.CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService.LoadAsync();

        // Legacy field loads correctly
        settingsService.CurrentSettings.DefaultProjectFolder.ShouldBe(@"C:\Legacy");
        settingsService.CurrentSettings.Theme.ShouldBe(Models.StudioTheme.Dark);

        // New fields not in legacy JSON get defaults
        settingsService.CurrentSettings.LogRetentionDays.ShouldBe(31); // DefaultLogRetentionDays
        settingsService.CurrentSettings.ToolPathOverrides.ShouldBeEmpty();
        settingsService.CurrentSettings.Output.WrapContent.ShouldBeFalse();
        settingsService.CurrentSettings.Output.IsVerboseLogging.ShouldBeFalse();
        settingsService.CurrentSettings.Output.AutoScroll.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Upgrade_Settings_On_Save()
    {
        var legacyJson = """{"defaultProjectFolder": "C:\\Upgrade"}""";

        Directory.CreateDirectory(SettingsDir);
        await File.WriteAllTextAsync(SettingsFile, legacyJson);

        var provider = IntegrationTestHarness.CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        await settingsService.LoadAsync();

        // Modify and save
        settingsService.CurrentSettings.DefaultProjectFolder = @"C:\Upgraded";
        await settingsService.SaveSettingsAsync();

        // Re-read the raw file — verify it contains the current schema
        var rawJson = await File.ReadAllTextAsync(SettingsFile);
        rawJson.ShouldContain("logRetentionDays");
        rawJson.ShouldContain("toolPathOverrides");
        rawJson.ShouldContain("output");
        rawJson.ShouldContain("wrapContent");
        rawJson.ShouldContain("defaultProjectFolder");
        rawJson.ShouldContain(@"C:\\Upgraded");
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    private static void Cleanup()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                File.Delete(SettingsFile);
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
