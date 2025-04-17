using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace FSH.Framework.Infrastructure.Common.Extensions;
public static partial class EnumExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        object[] attr = enumValue.GetType().GetField(enumValue.ToString())!
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attr.Length > 0)
            return ((DescriptionAttribute)attr[0]).Description;
        string result = enumValue.ToString();
        result = MyRegex().Replace(result, "$1 $2");
        result = MyRegex1().Replace(result, "$1 $2");
        result = MyRegex2().Replace(result, "$1 $2");
        result = MyRegex3().Replace(result, " $1");
        return result;
    }

    public static ReadOnlyCollection<string> GetDescriptionList(this Enum enumValue)
    {
        string result = enumValue.GetDescription();
        return new ReadOnlyCollection<string>(result.Split(',').ToList());
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex MyRegex();
    [GeneratedRegex("([A-Za-z])([0-9])")]
    private static partial Regex MyRegex1();
    [GeneratedRegex("([0-9])([A-Za-z])")]
    private static partial Regex MyRegex2();
    [GeneratedRegex("(?<!^)(?<! )([A-Z][a-z])")]
    private static partial Regex MyRegex3();
}