namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class SearchParentEntityRequest
{
    public SearchParentEntityRequest(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, Dictionary<string, string> pks)
    {
        string thenamespace = stringnamespace + entitynameplural;
        string searchparententityrequest = string.Empty;
        string entitytolower = entity.ToLower();
        string parentguids = string.Empty;
        string parententity = string.Empty;
        string parententityplural = string.Empty;
        foreach (var key in pks)
        {
            parententity = key.Key;
            parententityplural = key.Value;

            parentguids = parentguids + "public Guid?" + parententity + "Id { get; set; }" + Environment.NewLine;
        }

        string entitybyid = pathtobasicsources + "SearchParentEntityRequest.txt";
        string parentspec = File.ReadAllText(entitybyid)
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&EntityPlural&>", entitynameplural)
       .Replace("<&Entity&>", entity)
       .Replace("<&EntityToLower&>", entitytolower)
       .Replace("<&ParentGuids&>", parentguids)
       .Replace("<&ParentEntity&>", parententity)
       .Replace("<&ParentEntityPlural&>", parententityplural);

        File.WriteAllText(filesavelocation + "/" + "Search" + entitynameplural + "Request.cs", parentspec);

    }
}