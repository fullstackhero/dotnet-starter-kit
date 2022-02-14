using System.ComponentModel;
using System.Data;
using ClosedXML.Excel;
using FSH.WebApi.Application.Common.Exporters;

namespace FSH.WebApi.Infrastructure.Common.Export;

public class ExcelWriter : IExcelWriter
{
    public Stream WriteToStream<T>(IList<T> data)
    {
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
        DataTable table = new DataTable("table", "table");
        foreach (PropertyDescriptor prop in properties)
            table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        foreach (T item in data)
        {
            DataRow row = table.NewRow();
            foreach (PropertyDescriptor prop in properties)
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            table.Rows.Add(row);
        }

        using XLWorkbook wb = new XLWorkbook();
        wb.Worksheets.Add(table);
        Stream stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
