namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateParentEntityRequest
{
    public CreateParentEntityRequest(string filesavelocation, string _request, string _propertylines, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventpath, string validatortype, string validatorname )
    {
        string basicsources = pathtobasicsources + "CreateParentEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string createentityrequest = string.Empty;

        createentityrequest = File.ReadAllText(basicsources)
        .Replace("<&EventsPath&>", eventpath)
        .Replace("<&PropertyLines&>", _propertylines)
        .Replace("<&StringNameSpace&>", thenamespace)
        .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower)
        .Replace("<&ValidatorName&>", validatorname)
        .Replace("<&ValidatorNameToLower&>", validatorname.ToLower())
        .Replace("<&Request&>", _request);

        File.WriteAllText(filesavelocation + "/" + "Create" + entity + "Request.cs", createentityrequest);
    }
}