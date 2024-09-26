using ClosedXML.Excel;
using ClosedXML.Report;
using System.ComponentModel;
using System.Data;
using FSH.Framework.Core.DataIO;

namespace FSH.Framework.Infrastructure.DataIO;


public class ExcelWriter : IExcelWriter
{
    public Stream WriteToStream<T>(IList<T> data)
    {
        var properties = TypeDescriptor.GetProperties(typeof(T));
        var table = new DataTable("Sheet1", "table"); // "Sheet1" = typeof(T).Name
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

    public Stream WriteToTemplate<T>(T data, string templateFile)
    {
        var template = new XLTemplate(templateFile);
        template.AddVariable(data);
        template.Generate();

        // save to file on API server
        const string outputFile = @".\Output\AssetDeliveryFrom.xlsx";
        template.SaveAs(outputFile);

        // or get bytes to return excel file from web api
        Stream stream = new MemoryStream();
        template.Workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
