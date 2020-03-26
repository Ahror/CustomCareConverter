using System;
using System.Collections.ObjectModel;

namespace CustomCareConverter.Data
{
    public class RowInfo
    {
        public int Index { get; set; }
        public Guid Id { get; set; }
        public RowInfo()
        {
            Id = Guid.NewGuid();
            CellItems = new ObservableCollection<CellItem>();
        }
        public ObservableCollection<CellItem> CellItems { get; }
    }
}
