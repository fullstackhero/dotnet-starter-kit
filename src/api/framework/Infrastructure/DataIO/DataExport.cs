using ClosedXML.Excel;
using ClosedXML.Report;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using FSH.Framework.Core.DataIO;

namespace FSH.Framework.Infrastructure.DataIO;

public class DataExport : IDataExport
{
    /// <summary>
    /// Exports List of type <typeparamref name="T"/> as an Excel file to the browser client to be saved.
    /// </summary>
    /// <param name="list">List to be used to create an IXLWorkbook for export as Excel file to browser client.</param>
    /// <returns>byte[] is used to export as an Excel file to browser client with function
    /// 
    /// internal async Task SendFileToBrowserAsync(IJSRuntime jsRuntime, byte[] fileByteArray, string fileName,
    ///      string mimeTypeString, string extension)
    /// {
    ///     // Export as Excel Workbook via JavaScript
    ///     await jsRuntime.InvokeAsync<ExportDataFile>(
    ///         "DownloadFile",
    ///         fileName + extension,
    ///         mimeTypeString,
    ///         Convert.ToBase64String(fileByteArray)
    ///     );
    /// }
    /// </returns>
    
    public byte[] ListToByteArray<T>(List<T> list)
    {
        if (list is null || list.Count is 0)
        {
            throw new ArgumentNullException(nameof(list));
        }

        // Create DataTable from List
        DataTable dataTable = ListToDataTable(list);
        // Create IXLWorkbook from DataTable
        IXLWorkbook workbook = DataTableToIXLWorkbook(typeof(T).Name, dataTable);
        
        // Convert IXLWorkbook to ByteArray
        byte[] fileByteArray;

        using (MemoryStream memoryStream = new())
        {
            workbook.SaveAs(memoryStream);
            fileByteArray = memoryStream.ToArray();
        }

        return fileByteArray ;
    }
    
    /// <summary>
    /// Creates a DataTable from a List of type <typeparamref name="T"/>; using the properties of <typeparamref name="T"/> to create the DataTable Columns and the items from List of type <typeparamref name="T"/> to create the DataTables Rows.
    /// </summary>
    /// <typeparam name="T">DataType used to create the DataTable; DataType properities are used to create the DataTable Columns.</typeparam>
    /// <param name="list">List of items to create the rows of the DataTable.</param>
    /// <returns>Returns a DataTable created from the List of type <typeparamref name="T"/></returns>
    private static DataTable ListToDataTable<T>(List<T> list)
    {
        if (list is null || list.Count is 0)
        {
            throw new ArgumentNullException(nameof(list));
        }

        DataTable dataTable = new DataTable(typeof(T).Name);

        // Create data table columns from data model properties
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            dataTable.Columns.Add(property.Name);
        }

        // Create data table rows from list items
        foreach (T item in list)
        {
            object[] values = new object[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                //inserting property values to datatable rows
                values[i] = properties[i].GetValue(item, null);
            }

            dataTable.Rows.Add(values);
        }

        return dataTable;
    }
    
    /// <summary>
    /// Create IXLWorkbook from a DataTable
    /// </summary>
    /// <param name="workbookName">Name of IXLWorkbook to be created.</param>
    /// <param name="dataTable">DataTable to be used to create of IXLWorkbook.</param>
    /// <returns>Returns an IXLWorkbook created from a DataTable.</returns>
    private static IXLWorkbook DataTableToIXLWorkbook(string workbookName, DataTable dataTable)
    {
        if (string.IsNullOrWhiteSpace(workbookName))
        {
            throw new ArgumentNullException(nameof(workbookName));
        }

        if (dataTable is null || dataTable.Rows.Count is 0)
        {
            throw new ArgumentNullException(nameof(dataTable));
        }

        XLWorkbook workbook = new();
        workbook.Worksheets.Add(dataTable, workbookName);
        return workbook;
    }

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
