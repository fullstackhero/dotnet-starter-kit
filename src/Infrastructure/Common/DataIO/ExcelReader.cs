using ClosedXML.Excel;
using FSH.WebApi.Application.Common.DataIO;
using FSH.WebApi.Application.Common.FileStorage;
using FSH.WebApi.Domain.Common;
using System.Data;
using System.Text.RegularExpressions;

namespace FSH.WebApi.Infrastructure.Common.Import;
public class ExcelReader : IExcelReader
{
    public async Task<IList<T>> ToListAsync<T>(FileUploadRequest request, FileType supportedFileType, string sheetName = "Sheet1")
    {
        string base64Data = Regex.Match(request.Data, string.Format("data:{0}/(?<type>.+?),(?<data>.+)", supportedFileType.ToString().ToLower())).Groups["data"].Value;
        var streamData = new MemoryStream(Convert.FromBase64String(base64Data));

        List<T> list = new List<T>();
        Type typeOfObject = typeof(T);

        using (IXLWorkbook workbook = new XLWorkbook(streamData))
        {
            // Read the first Sheet from Excel file.
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);
            if (worksheet != null)
            {
                var properties = typeOfObject.GetProperties();

                // header column texts
                var columns = worksheet.FirstRow().Cells().Select((v, i) => new { v.Value, Index = i + 1 });

                // indexing in closedxml starts with 1 not from 0
                // Skip first row which is used for column header texts
                foreach (IXLRow row in worksheet.RowsUsed().Skip(1))
                {
                    T item = (T)Activator.CreateInstance(typeOfObject);

                    foreach (var prop in properties)
                    {
                        try
                        {
                            var propertyType = prop.PropertyType;
                            var col = columns.SingleOrDefault(c => c.Value.ToString() == prop.Name);
                            if (col == null) continue;

                            object? obj = GetObjectByDataType(propertyType, row.Cell(col.Index).Value);
                            if(obj != null) prop.SetValue(item, obj);
                        }
                        catch
                        {
                            // if any error
                             return await Task.FromResult(new List<T>());
                        }
                    }

                    if (item != null) list.Add(item);
                }
            }
        }

        return await Task.FromResult(list);
    }

    private static object? GetObjectByDataType(Type propertyType, XLCellValue cellValue)
    {
        if (cellValue.ToString() == "null" || cellValue.IsBlank)
        {
            return null;
        }

        object? val;

        if (propertyType.IsEnum)
        {
            val = Convert.ToInt32(cellValue.GetNumber());
            return Enum.ToObject(propertyType, val);
        }
        else if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
        {
            val = Guid.Parse(cellValue.ToString());

        }
        else if (propertyType == typeof(int) || propertyType == typeof(int?))
        {
            val = Convert.ToInt32(cellValue.GetNumber());
        }
        else if (propertyType == typeof(decimal))
        {
            val = Convert.ToDecimal(cellValue.GetNumber());
        }
        else if (propertyType == typeof(long))
        {
            val = Convert.ToInt64(cellValue.GetNumber());
        }
        else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
        {
            val = Convert.ToBoolean(cellValue.GetBoolean());
        }
        else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            val = Convert.ToDateTime(cellValue.GetDateTime());
        }
        else
        {
            val = cellValue.ToString();
        }

         // return val;

        return Convert.ChangeType(val, Nullable.GetUnderlyingType(propertyType) ?? propertyType);
    }
}