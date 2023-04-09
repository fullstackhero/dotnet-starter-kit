using System.ComponentModel;

namespace FSH.WebApi.Domain.Common;

public enum FileType
{
    [Description(".jpg,.png,.jpeg")]
    Image,
    [Description(".xls,.xlsx")]
    Excel,
    [Description(".zip")]
    QuizMedia,
    [Description(".pdf,.doc,.zip,.rar")]
    Doccument,
}