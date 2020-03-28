using CustomCareConverter.ViewModels;
using MahApps.Metro.Controls;

namespace CustomCareConverter.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : MetroWindow
    {
        internal ImportViewModel ViewModel { get; }
        public ImportView()
        {
            InitializeComponent();
            ViewModel = new ImportViewModel();
            DataContext = ViewModel;
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.LoadModes.Execute(null);
            if (!ViewModel.IsZipFileSelected)
            {
                this.Close();
            }
        }
    }
}
