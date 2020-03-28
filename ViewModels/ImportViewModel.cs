using CsvHelper;
using CustomCareConverter.Data;
using dBASE.NET;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows.Forms;
using System.Windows.Input;

namespace CustomCareConverter.ViewModels
{
    public class ImportViewModel : ReactiveObject
    {
        string _resultText;
        public string ResultText
        {
            get => _resultText;
            set => this.RaiseAndSetIfChanged(ref _resultText, value);
        }
        public ImportViewModel()
        {
            LoadModes = ReactiveCommand.Create(LoadFiles, outputScheduler: Scheduler.CurrentThread);
            Export = ReactiveCommand.Create(ImportDataToDBF);
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

        private void ImportDataToDBF()
        {
            var selectedPrograms = new List<RowInfo>();
            var dir = Directory.GetCurrentDirectory();
            var dbf = new Dbf();
            dbf.Read(Path.Combine(dir, "DBF/bank_mode.DBF"));
            int maxBankId = (int)dbf.Records.Max(x => x.Data[0]);
            foreach (var mode in Modes)
            {
                if (mode.IsSelected == false)
                {
                    continue;
                }
                selectedPrograms.AddRange(mode.ProgramsRowInfo);
                var record = dbf.CreateRecord();
                int index = 0;
                if (dbf.Records.FirstOrDefault(x => (int)x.Data[0] == int.Parse(mode.ModeRowInfo.CellItems[0].Value)) != null)
                {
                    maxBankId++;
                    mode.ModeRowInfo.CellItems[0].Value = maxBankId.ToString();
                }
                foreach (var rowItem in mode.ModeRowInfo.CellItems)
                {
                    if (index == 4)
                        record.Data[index] = rowItem.Value.ToLower() == "true";
                    else if (index == 0)
                        record.Data[index] = int.Parse(rowItem.Value);
                    else if (index == 5)
                        record.Data[index] = DateTime.Parse(rowItem.Value);
                    else if (index == 7)
                        record.Data[index] = DateTime.Parse(rowItem.Value);
                    else
                        record.Data[index] = rowItem.Value;
                    index++;
                }
            }
            var programDbf = new Dbf();
            ImportBankProgram(selectedPrograms, dir, programDbf);
            Save(dir, dbf, programDbf);
            ResultText = "Import finished!";
        }

        private static void ImportBankProgram(List<RowInfo> selectedPrograms, string dir, Dbf programDbf)
        {
            programDbf.Read(Path.Combine(dir, "DBF/bank_program.DBF"));
            foreach (var program in selectedPrograms)
            {
                var record = programDbf.CreateRecord();
                int index = 0;
                foreach (var item in program.CellItems)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        if (index == 0)
                            record.Data[index] = int.Parse(item.Value);
                        else if (index == 1)
                            record.Data[index] = int.Parse(item.Value);
                        else if (index == 3)
                            record.Data[index] = double.Parse(item.Value);
                        else if (index == 5)
                            record.Data[index] = double.Parse(item.Value);
                        else if (index == 6)
                            record.Data[index] = double.Parse(item.Value);
                        else if (index == 7)
                            record.Data[index] = int.Parse(item.Value);
                        else if (index == 8)
                            record.Data[index] = int.Parse(item.Value);
                        else if (index == 11)
                            record.Data[index] = DateTime.Parse(item.Value);
                        else if (index == 13)
                            record.Data[index] = DateTime.Parse(item.Value);
                        else if (index == 15)
                            record.Data[index] = double.Parse(item.Value);
                        else
                            record.Data[index] = item.Value;
                    }

                    index++;
                }
            }
        }

        private static void Save(string dir, Dbf bank_mode, Dbf bank_program)
        {
            var newdbf = new Dbf();
            newdbf.Fields.AddRange(bank_mode.Fields);
            newdbf.Records.AddRange(bank_mode.Records);
            newdbf.Write(Path.Combine(dir, "DBF/bank_mode.DBF"));

            var newBankProgram = new Dbf();
            newBankProgram.Fields.AddRange(bank_program.Fields);
            newBankProgram.Records.AddRange(bank_program.Records);
            newBankProgram.Write(Path.Combine(dir, "DBF/bank_program.DBF"));
        }

        public ObservableCollection<Mode> Modes { get; set; }

