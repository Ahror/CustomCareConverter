using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomCareConverter.Data
{
    public class ColumnInfo
    {
        public ColumnInfo(string name)
        {
            Name = name;
            DataType = null;
        }

        public string Name { get; set; }
        public DataType? DataType { get; set; }

        public override string ToString()
        {
            return $"Name={Name}; DataType={DataType}";
        }
    }

    public enum DataType
    {
        Text,
        Int,
        Decimal,
        DateTime,
        Boolean,
    }
}
