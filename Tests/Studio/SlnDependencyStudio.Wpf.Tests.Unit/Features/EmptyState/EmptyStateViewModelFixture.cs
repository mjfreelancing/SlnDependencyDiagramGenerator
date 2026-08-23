using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.EmptyState;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.EmptyState;

[Collection(nameof(ReactiveUIInitializer))]
public class EmptyStateViewModelFixture
{
    private readonly IRecentProjectsStore _recentProjects = Substitute.For<IRecentProjectsStore>();

    public class Construction : EmptyStateViewModelFixture
    {
        [Fact]
        public void Should_Populate_RecentProjects_From_Service()
        {
            var recentEntries = new ObservableCollection<RecentProjectEntry>
            {
                new RecentProjectEntry("b.sds", "b", true),
                new RecentProjectEntry("a.sds", "a", true)
            };

            _recentProjects.RecentProjects.Returns(recentEntries);
            _recentProjects.HasRecentProjects.Returns(true);

            var vm = new EmptyStateViewModel(_recentProjects);

            vm.RecentProjects.Count.ShouldBe(2);
            vm.RecentProjects[0].FilePath.ShouldBe("b.sds");
            vm.RecentProjects[1].FilePath.ShouldBe("a.sds");
            vm.HasRecentProjects.ShouldBeTrue();
        }

        [Fact]
        public void Should_Set_HasRecentProjects_False_When_No_Entries()
        {
            _recentProjects.RecentProjects.Returns([]);
            _recentProjects.HasRecentProjects.Returns(false);

            var vm = new EmptyStateViewModel(_recentProjects);

            vm.RecentProjects.Count.ShouldBe(0);
            vm.HasRecentProjects.ShouldBeFalse();
        }
    }

    public class NewProjectCommand : EmptyStateViewModelFixture
    {
        [Fact]
        public async Task Should_Fire_NewProjectRequested_Interaction()
        {
            var vm = new EmptyStateViewModel(Substitute.For<IRecentProjectsStore>());

            System.Reactive.Unit? captured = null;

            vm.NewProjectRequested.RegisterHandler(ctx =>
            {
                captured = ctx.Input;
                ctx.SetOutput(System.Reactive.Unit.Default);
            });

            await vm.NewProjectCommand.Execute();

            captured.ShouldNotBeNull();
        }
    }

    public class OpenProjectCommand : EmptyStateViewModelFixture
    {
        [Fact]
        public async Task Should_Fire_OpenProjectRequested_Interaction()
        {
            var vm = new EmptyStateViewModel(Substitute.For<IRecentProjectsStore>());

            System.Reactive.Unit? captured = null;

            vm.OpenProjectRequested.RegisterHandler(ctx =>
            {
                captured = ctx.Input;
                ctx.SetOutput(System.Reactive.Unit.Default);
            });

            await vm.OpenProjectCommand.Execute();

            captured.ShouldNotBeNull();
        }
    }

    public class OpenRecentProjectCommand : EmptyStateViewModelFixture
    {
        [Fact]
        public async Task Should_Fire_OpenRecentProjectRequested_Interaction_With_FilePath()
        {
            var vm = new EmptyStateViewModel(Substitute.For<IRecentProjectsStore>());

            string? captured = null;

            vm.OpenRecentProjectRequested.RegisterHandler(ctx =>
            {
                captured = ctx.Input;
                ctx.SetOutput(System.Reactive.Unit.Default);
            });

            await vm.OpenRecentProjectCommand.Execute("test.sds");

            captured.ShouldBe("test.sds");
        }
    }
}
