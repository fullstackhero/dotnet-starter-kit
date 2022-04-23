using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class UpdateEntityWithNavRequest
{
    public UpdateEntityWithNavRequest(string filesavelocation, string _request, string _propertylines, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventsusing, string validatortype, string validatorname)
    {
        string basicsources = pathtobasicsources + "UpdateEntityWithNavRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string createentityrequest = string.Empty;

        createentityrequest = File.ReadAllText(basicsources)
        .Replace("<&EventsPath&>", eventsusing)
        .Replace("<&PropertyLines&>", _propertylines)
        .Replace("<&StringNameSpace&>", thenamespace)
        .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower)
        .Replace("<&Request&>", _request);

        File.WriteAllText(filesavelocation + "/" + "Update" + entity + "Request.cs", createentityrequest);
    }
}