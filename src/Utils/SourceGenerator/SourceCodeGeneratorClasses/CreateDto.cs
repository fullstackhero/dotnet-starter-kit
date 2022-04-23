using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateDto
{
    public CreateDto(string filesavelocation, string propertylines, string detaillines, string relationallines, string theusings, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, bool hasnavigations)
    {
        string basicsources = pathtobasicsources + "BasicDto.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string dtoText = File.ReadAllText(basicsources);
        string dtoDetailsText = dtoText;
        dtoText = dtoText.Replace("<&theusings&>", string.Empty);
        dtoText = dtoText.Replace("<&StringNameSpace&>", thenamespace);
        dtoText = dtoText.Replace("<&Entity&>", entity + "Dto");
        dtoText = dtoText.Replace("<&PropertyLines&>", propertylines);
        dtoText = dtoText.Replace("<&RelationalLines&>", relationallines);
        dtoText = dtoText.Replace("<&DetailLines&>", string.Empty);
        dtoText = dtoText.Replace("^(?:[\t ]*(?:\r?\n|\r))+", string.Empty);
        dtoText = Regex.Replace(dtoText, @"(^\p{Zs}*\r\n){2,}", "\r\n", RegexOptions.Multiline);
        File.WriteAllText(filesavelocation + "/" + entity + "Dto.cs", dtoText);

        if (hasnavigations)
        {
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
}

