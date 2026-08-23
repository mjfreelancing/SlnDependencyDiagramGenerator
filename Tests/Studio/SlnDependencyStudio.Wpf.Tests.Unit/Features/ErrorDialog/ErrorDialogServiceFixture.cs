using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.ErrorDialog;

[Collection(nameof(ReactiveUIInitializer))]
public class ErrorDialogServiceFixture
{
    private readonly ErrorDialogService _service = new();

    public class ShowError : ErrorDialogServiceFixture
    {
        [Fact]
        public void Should_Not_Be_Null()
        {
            _service.ShowError.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Support_RegisterHandler()
        {
            var handlerRegistered = false;

            _service.ShowError.RegisterHandler(_ =>
            {
                handlerRegistered = true;
            });

            handlerRegistered.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Invoke_Registered_Handler_When_Handled()
        {
            ErrorInfo? capturedError = null;

            _service.ShowError.RegisterHandler(context =>
            {
                capturedError = context.Input;
                context.SetOutput(System.Reactive.Unit.Default);
            });

            var errorInfo = new ErrorInfo("Test Title", "Test Message");

            await _service.ShowError.Handle(errorInfo);

            capturedError.ShouldNotBeNull();
            capturedError!.Title.ShouldBe("Test Title");
            capturedError.Message.ShouldBe("Test Message");
        }
    }
}
