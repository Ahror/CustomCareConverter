using ReactiveUI;

namespace CustomCareConverter.Data
{
    public class CellItem : ReactiveUI.ReactiveObject
    {
        public int Order { get; set; }
        public ColumnInfo ColumnInfo { get; set; }
        string _value;
        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
