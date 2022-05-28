using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EnumFastToStringGenerated;

namespace FSH.WebApi.Domain.Common;

[EnumGenerator]
public enum FileType
{
    [Display(Name = ".jpg,.png,.jpeg")]
    Image
}