using System.Reflection;
using System.Windows.Input;
using Velopack;

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

        // Set window title with version information when installed
        SetWindowTitle();

        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
    }

    private void SetWindowTitle()
    {
        string baseTitle = "HueCue - Video Histogram Viewer";
        
        try
        {
            // Check if the application is installed via Velopack
            UpdateManager updateManager = new(new Velopack.Sources.VelopackFlowSource());
            if (updateManager.IsInstalled)
            {
                // Get version from assembly
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    Title = $"{baseTitle} v{version.Major}.{version.Minor}.{version.Build}";
                    return;
                }
            }
        }
        catch
        {
            // If there's any issue getting version info, fall back to base title
        }
        
        Title = baseTitle;
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
