namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class GetEntityWithNavRequest
{
    public GetEntityWithNavRequest(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, Dictionary<string, string> pks)
    {
        string entityrequesthandle = string.Empty;
        string getentityhandle = string.Empty;
       foreach (var key in pks)
        {
            string parentEntity = key.Key;
            string requesthandlesource = pathtobasicsources + "GetEntityRequestHandle.txt";
            getentityhandle = File.ReadAllText(requesthandlesource)
            .Replace("<&Entity&>", entity)
            .Replace("<&Parent&>", parentEntity);
            entityrequesthandle = entityrequesthandle + getentityhandle + Environment.NewLine;
            // adapt code in class as needed Now jump out foreach
            break;


        }

        string basicsources = pathtobasicsources + "GetEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string getentityrequest = string.Empty;
        string entitytolower = entity.ToLower();
        getentityrequest = File.ReadAllText(basicsources) 
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&Entity&>", entity)
       .Replace("<&EntityToLower&>", entitytolower)
       .Replace("<&GetEntityRequestHandle&>", entityrequesthandle);


        File.WriteAllText(filesavelocation + "/" + "Get" + entity + "Request.cs", getentityrequest);



        // Make source EntityByIdWithParentSpec for the navigation properties
        foreach (var key in pks)
        {
            string parentEntity = key.Key;
            string entitybyid = pathtobasicsources + "EntityByIdWithParentSpec.txt";
            string parent = File.ReadAllText(entitybyid)
                .Replace("<&StringNameSpace&>", thenamespace)
                .Replace("<&Entity&>", entity)
                .Replace("<&EntityPlural&>", entitynameplural)
                .Replace("<&Parent&>", parentEntity)
                .Replace("<&ParentToLower&>", parentEntity.ToLower());

            File.WriteAllText(filesavelocation + "/" + entity + "ByIdWith" + parentEntity + "Spec.cs", parent);
        }

    }
}
