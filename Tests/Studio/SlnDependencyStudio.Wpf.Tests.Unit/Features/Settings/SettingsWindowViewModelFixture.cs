using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Settings;
using SlnDependencyStudio.Wpf.Features.Theming;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Settings;

[Collection(nameof(ReactiveUIInitializer))]
public class SettingsWindowViewModelFixture
{
    private readonly IApplicationSettingsService _settingsService = Substitute.For<IApplicationSettingsService>();
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();
    private readonly ILogger<SettingsWindowViewModel> _logger = Substitute.For<ILogger<SettingsWindowViewModel>>();

    public class SaveCommand : SettingsWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Show_Error_Dialog_When_Save_Fails()
        {
            _settingsService.CurrentSettings.Returns(new ApplicationSettings());

            var errorDialog = ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction);

            var viewModel = new SettingsWindowViewModel(_settingsService, _themeService, errorDialog, _logger);
            viewModel.SettingsEditorViewModel = new SettingsEditorViewModel(
                _settingsService, _themeService, Substitute.For<ILogger<SettingsEditorViewModel>>());

            var exception = new InvalidOperationException("Save failed");

            _settingsService
                .SaveSettingsAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            interaction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            // The failure is routed to the wired ThrownExceptions handler, which shows the error dialog.
            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(viewModel.SaveCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Save Settings failed");
            capturedError.Message.ShouldBe("Save failed");
        }
    }
}
