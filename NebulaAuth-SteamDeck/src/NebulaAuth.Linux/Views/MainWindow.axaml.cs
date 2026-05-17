using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NebulaAuth.Linux.Services;
using NebulaAuth.Linux.ViewModels;

namespace NebulaAuth.Linux.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Enable file drop for mafile import
        AddHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(this, true);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files == null) return;

        if (DataContext is MainWindowViewModel vm)
        {
            foreach (var file in files)
            {
                var path = file.Path.LocalPath;
                if (path.EndsWith(".maFile", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".mafile", StringComparison.OrdinalIgnoreCase))
                {
                    // Import through service
                    // vm.ImportFile(path);
                }
            }
        }
    }
}
