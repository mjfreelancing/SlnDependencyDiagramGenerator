using Microsoft.Win32;
using ReactiveUI;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Output;

/// <summary>Output panel view that displays streaming log messages at the bottom of the main window.</summary>
public partial class OutputPanelView : ReactiveUserControl<OutputPanelViewModel>
{
    public OutputPanelView(OutputPanelViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind the verbose toggle to the view model.
            this.Bind(ViewModel, vm => vm.IsVerbose, view => view.VerboseCheckBox.IsChecked)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.WrapContent, view => view.WrapCheckBox.IsChecked)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.AutoScroll, view => view.AutoScrollCheckBox.IsChecked)
                .DisposeWith(disposables);

            // Auto-scroll to the bottom when new messages arrive.
            ViewModel!.Messages.CollectionChanged += OnMessagesCollectionChanged;

            Disposable
                .Create(() => ViewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged)
                .DisposeWith(disposables);

            // Register the save-file dialog interaction.
            ViewModel.SaveFileDialog.RegisterHandler(async context =>
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                    DefaultExt = ".txt",
                    FileName = "output.txt"
                };

                context.SetOutput(dialog.ShowDialog() == true ? dialog.FileName : null);
            }).DisposeWith(disposables);
        });
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.Action == NotifyCollectionChangedAction.Add && ViewModel!.AutoScroll)
        {
            MessageScrollViewer.ScrollToEnd();
        }
    }
}
