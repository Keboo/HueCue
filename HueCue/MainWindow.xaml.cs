using System.Windows.Input;

namespace HueCue;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
    }

    private void OnClose(object sender, ExecutedRoutedEventArgs e)
    {
        // Dispose of the ViewModel to clean up video resources
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Dispose();
        }
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Ensure cleanup on window close
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Dispose();
        }
        base.OnClosed(e);
    }
}
