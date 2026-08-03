using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Settings;
using SlnDependencyStudio.Wpf.Features.Theming;
using SlnDependencyStudio.Wpf.Models;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Settings;

[Collection(nameof(ReactiveUIInitializer))]
public class SettingsEditorViewModelFixture
{
    private readonly IApplicationSettingsService _settingsService = Substitute.For<IApplicationSettingsService>();
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();
    private readonly ILogger<SettingsEditorViewModel> _logger = Substitute.For<ILogger<SettingsEditorViewModel>>();
    private readonly ApplicationSettings _settings = new();
    private readonly SettingsEditorViewModel _viewModel;

    public SettingsEditorViewModelFixture()
    {
        _settingsService.CurrentSettings.Returns(_settings);
        _viewModel = new SettingsEditorViewModel(_settingsService, _themeService, _logger);
    }

    public class Construction : SettingsEditorViewModelFixture
    {
        [Fact]
        public void Should_Populate_DefaultProjectFolder_From_Settings()
        {
            _settings.DefaultProjectFolder = @"C:\Projects";

            var vm = new SettingsEditorViewModel(_settingsService, _themeService, _logger);

            vm.DefaultProjectFolder.ShouldBe(@"C:\Projects");
        }

        [Fact]
        public void Should_Populate_LogRetentionDays_From_Settings()
        {
            _settings.LogRetentionDays = 14;

            var vm = new SettingsEditorViewModel(_settingsService, _themeService, _logger);

            vm.LogRetentionDays.ShouldBe(14);
        }

        [Fact]
        public void Should_Default_LogRetentionDays_To_Default()
        {
            _viewModel.LogRetentionDays.ShouldBe(ApplicationSettings.DefaultLogRetentionDays);
        }

        [Fact]
        public void Should_Set_IsDarkTheme_When_Theme_Is_Dark()
        {
            _settings.Theme = StudioTheme.Dark;

            var vm = new SettingsEditorViewModel(_settingsService, _themeService, _logger);

            vm.IsDarkTheme.ShouldBeTrue();
        }

        [Fact]
        public void Should_Set_IsDarkTheme_False_When_Theme_Is_Light()
        {
            _viewModel.IsDarkTheme.ShouldBeFalse();
        }

        [Fact]
        public void Should_Capture_OriginalTheme()
        {
            var original = _viewModel.GetOriginalTheme();
            original.ShouldBeSameAs(StudioTheme.Light);
        }

        [Fact]
        public void Should_Have_BrowseCommands()
        {
            _viewModel.BrowseDefaultProjectFolderCommand.ShouldNotBeNull();
            _viewModel.BrowseD2ToolPathCommand.ShouldNotBeNull();
            _viewModel.BrowseMmdcToolPathCommand.ShouldNotBeNull();
        }
    }

    public class ApplyToSettings : SettingsEditorViewModelFixture
    {
        [Fact]
        public void Should_Write_DefaultProjectFolder()
        {
            _viewModel.DefaultProjectFolder = @"C:\Custom";
            var target = new ApplicationSettings();

            _viewModel.ApplyToSettings(target);

            target.DefaultProjectFolder.ShouldBe(@"C:\Custom");
        }

        [Fact]
        public void Should_Write_LogRetentionDays()
        {
            _viewModel.LogRetentionDays = 7;
            var target = new ApplicationSettings();

            _viewModel.ApplyToSettings(target);

            target.LogRetentionDays.ShouldBe(7);
        }

        [Fact]
        public void Should_Write_Light_Theme()
        {
            _viewModel.IsDarkTheme = false;
            var target = new ApplicationSettings();

            _viewModel.ApplyToSettings(target);

            target.Theme.ShouldBeSameAs(StudioTheme.Light);
        }

        [Fact]
        public void Should_Write_Dark_Theme()
        {
            _viewModel.IsDarkTheme = true;
            var target = new ApplicationSettings();

            _viewModel.ApplyToSettings(target);

            target.Theme.ShouldBeSameAs(StudioTheme.Dark);
        }

        [Fact]
        public void Should_Write_D2ToolPath()
        {
            _viewModel.D2ToolPath = @"C:\tools\d2.exe";
            var target = new ApplicationSettings();

            _viewModel.ApplyToSettings(target);

            target.ToolPathOverrides["d2"].ShouldBe(@"C:\tools\d2.exe");
        }

        [Fact]
        public void Should_Remove_D2ToolPath_When_Empty()
        {
            _settings.ToolPathOverrides["d2"] = @"C:\old\d2.exe";
            _viewModel.D2ToolPath = string.Empty;
            var target = new ApplicationSettings();

            _viewModel.ApplyToSettings(target);

            target.ToolPathOverrides.ShouldNotContainKey("d2");
        }
    }

    public class ThemeToggle : SettingsEditorViewModelFixture
    {
        [Fact]
        public void Should_Apply_Dark_Theme_When_Toggled()
        {
            _viewModel.IsDarkTheme = true;

            _themeService.Received(1).ApplyTheme(StudioTheme.Dark);
        }

        [Fact]
        public void Should_Apply_Light_Theme_When_Toggled_Back()
        {
            // Construction triggers ApplyTheme(Light) once via WhenAnyValue initial value
            _viewModel.IsDarkTheme = true;
            _viewModel.IsDarkTheme = false;

            // Called twice: construction (Light) + toggle back (Light)
            _themeService.Received(2).ApplyTheme(StudioTheme.Light);
        }
    }
}
