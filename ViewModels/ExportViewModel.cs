﻿using CsvHelper;
using CustomCareConverter.Data;
using dBASE.NET;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows.Forms;
using System.Windows.Input;

namespace CustomCareConverter.ViewModels
{
    public class ExportViewModel : ReactiveObject
    {
        string _resultText;
        public string ResultText
        {
            get => _resultText;
            set => this.RaiseAndSetIfChanged(ref _resultText, value);
        }

        bool _closeButtonClicked;
        public bool CloseButtonClicked
        {
            get => _closeButtonClicked;
            set => this.RaiseAndSetIfChanged(ref _closeButtonClicked, value);
        }
        public ExportViewModel()
        {
            LoadModes = ReactiveCommand.Create(LoadFiles, outputScheduler: Scheduler.CurrentThread);
            Export = ReactiveCommand.Create(ExportDataToCSV);
            Cancel = ReactiveCommand.Create(CloseWindow);
            Modes = new ObservableCollection<Mode>();
            this.WhenAnyValue(vm => vm.SelectAll).Subscribe((old) =>
            {
                SelectAllModes(old);
            });

        }

        void SelectAllModes(bool selectAll)
        {
            foreach (var mode in Modes)
            {
                mode.IsSelected = selectAll;
            }
        }

        private void CloseWindow()
        {
            CloseButtonClicked = !CloseButtonClicked;
        }

