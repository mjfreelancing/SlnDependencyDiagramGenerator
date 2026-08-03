using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Tests.Unit.DependencyInjection;

public class ScopedOperationFactoryFixture
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly ITestService _testService = Substitute.For<ITestService>();

    public ScopedOperationFactoryFixture()
    {
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.GetService(typeof(ITestService)).Returns(_testService);
    }

    public interface ITestService
    {
        int GetValue();
        Task<int> GetValueAsync(CancellationToken cancellationToken);
    }

    public class Execute : ScopedOperationFactoryFixture
    {
        [Fact]
        public void Should_Resolve_Service_And_Return_Result()
        {
            _testService.GetValue().Returns(42);

            var factory = new ScopedOperationFactory<ITestService>(_scopeFactory);

            var result = factory.Execute(svc => svc.GetValue());

            result.ShouldBe(42);
            _testService.Received(1).GetValue();
        }

        [Fact]
        public void Should_Create_And_Dispose_Scope()
        {
            var factory = new ScopedOperationFactory<ITestService>(_scopeFactory);

            factory.Execute(_ => 0);

            _scopeFactory.Received(1).CreateScope();
            _scope.Received(1).Dispose();
        }
    }

    public class ExecuteAsync : ScopedOperationFactoryFixture
    {
        [Fact]
        public async Task Should_Resolve_Service_And_Return_Result()
        {
            _testService
                .GetValueAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(99));

            var factory = new ScopedOperationFactory<ITestService>(_scopeFactory);

            var result = await factory.ExecuteAsync(
                (svc, ct) => svc.GetValueAsync(ct), CancellationToken.None);

            result.ShouldBe(99);
            await _testService.Received(1).GetValueAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Pass_CancellationToken_To_Operation()
        {
            using var cts = new CancellationTokenSource();

            var factory = new ScopedOperationFactory<ITestService>(_scopeFactory);

            await factory.ExecuteAsync(
                (_, ct) =>
                {
                    ct.ShouldBe(cts.Token);
                    return Task.FromResult(0);
                },
                cts.Token);
        }

        [Fact]
        public async Task Should_Create_And_Dispose_Scope()
        {
            var factory = new ScopedOperationFactory<ITestService>(_scopeFactory);

            await factory.ExecuteAsync((_, _) => Task.FromResult(0), CancellationToken.None);

            _scopeFactory.Received(1).CreateScope();
            _scope.Received(1).Dispose();
        }

        [Fact]
        public async Task Should_Dispose_Scope_When_Operation_Throws()
        {
            _testService
                .GetValueAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<int>(new InvalidOperationException("fail")));

            var factory = new ScopedOperationFactory<ITestService>(_scopeFactory);

            await Should.ThrowAsync<InvalidOperationException>(() =>
                factory.ExecuteAsync((svc, ct) => svc.GetValueAsync(ct), CancellationToken.None));

            _scope.Received(1).Dispose();
        }
    }
}
