using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Extensions;
using SlnDependencyStudio.Wpf.Features.Application.Models;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Application.Extensions;

public class ApplicationSettingsServiceExtensionsFixture
{
    private readonly IApplicationSettingsService _settingsService = Substitute.For<IApplicationSettingsService>();
    private readonly ApplicationSettings _settings = new();

    public ApplicationSettingsServiceExtensionsFixture()
    {
        _settingsService.CurrentSettings.Returns(_settings);
    }

    public class ResolveProjectFolder : ApplicationSettingsServiceExtensionsFixture
    {
        [Fact]
        public void Should_Return_Folder_When_Set_And_Exists()
        {
            _settings.DefaultProjectFolder = Environment.CurrentDirectory;

            var result = _settingsService.ResolveProjectFolder();

            result.ShouldBe(Environment.CurrentDirectory);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Does_Not_Exist()
        {
            _settings.DefaultProjectFolder = @"C:\NonExistentFolder_12345";

            var result = _settingsService.ResolveProjectFolder();

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Is_Null()
        {
            _settings.DefaultProjectFolder = null!;

            var result = _settingsService.ResolveProjectFolder();

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Is_Empty()
        {
            _settings.DefaultProjectFolder = string.Empty;

            var result = _settingsService.ResolveProjectFolder();

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Is_Whitespace()
        {
            _settings.DefaultProjectFolder = "   ";

            var result = _settingsService.ResolveProjectFolder();

            result.ShouldBe(string.Empty);
        }
    }
}
