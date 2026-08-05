using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Models;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Application;

public class ApplicationSettingsServiceFixture
{
    private static readonly string TestDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private static readonly string SettingsFilePath = Path.Combine(TestDir, "settings.json");
    private static readonly string StateFilePath = Path.Combine(TestDir, "state.json");

    public class LoadAsync : ApplicationSettingsServiceFixture
    {
        [Fact]
        public async Task Should_Return_Defaults_When_No_Files_Exist()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.FileExists(Arg.Any<string>()).Returns(false);

            var service = CreateService(fileSystem);

            await service.LoadAsync(TestContext.Current.CancellationToken);

            service.CurrentSettings.DefaultProjectFolder.ShouldBe(string.Empty);
            service.CurrentSettings.LogRetentionDays.ShouldBe(31);
            service.CurrentSettings.Theme.ShouldBe(StudioTheme.Light);
            service.CurrentState.RecentProjects.ShouldBeEmpty();
            service.CurrentState.WindowPlacement.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Not_Check_State_File_When_Settings_File_Exists()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();

            var settingsJson = """{"defaultProjectFolder": "C:\\Projects", "logRetentionDays": 14}""";
            var settingsStream = new MemoryStream(Encoding.UTF8.GetBytes(settingsJson));

            fileSystem.FileExists(SettingsFilePath).Returns(true);
            fileSystem.OpenRead(SettingsFilePath).Returns(settingsStream);

            serializer
                .DeserializeAsync<ApplicationSettings>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationSettings
                {
                    DefaultProjectFolder = @"C:\Projects",
                    LogRetentionDays = 14
                });

            // State file does not exist
            fileSystem.FileExists(StateFilePath).Returns(false);

            var service = CreateService(serializer, fileSystem);

            await service.LoadAsync(TestContext.Current.CancellationToken);

