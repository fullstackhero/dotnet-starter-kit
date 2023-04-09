using ClosedXML.Excel;
using FSH.WebApi.Application.Common.DataIO;
using System.ComponentModel;
using System.Data;

namespace FSH.WebApi.Infrastructure.Common.DataIO;

public class ExcelWriter : IExcelWriter
{
    public Stream WriteToStream<T>(IList<T> data)
    {
        var properties = TypeDescriptor.GetProperties(typeof(T));
        var table = new DataTable("table", "table");
        foreach (PropertyDescriptor prop in properties)
            table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        foreach (var item in data)
        {
            var row = table.NewRow();
            foreach (PropertyDescriptor prop in properties)
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            table.Rows.Add(row);
        }

        using var wb = new XLWorkbook();
        wb.Worksheets.Add(table);
        Stream stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
