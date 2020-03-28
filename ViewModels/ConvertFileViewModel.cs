﻿using CustomCareConverter.Views;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Input;

namespace CustomCareConverter.ViewModels
{
    public class ConvertFileViewModel : ReactiveObject
    {
        public ICommand ShowExportTreatment { get; private set; }
        public ICommand ShowImportTreatment { get; private set; }
        public ICommand Retry { get; set; }
        public ICommand Cancel { get; set; }

        public ConvertFileViewModel()
        {
            ShowExportTreatment = ReactiveCommand.Create(ShowExportView, outputScheduler: Scheduler.CurrentThread);
            ShowImportTreatment = ReactiveCommand.Create(ShowImportView, outputScheduler: Scheduler.CurrentThread);
            Cancel = ReactiveCommand.Create(CloseApp, outputScheduler: Scheduler.CurrentThread);
            ValidateFiles();
            Retry = ReactiveCommand.Create(ValidateFiles);
            this.WhenAnyValue(vm => vm.IsFileLocked).Subscribe((old) =>
            {
                ShowRetryWindow = !old;
            });
        }

        void CloseApp()
        {
            Application.Current.Shutdown();
        }

        void ValidateFiles()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var modeFileInfo = new FileInfo(Path.Combine(currentDir, "DBF/bank_mode.DBF"));
            var programFileInfo = new FileInfo(Path.Combine(currentDir, "DBF/bank_mode.DBF"));
            IsFileLocked = !CheckIsFileLocked(modeFileInfo) || !CheckIsFileLocked(programFileInfo);
        }

        bool _showRetryWindow;
        public bool ShowRetryWindow
        {
            get => _showRetryWindow;
            set => this.RaiseAndSetIfChanged(ref _showRetryWindow, value);
        }


        bool _isFileLocked;
        public bool IsFileLocked
        {
            get => _isFileLocked;
            set => this.RaiseAndSetIfChanged(ref _isFileLocked, value);
        }


        protected virtual bool CheckIsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                    stream.Close();
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        void ShowExportView()
        {
            ExportView view = new ExportView();
            view.ShowDialog();
        }

        void ShowImportView()
        {
            ImportView view = new ImportView();
            view.ShowDialog();
        }
    }
}