        private void ExportDataToCSV()
        {
            if (!Modes.Any())
            {
                ResultText = "There are not any selected item";
                return;
            }
            var selectedPrograms = new List<RowInfo>();
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    foreach (var mode in Modes)
                    {
                        if (mode.IsSelected == false)
                        {
                            continue;
                        }

                        selectedPrograms.AddRange(mode.ProgramsRowInfo);
                        var wrapper = new ExpandoObject() as IDictionary<string, object>;
                        foreach (var rowItem in mode.ModeRowInfo.CellItems)
                        {
                            wrapper.Add(rowItem.ColumnInfo.Name, rowItem.Value);
                        }
                        csv.WriteRecord(wrapper);
                        csv.NextRecord();
                    }
                    writer.Flush();
                    stream.Position = 0;
                    if (!Directory.Exists("CSV"))
                        Directory.CreateDirectory("CSV");
                    using (var fileStream = File.Create("CSV/bank_mode.csv"))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(fileStream);
                    }
                }
            }

            ExportBankPrograms(selectedPrograms);
            ZippingFile();
            ResultText = "Export finished!";
        }

        private static void ExportBankPrograms(List<RowInfo> selectedPrograms)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter programWriter = new StreamWriter(stream))
                {
                    using (CsvWriter csv = new CsvWriter(programWriter, CultureInfo.InvariantCulture))
                    {
                        foreach (var programRowInfo in selectedPrograms)
                        {
                            var wrapper = new ExpandoObject() as IDictionary<string, object>;
                            foreach (var rowItem in programRowInfo.CellItems)
                            {
                                wrapper.Add(rowItem.ColumnInfo.Name, rowItem.Value);
                            }
                            csv.WriteRecord(wrapper);
                            csv.NextRecord();
                        }
                        programWriter.Flush();
                        stream.Position = 0;

                        using (var fileStream = File.Create("CSV/bank_program.csv"))
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }

        private static void ZippingFile()
        {
            var folderBrowserDialog1 = new SaveFileDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string zipPath = folderBrowserDialog1.FileName;
                if (!zipPath.Contains("zip"))
                    zipPath = zipPath + ".zip";
                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); }
                    catch { }
                }
                ZipFile.CreateFromDirectory("CSV", zipPath);
            }
        }

        public ObservableCollection<Mode> Modes { get; set; }
        public ObservableCollection<ModeItem> ModeList { get; set; }


        private void LoadFiles()
        {
            var dir = Directory.GetCurrentDirectory();
            LoadModeListFromFile(dir);
            LoadModeFromFile(dir);
            LoadProgramFromFile(dir);
        }

        void LoadProgramFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "bank_program.DBF");
            if (File.Exists(filePath))
            {
                var dbf = new Dbf();
                dbf.Read(filePath);
                var columns = new List<ColumnInfo>();
                foreach (DbfField field in dbf.Fields)
                {
                    var columnInfo = new ColumnInfo(field.Name);
                    columnInfo.DataType = GetDataType(field.Type);
                    columns.Add(columnInfo);
                }

                foreach (DbfRecord record in dbf.Records)
                {
                    var rowInfo = new RowInfo();
                    for (int i = 0; i < dbf.Fields.Count; i++)
                    {
                        var rowValue = record[i]?.ToString();
                        ColumnInfo columnInfo = columns[i];

                        if (columnInfo.Name == "BANKT_ID")
                        {
                            var mode = Modes.FirstOrDefault(m => m.Id == int.Parse(rowValue));
                            if (mode != null)
                                mode.ProgramsRowInfo.Add(rowInfo);
                        }
                        rowInfo.CellItems.Add(new CellItem { ColumnInfo = columnInfo, Value = rowValue, Order = i });
                    }
                }
            }
        }

        void LoadModeFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "bank_mode.DBF");
            if (File.Exists(filePath))
            {
                var dbf = new Dbf();
                dbf.Read(filePath);
                var columns = new List<ColumnInfo>();
                foreach (DbfField field in dbf.Fields)
                {
                    var columnInfo = new ColumnInfo(field.Name);
                    columnInfo.DataType = GetDataType(field.Type);
                    columns.Add(columnInfo);
                }
                List<DbfRecord> records = new List<DbfRecord>();
                if (UserType.IsAdmin)
                {
                    records = dbf.Records;
                }
                else
                {
                    foreach (var item in dbf.Records)
                    {
                        if (ModeList.All(m => m.Id != int.Parse(item.Data[1].ToString())))
                            records.Add(item);
                    }
                }
                foreach (DbfRecord record in records)
                {
                    var mode = new Mode();
                    var rowInfo = new RowInfo();
                    mode.ModeRowInfo = rowInfo;
                    for (int i = 0; i < dbf.Fields.Count; i++)
                    {
                        var rowValue = record[i]?.ToString();
                        ColumnInfo columnInfo = columns[i];

                        if (columnInfo.Name == "MODENAME")
                        {
                            mode.Name = rowValue.ToString();
                        }
                        else if (columnInfo.Name == "MODE_CODE")
                        {
                            mode.ModeCode = rowValue;
                        }
                        else if (columnInfo.Name == "BANKT_ID")
                        {
                            mode.Id = int.Parse(rowValue);
                        }

                        rowInfo.CellItems.Add(new CellItem { ColumnInfo = columnInfo, Value = rowValue, Order = i });
                    }
                    Modes.Add(mode);
                }
            }
        }

        void LoadModeListFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "ModeList.csv");
            if (File.Exists(filePath))
            {
                ModeList = new ObservableCollection<ModeItem>();
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>();
                    bool isThisFirstTime = false;
                    foreach (var record in records)
                    {
                        var mode = new ModeItem();
                        if (!isThisFirstTime)
                        {
                            var mode1 = new ModeItem();
                            bool first = false;
                            isThisFirstTime = true;
                            foreach (var cell in record)
                            {
                                if (!first)
                                {
                                    mode.Id = int.Parse(cell.Key);
                                    mode1.Id = int.Parse(cell.Value);
                                    first = true;
                                }
                                else
                                {
                                    mode.Name = cell.Key;
                                    mode1.Name = cell.Value;
                                }

                            }
                            ModeList.Add(mode);
                            ModeList.Add(mode1);
                        }

                        else
                        {
                            bool first = false;
                            foreach (var cell in record)
                            {
                                if (!first)
                                {
                                    first = true;
                                    mode.Id = int.Parse(cell.Value);
                                }
                                else
                                {
                                    mode.Name = cell.Value;
                                }
                            }
                            ModeList.Add(mode);
                        }
                    }
                }
            }
        }
        protected ColumnInfo GetColumnInfo(ColumnInfo columnInfo, string rowValue)
        {
            if (columnInfo.DataType == null)
            {
                columnInfo.DataType = DataType.Text;
            }
            return columnInfo;
        }
        bool _selectAll;
        public bool SelectAll
        {
            get => _selectAll;
            set => this.RaiseAndSetIfChanged(ref _selectAll, value);
        }

        DataType? GetDataType(DbfFieldType type)
        {
            DataType dataType = default;
            switch (type)
            {
                case DbfFieldType.Currency:
                case DbfFieldType.Double:
                case DbfFieldType.Float:
                    dataType = DataType.Decimal;
                    break;
                case DbfFieldType.Date:
                case DbfFieldType.DateTime:
                    dataType = DataType.DateTime;
                    break;
                case DbfFieldType.Numeric:
                case DbfFieldType.Integer:
                    dataType = DataType.Int;
                    break;
                case DbfFieldType.Logical:
                    dataType = DataType.Boolean;
                    break;
                case DbfFieldType.Memo:
                case DbfFieldType.General:
                case DbfFieldType.Character:
                case DbfFieldType.Picture:
                case DbfFieldType.NullFlags:
                    dataType = DataType.Text;
                    break;
                default:
                    dataType = DataType.Text;
                    break;
            }
            return dataType;
        }
        public ICommand LoadModes { get; }
        public ICommand Export { get; }
        public ICommand Cancel { get; }
        public string FilePath { get; private set; }
    }
}