        private void LoadFiles()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "|*.zip";
            var dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                var zipFile = openFileDialog.FileName;
                var dir = Directory.GetCurrentDirectory();
                DeleteExistedFile();
                ZipFile.ExtractToDirectory(zipFile, "CSV");
                LoadModeFromFile(dir);
                LoadProgramFromFile(dir);
            }
        }

        private static void DeleteExistedFile()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo("CSV");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private void LoadProgramFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "CSV/bank_program.csv");
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    bool isColumnCreated = false;
                    var records = csv.GetRecords<dynamic>();
                    int rowIndex = 0;
                    var columns = new List<ColumnInfo>();
                    foreach (var record in records)
                    {
                        var rowInfo = new RowInfo { Index = rowIndex };
                        int order = 0;
                        foreach (var cell in record)
                        {
                            ColumnInfo column = default;
                            if (isColumnCreated)
                            {
                                column = columns.FirstOrDefault(c => c.Name == cell.Key);
                                if (column.DataType == null || column.DataType != DataType.Text)
                                {
                                    column.DataType = GetDataTypeFromRowValue(cell.Value);
                                }
                            }
                            else
                            {
                                column = new ColumnInfo(cell.Key);
                                GetColumnInfo(column, cell.Value);
                                columns.Add(column);
                            }

                            var rowValue = cell.Value?.ToString() ?? "";
                            if (column.Name == "BANKT_ID")
                            {
                                var mode = Modes.FirstOrDefault(m => m.Id == int.Parse(rowValue));
                                mode.ProgramsRowInfo.Add(rowInfo);
                            }

                            order++;
                            rowInfo.CellItems.Add(new CellItem { ColumnInfo = column, Value = rowValue, Order = order });
                        }
                        isColumnCreated = true;
                        rowIndex++;
                    }
                    SetColumnDataTypes(columns);
                }
            }
        }
        protected ColumnInfo GetColumnInfo(ColumnInfo columnInfo, string rowValue)
        {
            if (columnInfo.DataType == null)
            {
                columnInfo.DataType = GetDataTypeFromRowValue(rowValue);
            }
            return columnInfo;
        }

        protected DataType? GetDataTypeFromRowValue(string rowValue)
        {
            DataType? result = null;
            if (!string.IsNullOrEmpty(rowValue))
            {
                if (DateTime.TryParse(rowValue, out var _))
                {
                    result = DataType.DateTime;
                }
                else if (decimal.TryParse(rowValue, out _) && (rowValue.Contains(".") || rowValue.Contains(",")))
                {
                    result = DataType.Decimal;
                }
                else if (int.TryParse(rowValue, out _))
                {
                    result = DataType.Int;
                }
                else if (bool.TryParse(rowValue, out _))
                {
                    result = DataType.Boolean;
                }
                else
                {
                    result = DataType.Text;
                }
            }
            return result;
        }
        private void LoadModeFromFile(string dir)
        {
            var filePath = Path.Combine(dir, "CSV/bank_mode.csv");
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    bool isColumnCreated = false;
                    var records = csv.GetRecords<dynamic>();
                    int rowIndex = 0;
                    var columns = new List<ColumnInfo>();
                    foreach (var record in records)
                    {
                        var mode = new Mode();
                        var rowInfo = new RowInfo { Index = rowIndex };
                        int order = 0;
                        foreach (var cell in record)
                        {
                            ColumnInfo column = default;
                            if (isColumnCreated)
                            {
                                column = columns.FirstOrDefault(c => c.Name == cell.Key);
                                if (column.DataType == null || column.DataType != DataType.Text)
                                {
                                    column.DataType = GetDataTypeFromRowValue(cell.Value);
                                }
                            }
                            else
                            {
                                column = new ColumnInfo(cell.Key);
                                GetColumnInfo(column, cell.Value);
                                columns.Add(column);
                            }
                            if (column.Name == "MODENAME")
                            {
                                mode.Name = cell.Value.ToString();
                            }
                            else if (column.Name == "BANKT_ID")
                            {
                                mode.Id = int.Parse(cell.Value);
                            }

                            order++;
                            var rowValue = cell.Value?.ToString() ?? "";
                            rowInfo.CellItems.Add(new CellItem { ColumnInfo = column, Value = rowValue, Order = order });
                        }
                        mode.ModeRowInfo = rowInfo;
                        Modes.Add(mode);
                        isColumnCreated = true;
                        rowIndex++;
                    }
                    SetColumnDataTypes(columns);
                }
            }
        }

        void SetColumnDataTypes(IEnumerable<ColumnInfo> columns)
        {
            foreach (var column in columns)
            {
                if (column.DataType == null)
                {
                    column.DataType = DataType.Text;
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

