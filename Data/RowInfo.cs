using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
