using ReactiveUI;
using System.Collections.Generic;

namespace CustomCareConverter.Data
{
    public class ModeItem : ReactiveObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Mode : ReactiveObject
    {
        public Mode()
        {
            ProgramsRowInfo = new List<RowInfo>();
        }
        public int Id { get; set; }

        bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        string _name;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        string _modeCode;
        public string ModeCode
        {
            get => _modeCode;
            set => this.RaiseAndSetIfChanged(ref _modeCode, value);
        }

        public RowInfo ModeRowInfo { get; set; }

        public IList<RowInfo> ProgramsRowInfo { get; set; }

    }
}