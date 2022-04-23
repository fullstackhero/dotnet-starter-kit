using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateDetailDto
{
    public CreateDetailDto(string filesavelocation, string propertylines, string detaillines, string relationallines, string theusings, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, bool hasnavigations)
    {
        string basicsources = pathtobasicsources + "BasicDto.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string dtoDetailsText = File.ReadAllText(basicsources);

        dtoDetailsText = dtoDetailsText.Replace("<&theusings&>", theusings);
        dtoDetailsText = dtoDetailsText.Replace("<&StringNameSpace&>", thenamespace);
        dtoDetailsText = dtoDetailsText.Replace("<&Entity&>", entity + "DetailsDto");
        dtoDetailsText = dtoDetailsText.Replace("<&PropertyLines&>", propertylines);
        dtoDetailsText = dtoDetailsText.Replace("<&RelationalLines&>", string.Empty);
        dtoDetailsText = dtoDetailsText.Replace("<&DetailLines&>", detaillines);
        dtoDetailsText = Regex.Replace(dtoDetailsText, @"(^\p{Zs}*\r\n){2,}", "\r\n", RegexOptions.Multiline);
        File.WriteAllText(filesavelocation + "/" + entity + "DetailsDto.cs", dtoDetailsText);

    }
}

