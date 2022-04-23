namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class GetSingleResultEntityRequest
{
    public GetSingleResultEntityRequest(string pathtodata, string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural)
    {
        string basicsources = pathtobasicsources + "GetSingleResultEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string getentityrequest = string.Empty;
        string entitytolower = entity.ToLower();
        getentityrequest = File.ReadAllText(basicsources)
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower);
        File.WriteAllText(filesavelocation + "/" + "Get" + entity + "Request.cs", getentityrequest);

    }
}