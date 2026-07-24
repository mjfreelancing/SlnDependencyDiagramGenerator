using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.RecentProjects;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.RecentProjects;

public class RecentProjectsServiceFixture
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IApplicationSettingsService _settingsService = Substitute.For<IApplicationSettingsService>();
    private readonly ApplicationState _state = new();
    private readonly RecentProjectsService _service;

    public RecentProjectsServiceFixture()
    {
        _settingsService.CurrentState.Returns(_state);
        _service = new RecentProjectsService(_fileSystem, _settingsService);
    }

    public class Add : RecentProjectsServiceFixture
    {
        [Fact]
        public void Should_Insert_At_Front()
        {
            _state.RecentProjects.AddRange(["old1.sds", "old2.sds"]);

            _service.Add("new.sds");

            _state.RecentProjects[0].ShouldBe("new.sds");
            _state.RecentProjects.Count.ShouldBe(3);
        }

        [Fact]
        public void Should_Deduplicate_Existing_Entry()
        {
            _state.RecentProjects.AddRange(["existing.sds", "other.sds"]);

            _service.Add("existing.sds");

            _state.RecentProjects[0].ShouldBe("existing.sds");
            _state.RecentProjects.Count.ShouldBe(2);
        }

        [Fact]
        public void Should_Trim_To_MaxEntries()
        {
            for (var i = 0; i < 12; i++)
            {
                _state.RecentProjects.Add($"file{i}.sds");
            }

            _service.Add("new.sds");

            _state.RecentProjects.Count.ShouldBe(10);
            _state.RecentProjects[0].ShouldBe("new.sds");
        }

        [Fact]
        public void Should_Persist_After_Add()
        {
            _service.Add("file.sds");

            _settingsService.Received(1).SaveState();
        }
    }

    public class Remove : RecentProjectsServiceFixture
    {
        [Fact]
        public void Should_Remove_Existing_Entry()
        {
            _state.RecentProjects.AddRange(["file1.sds", "file2.sds", "file3.sds"]);

            _service.Remove("file2.sds");

            _state.RecentProjects.ShouldNotContain("file2.sds");
            _state.RecentProjects.Count.ShouldBe(2);
        }

        [Fact]
        public void Should_Not_Throw_When_Entry_Not_Found()
        {
            _state.RecentProjects.Add("file.sds");

            Should.NotThrow(() => _service.Remove("nonexistent.sds"));
        }

        [Fact]
        public void Should_Persist_After_Remove()
        {
            _service.Remove("file.sds");

            _settingsService.Received(1).SaveState();
        }
    }

    public class GetRecent : RecentProjectsServiceFixture
    {
        [Fact]
        public void Should_Return_Entries_For_Existing_Files()
        {
            _state.RecentProjects.AddRange([@"C:\Projects\proj1.sds", @"C:\Projects\proj2.sds"]);
            _fileSystem.FileExists(@"C:\Projects\proj1.sds").Returns(true);
            _fileSystem.FileExists(@"C:\Projects\proj2.sds").Returns(true);

            var result = _service.GetRecent();

            result.Length.ShouldBe(2);
            result[0].FilePath.ShouldBe(@"C:\Projects\proj1.sds");
            result[0].DisplayName.ShouldBe("proj1");
            result[0].Exists.ShouldBeTrue();
        }

        [Fact]
        public void Should_Mark_Missing_Files_As_Not_Existing()
        {
            _state.RecentProjects.Add(@"C:\Projects\missing.sds");
            _fileSystem.FileExists(@"C:\Projects\missing.sds").Returns(false);

            var result = _service.GetRecent();

            result[0].Exists.ShouldBeFalse();
        }

        [Fact]
        public void Should_Mix_Existing_And_Missing_Files()
        {
            _state.RecentProjects.AddRange([@"C:\Projects\exists.sds", @"C:\Projects\missing.sds"]);
            _fileSystem.FileExists(@"C:\Projects\exists.sds").Returns(true);
            _fileSystem.FileExists(@"C:\Projects\missing.sds").Returns(false);

            var result = _service.GetRecent();

            result[0].Exists.ShouldBeTrue();
            result[1].Exists.ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_Empty_When_No_Entries()
        {
            var result = _service.GetRecent();

            result.ShouldBeEmpty();
        }
    }
}