            service.CurrentSettings.DefaultProjectFolder.ShouldBe(@"C:\Projects");
            service.CurrentSettings.LogRetentionDays.ShouldBe(14);
        }

        [Fact]
        public async Task Should_Load_Both_Settings_And_State()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();

            fileSystem.FileExists(SettingsFilePath).Returns(true);
            fileSystem.FileExists(StateFilePath).Returns(true);

            var settingsStream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            var stateStream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            fileSystem.OpenRead(SettingsFilePath).Returns(settingsStream);
            fileSystem.OpenRead(StateFilePath).Returns(stateStream);

            serializer
                .DeserializeAsync<ApplicationSettings>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationSettings
                {
                    DefaultProjectFolder = @"C:\Projects",
                    Theme = StudioTheme.Dark
                });

            serializer
                .DeserializeAsync<ApplicationState>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationState
                {
                    RecentProjects = ["project1.sds", "project2.sds"]
                });

            var service = CreateService(serializer, fileSystem);

            await service.LoadAsync(TestContext.Current.CancellationToken);

            service.CurrentSettings.DefaultProjectFolder.ShouldBe(@"C:\Projects");
            service.CurrentSettings.Theme.ShouldBe(StudioTheme.Dark);
            service.CurrentState.RecentProjects.ShouldBe(["project1.sds", "project2.sds"]);
        }

        [Fact]
        public async Task Should_Fallback_To_Defaults_When_Settings_File_Cannot_Be_Deserialized()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();

            fileSystem.FileExists(SettingsFilePath).Returns(true);
            fileSystem.OpenRead(SettingsFilePath).Returns(new MemoryStream(Encoding.UTF8.GetBytes("{}")));

            serializer
                .DeserializeAsync<ApplicationSettings>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromException<ApplicationSettings?>(new JsonException("Corrupt JSON")));

            var service = CreateService(serializer, fileSystem);

            await service.LoadAsync(TestContext.Current.CancellationToken);

            // Falls back to defaults instead of throwing.
            service.CurrentSettings.DefaultProjectFolder.ShouldBe(string.Empty);
            service.CurrentSettings.LogRetentionDays.ShouldBe(ApplicationSettings.DefaultLogRetentionDays);
        }

        [Fact]
        public async Task Should_Fallback_To_Defaults_When_State_File_Cannot_Be_Deserialized()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();

            fileSystem.FileExists(SettingsFilePath).Returns(false);
            fileSystem.FileExists(StateFilePath).Returns(true);
            fileSystem.OpenRead(StateFilePath).Returns(new MemoryStream(Encoding.UTF8.GetBytes("{}")));

            serializer
                .DeserializeAsync<ApplicationState>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromException<ApplicationState?>(new JsonException("Corrupt JSON")));

            var service = CreateService(serializer, fileSystem);

            await service.LoadAsync(TestContext.Current.CancellationToken);

            // Falls back to defaults instead of throwing.
            service.CurrentState.RecentProjects.ShouldBeEmpty();
            service.CurrentState.WindowPlacement.ShouldBeNull();
        }
    }

    public class SaveSettingsAsync : ApplicationSettingsServiceFixture
    {
        [Fact]
        public async Task Should_Create_Directory_Then_Write_Then_Move()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();
            var service = CreateService(serializer, fileSystem);

            var tempPath = SettingsFilePath + ".tmp";
            var writeStream = new MemoryStream();

            fileSystem.OpenWrite(tempPath).Returns(writeStream);

            await service.SaveSettingsAsync(TestContext.Current.CancellationToken);

            // Verify directory was created
            fileSystem.Received(1).CreateDirectory(TestDir);

            // Verify temp file was written
            fileSystem.Received(1).OpenWrite(tempPath);
            await serializer.Received(1).SerializeAsync(writeStream, service.CurrentSettings, Arg.Any<CancellationToken>());

            // Verify temp file was moved to final path
            fileSystem.Received(1).MoveFile(tempPath, SettingsFilePath, overwrite: true);
        }

        [Fact]
        public async Task Should_Write_CurrentSettings_To_Stream()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();
            var service = CreateService(serializer, fileSystem);

            service.CurrentSettings.DefaultProjectFolder = @"D:\Test";
            service.CurrentSettings.LogRetentionDays = 7;

            fileSystem.OpenWrite(Arg.Any<string>()).Returns(new MemoryStream());

            await service.SaveSettingsAsync(TestContext.Current.CancellationToken);

            // Verify the serializer received the CurrentSettings instance with our modified values
            await serializer.Received(1).SerializeAsync(
                Arg.Any<Stream>(),
                Arg.Is<ApplicationSettings>(s => s.DefaultProjectFolder == @"D:\Test" && s.LogRetentionDays == 7),
                Arg.Any<CancellationToken>());
        }
    }

    public class SaveState : ApplicationSettingsServiceFixture
    {
        [Fact]
        public void Should_Create_Directory_Then_Write_Then_Move()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var serializer = Substitute.For<IStudioJsonSerializer>();
            var service = CreateService(serializer, fileSystem);

            serializer.Serialize(Arg.Any<ApplicationState>()).Returns("""{"recentProjects":[]}""");

            service.CurrentState.RecentProjects.Add(@"C:\recent.sds");
            service.SaveState();

            var tempPath = StateFilePath + ".tmp";

            // Verify directory was created
            fileSystem.Received(1).CreateDirectory(TestDir);

            // Verify state was serialized and written to temp
            serializer.Received(1).Serialize(service.CurrentState);
            fileSystem.Received(1).WriteAllText(tempPath, """{"recentProjects":[]}""");

            // Verify temp file was moved to final path
            fileSystem.Received(1).MoveFile(tempPath, StateFilePath, overwrite: true);
        }
    }

    private static ApplicationSettingsService CreateService(IFileSystem fileSystem)
    {
        var serializer = Substitute.For<IStudioJsonSerializer>();
        return new ApplicationSettingsService(serializer, fileSystem, TestDir, Substitute.For<ILogger<ApplicationSettingsService>>());
    }

    private static ApplicationSettingsService CreateService(IStudioJsonSerializer serializer, IFileSystem fileSystem)
    {
        return new ApplicationSettingsService(serializer, fileSystem, TestDir, Substitute.For<ILogger<ApplicationSettingsService>>());
    }
}
