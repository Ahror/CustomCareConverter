using CustomCareConverter.ViewModels;
using MahApps.Metro.Controls;

namespace CustomCareConverter.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : MetroWindow
    {
        ImportViewModel _viewModel;
        public ImportView()
        {
            InitializeComponent();
            _viewModel = new ImportViewModel();
            DataContext = _viewModel;
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.LoadModes.Execute(null);
        }
    }
}
