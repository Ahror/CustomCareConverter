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
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows.Input;

namespace CustomCareConverter.ViewModels
{
    public class ImportViewModel : ReactiveObject
    {
        public ImportViewModel()
        {
            LoadModes = ReactiveCommand.Create(LoadFiles, outputScheduler: Scheduler.CurrentThread);
            this.WhenAnyValue(vm => vm.SelectAll).Subscribe((old) =>
            {
                if (old == false)
                    return;

                SelectAllModes();
            });
            Export = ReactiveCommand.Create(ExportDataToDBF);
            Modes = new ObservableCollection<Mode>();
        }

        void SelectAllModes()
        {
            foreach (var mode in Modes)
            {
                mode.IsSelected = true;
            }
        }

        private void ExportDataToDBF()
        {
            var selectedPrograms = new List<RowInfo>();

            var dbf = new Dbf();
            foreach (var mode in Modes)
            {
                if (mode.IsSelected == false)
                {
                    continue;
                }

                selectedPrograms.AddRange(mode.ProgramsRowInfo);
                foreach (var rowItem in mode.ModeRowInfo.CellItems)
                {
                    //dbf.Fields.Add(new DbfField(rowItem.ColumnInfo.Name,DbfFieldType.))
                }
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

