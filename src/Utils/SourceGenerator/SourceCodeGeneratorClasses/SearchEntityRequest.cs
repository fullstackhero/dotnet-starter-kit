using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class SearchEntityRequest
{
    public SearchEntityRequest(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural)
    {
        string basicsources = pathtobasicsources + "SearchEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string searchentityrequest = string.Empty;
        string entitytolower = entity.ToLower();
        searchentityrequest = File.ReadAllText(basicsources)
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&EntityPlural&>", entitynameplural)
       .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower);

        File.WriteAllText(filesavelocation + "/" + "Search" + entitynameplural + "Request.cs", searchentityrequest);
    }
}
