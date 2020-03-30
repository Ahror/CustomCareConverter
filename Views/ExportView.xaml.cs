using CustomCareConverter.ViewModels;
using MahApps.Metro.Controls;
using ReactiveUI;
using System;
using System.Windows;

namespace CustomCareConverter.Views
{
    /// <summary>
    /// Interaction logic for ExportView.xaml
    /// </summary>
    public partial class ExportView : MetroWindow
    {
        ExportViewModel _viewModel;
        bool firstTime;
        public ExportView()
        {
            InitializeComponent();
            _viewModel = new ExportViewModel();
            DataContext = _viewModel;
            this.WhenAnyValue(vm => vm._viewModel.CloseButtonClicked).Subscribe((old) =>
            {
                if (firstTime) Close();
                firstTime = true;
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadModes.Execute(null);
        }
    }
}