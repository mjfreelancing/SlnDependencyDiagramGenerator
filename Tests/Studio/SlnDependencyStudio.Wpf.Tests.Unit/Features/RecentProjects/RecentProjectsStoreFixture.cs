using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.RecentProjects;

[Collection(nameof(ReactiveUIInitializer))]
public class RecentProjectsStoreFixture
{
    private readonly IRecentProjectsService _service = Substitute.For<IRecentProjectsService>();
    private readonly RecentProjectsStore _store;

    public RecentProjectsStoreFixture()
    {
        _store = new RecentProjectsStore(_service);
    }

    public class Construction : RecentProjectsStoreFixture
    {
        [Fact]
        public void Should_Refresh_On_Creation()
        {
            _service.Received(1).GetRecent();
        }

        [Fact]
        public void Should_Have_Empty_Collection_When_No_Recent()
        {
            _store.RecentProjects.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Have_HasRecentProjects_False_When_Empty()
        {
            _store.HasRecentProjects.ShouldBeFalse();
        }

        [Fact]
        public void Should_Have_HasRecentProjects_True_When_Entries_Exist()
        {
            _service.GetRecent().Returns([
                new RecentProjectEntry(@"C:\proj.sds", "proj", true)
            ]);

            var store = new RecentProjectsStore(_service);

            store.HasRecentProjects.ShouldBeTrue();
        }
    }

    public class Add : RecentProjectsStoreFixture
    {
        [Fact]
        public void Should_Delegate_To_Service()
        {
            _service.GetRecent().Returns([
                new RecentProjectEntry(@"C:\added.sds", "added", true)
            ]);

            _store.Add(@"C:\added.sds");

            _service.Received(1).Add(@"C:\added.sds");
            _store.RecentProjects.ShouldContain(e => e.FilePath == @"C:\added.sds");
        }

        [Fact]
        public void Should_Refresh_Collection_After_Add()
        {
            _service.GetRecent().Returns([
                new RecentProjectEntry(@"C:\first.sds", "first", true),
                new RecentProjectEntry(@"C:\added.sds", "added", true)
            ]);

            _store.Add(@"C:\added.sds");

            _store.RecentProjects.Count.ShouldBe(2);
        }
    }

    public class Remove : RecentProjectsStoreFixture
    {
        [Fact]
        public void Should_Delegate_To_Service()
        {
            _store.Remove(@"C:\remove.sds");

            _service.Received(1).Remove(@"C:\remove.sds");
        }

        [Fact]
        public void Should_Refresh_Collection_After_Remove()
        {
            _service.GetRecent().Returns([
                new RecentProjectEntry(@"C:\kept.sds", "kept", true)
            ]);

            _store.Remove(@"C:\removed.sds");

            _store.RecentProjects.Count.ShouldBe(1);
            _store.RecentProjects.ShouldNotContain(e => e.FilePath == @"C:\removed.sds");
        }
    }

    public class Refresh : RecentProjectsStoreFixture
    {
        [Fact]
        public void Should_Reload_Collection_From_Service()
        {
            _service.GetRecent().Returns([
                new RecentProjectEntry(@"C:\proj1.sds", "proj1", true),
                new RecentProjectEntry(@"C:\proj2.sds", "proj2", true)
            ]);

            _store.Refresh();

            _store.RecentProjects.Count.ShouldBe(2);
            _store.HasRecentProjects.ShouldBeTrue();
        }

        [Fact]
        public void Should_Clear_Collection_When_Empty()
        {
            _store.Refresh();

            _store.RecentProjects.ShouldBeEmpty();
            _store.HasRecentProjects.ShouldBeFalse();
        }
    }
}
