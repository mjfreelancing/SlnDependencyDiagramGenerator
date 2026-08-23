using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Extensions;
using SlnDependencyStudio.Wpf.Features.Application.Models;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Application.Extensions;

public class ApplicationSettingsServiceExtensionsFixture
{
    private readonly IApplicationSettingsService _settingsService = Substitute.For<IApplicationSettingsService>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
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
            _settings.DefaultProjectFolder = @"C:\Projects";
            _fileSystem.DirectoryExists(@"C:\Projects").Returns(true);

            var result = _settingsService.ResolveProjectFolder(_fileSystem);

            result.ShouldBe(@"C:\Projects");
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Does_Not_Exist()
        {
            _settings.DefaultProjectFolder = @"C:\NonExistentFolder_12345";
            _fileSystem.DirectoryExists(@"C:\NonExistentFolder_12345").Returns(false);

            var result = _settingsService.ResolveProjectFolder(_fileSystem);

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Is_Null()
        {
            _settings.DefaultProjectFolder = null!;

            var result = _settingsService.ResolveProjectFolder(_fileSystem);

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Is_Empty()
        {
            _settings.DefaultProjectFolder = string.Empty;

            var result = _settingsService.ResolveProjectFolder(_fileSystem);

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Empty_When_Folder_Is_Whitespace()
        {
            _settings.DefaultProjectFolder = "   ";

            var result = _settingsService.ResolveProjectFolder(_fileSystem);

            result.ShouldBe(string.Empty);
        }
    }
}
