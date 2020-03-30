using CustomCareConverter.ViewModels;
using MahApps.Metro.Controls;
using ReactiveUI;
using System;
using System.Windows;

namespace CustomCareConverter.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : MetroWindow
    {
        ImportViewModel ViewModel { get; }
        bool firstTime;
        public ImportView()
        {
            InitializeComponent();
            ViewModel = new ImportViewModel();
            DataContext = ViewModel;
            this.WhenAnyValue(vm => vm.ViewModel.CloseButtonClicked).Subscribe((old) =>
            {
                if (firstTime) Close();
                firstTime = true;
            });
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
