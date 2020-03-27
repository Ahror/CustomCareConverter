using CsvHelper;
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
        public ExportViewModel()
        {
            LoadModes = ReactiveCommand.Create(LoadFiles, outputScheduler: Scheduler.CurrentThread);
            this.WhenAnyValue(vm => vm.SelectAll).Subscribe((old) =>
            {
                if (old == false)
                    return;

                SelectAllModes();
            });

            Export = ReactiveCommand.Create(ExportDataToCSV);
            Modes = new ObservableCollection<Mode>();
        }

        void SelectAllModes()
        {
            foreach (var mode in Modes)
            {
                mode.IsSelected = true;
            }
        }

        private void ExportDataToCSV()
        {
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

                    using (var fileStream = File.Create("CSV/bank_mode.csv"))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(fileStream);
                    }
                }
            }

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
            //ZipFile.ExtractToDirectory(zipPath, extractPath);
            var folderBrowserDialog1 = new FolderBrowserDialog();

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath;
                var zipPath = Path.Combine(folderName, "CSV.zip");
                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); }
                    catch { }
                }
                ZipFile.CreateFromDirectory("CSV", zipPath);
            }
        }

        public ObservableCollection<Mode> Modes { get; set; }

        private void LoadFiles()
        {
            var dir = Directory.GetCurrentDirectory();
            LoadModeFromFile(dir);
            LoadProgramFromFile(dir);
        }



        private void LoadProgramFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "DBF/bank_program.DBF");
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
                            mode.ProgramsRowInfo.Add(rowInfo);
                        }
                        rowInfo.CellItems.Add(new CellItem { ColumnInfo = columnInfo, Value = rowValue, Order = i });
                    }
                }
            }
        }

        private void LoadModeFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "DBF/bank_mode.DBF");
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
        public string FilePath { get; private set; }
    }
}
