using CustomCareConverter.Views;
using Prism.Commands;
using ReactiveUI;
using System.Windows.Input;

namespace CustomCareConverter.ViewModels
{
    public class ConvertFileViewModel : ReactiveObject
    {
        public ICommand ShowExportTreatment { get; private set; }
        public ICommand ShowImportTreatment { get; private set; }
        public ICommand ExportTreatment { get; }
        public ICommand ImportTreatment { get; }

        public ConvertFileViewModel()
        {
            ShowExportTreatment = new DelegateCommand(ShowExportView);

            ShowImportTreatment = new DelegateCommand(ShowImportView);

            ExportTreatment = ReactiveCommand.Create(() =>
            {

            });

            ImportTreatment = ReactiveCommand.Create(() =>
            {

            });
        }

        private void ShowExportView()
        {
            ExportView view = new ExportView() { DataContext = this };
            view.ShowDialog();
        }

        private void ShowImportView()
        {
            ImportView view = new ImportView() { DataContext = this };
            view.ShowDialog();
        }
    }
}
