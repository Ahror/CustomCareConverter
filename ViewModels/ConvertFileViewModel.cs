using CustomCareConverter.Views;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.Windows.Input;

namespace CustomCareConverter.ViewModels
{
    public class ConvertFileViewModel : ReactiveObject
    {
        public ICommand ShowExportTreatment { get; private set; }
        public ICommand ShowImportTreatment { get; private set; }

        public ConvertFileViewModel()
        {
            ShowExportTreatment = ReactiveCommand.Create(ShowExportView,outputScheduler: Scheduler.CurrentThread);

            ShowImportTreatment = ReactiveCommand.Create(ShowImportView, outputScheduler: Scheduler.CurrentThread);
        }

        private void ShowExportView()
        {
            ExportView view = new ExportView();
            view.ShowDialog();
        }

        private void ShowImportView()
        {
            ImportView view = new ImportView();
            view.ShowDialog();
        }
    }
}