namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class DeleteEntityWithNavRequest
{
    public DeleteEntityWithNavRequest(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventpath)
    {
        string basicsources = pathtobasicsources + "DeleteEntityWithNavRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();

        string deleteentitywithnavsource = File.ReadAllText(basicsources)
            .Replace("<&EventsUsing&>", eventpath)
            .Replace("<&StringNameSpace&>", thenamespace)
            .Replace("<&Entity&>", entity)
            .Replace("<&EntityToLower&>", entitytolower);

        File.WriteAllText(filesavelocation + "/" + "Delete" + entity + "Request.cs", deleteentitywithnavsource);

    }
}
