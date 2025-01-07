using System.Windows;
using ControlBee;

namespace WpfSandbox;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Label1.Content = "foo";
    }
}
