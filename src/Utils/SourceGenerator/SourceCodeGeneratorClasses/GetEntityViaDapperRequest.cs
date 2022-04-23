namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class GetEntityViaDapperRequest
{
    public GetEntityViaDapperRequest(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural)
    {
        string basicsources = pathtobasicsources + "GetEntityViaDapperRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string getentityviadapperrequest = string.Empty;
        string entitytolower = entity.ToLower();
        getentityviadapperrequest = File.ReadAllText(basicsources)
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower);
        File.WriteAllText(filesavelocation + "/" + "Get" + entity + "ViaDapperRequest.cs", getentityviadapperrequest);

    }
}
