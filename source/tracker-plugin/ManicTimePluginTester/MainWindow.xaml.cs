using System.Windows;
using Finkit.ManicTime.Common;

namespace ManicTimePluginsTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = new MainWindowViewModel();
            _mainWindowViewModel.Dispatcher = Dispatcher;
            DataContext = _mainWindowViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel.OnLoaded();
        }
    }
}
