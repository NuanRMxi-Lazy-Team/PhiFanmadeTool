using Avalonia;
using Avalonia.Controls;

namespace PhiFanmade.Tool.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        App.StorageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
    }

    // read only
}