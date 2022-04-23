namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateChildEntityRequest
{
    public CreateChildEntityRequest(string filesavelocation, string _request, string _propertylines, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventpath)
    {
        string basicsources = pathtobasicsources + "CreateChildEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string createentityrequest = string.Empty;

        createentityrequest = File.ReadAllText(basicsources)
            .Replace("<&EntityPlural&>", entitynameplural)
            .Replace("<&EventsPath&>", eventpath)
            .Replace("<&PropertyLines&>", _propertylines)
            .Replace("<&StringNameSpace&>", thenamespace)
            .Replace("<&Entity&>", entity)
            .Replace("<&EntityToLower&>", entitytolower)
            .Replace("<&Request&>", _request);

        File.WriteAllText(filesavelocation + "/" + "Create" + entity + "Request.cs", createentityrequest);
    }
}