using ClosedXML.Excel;
using FSH.WebApi.Application.Common.Exporters;
using System.ComponentModel;
using System.Data;

namespace FSH.WebApi.Infrastructure.Export;

public class ExcelWriter : IExcelWriter
{
    public MemoryStream WriteToStream<T>(IList<T> data)
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

        using (XLWorkbook wb = new XLWorkbook())
        {
            wb.Worksheets.Add(table);
            MemoryStream stream = new MemoryStream();

            wb.SaveAs(stream);
            return stream;
        }
    }
}
