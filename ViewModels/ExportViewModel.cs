using ReactiveUI;
using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Input;
using System.Reactive.Concurrency;
using System.Data;
using System.Collections.Generic;
using dBASE.NET;
using CustomCareConverter.Data;
using System.Collections.ObjectModel;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using CsvHelper.Expressions;
using CsvHelper.TypeConversion;
using System.Dynamic;

namespace CustomCareConverter.ViewModels
{
    public class ExportViewModel : ReactiveObject
    {
        public ExportViewModel()
        {
            OpenFile = ReactiveCommand.Create(OpenDBFFile,outputScheduler: Scheduler.CurrentThread);
            Export = ReactiveCommand.Create(ExportDataToCSV);
            Columns = new ObservableCollection<ColumnInfo>();
            Rows = new ObservableCollection<RowInfo>();
        }

        private void ExportDataToCSV()
        {
            //using (var writer = new StringWriter("test.csv",))
            //using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            //{
            //    csv.WriteRecords(Rows);

            //    writer.ToString();
            //}

            using (MemoryStream stream = new MemoryStream())
            {
                using (var reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream))
                using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    foreach (var rowInfo in Rows)
                    {
                        var wrapper = new ExpandoObject() as IDictionary<string, object>;
                        foreach (var rowItem in rowInfo.CellItems)
                        {
                            wrapper.Add(rowItem.ColumnInfo.Name, rowItem.Value);
                        }
                        csv.WriteRecord(wrapper);
                        csv.NextRecord();
                    }
                    stream.Position = 0;
                    var aa = reader.ReadToEnd();
                }
            }
        }

        public ObservableCollection<ColumnInfo> Columns { get; }

        public ObservableCollection<RowInfo> Rows { get; set; }
        private void OpenDBFFile()
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Filter = "Data Base File|*.DBF";
            dialog.ShowDialog();
            FilePath = dialog.FileName;
            if (File.Exists(FilePath))
            {
                var dbf = new Dbf();
                dbf.Read(FilePath);
                foreach (DbfField field in dbf.Fields)
                {
                    var columnInfo = new ColumnInfo(field.Name);
                    columnInfo.DataType = GetDataType(field.Type);
                    Columns.Add(columnInfo);
                }

                foreach (DbfRecord record in dbf.Records)
                {
                    var rowInfo = new RowInfo();
                    Rows.Add(rowInfo);
                    for (int i = 0; i < dbf.Fields.Count; i++)
                    {
                        var rowValue = record[i]?.ToString();
                        ColumnInfo columnInfo = Columns[i];
                        rowInfo.CellItems.Add(new CellItem { ColumnInfo = columnInfo, Value = rowValue, Order = i });
                    }
                }
            }
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

        public ICommand OpenFile { get;  }
        public ICommand Export { get; }
        public string FilePath { get; private set; }
    }
}
