namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateEntityByParentSpec
{
    public CreateEntityByParentSpec(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, Dictionary<string, string> pks)
    {
        string basicsources = pathtobasicsources + "EntityByParentSpec.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        foreach (var key in pks)
        {
            string parentEntity = key.Key;
            string parentEntityPlural = key.Value;
            string parent = File.ReadAllText(basicsources)
                .Replace("<&StringNameSpace&>", thenamespace)
                .Replace("<&Entity&>", entity)
                .Replace("<&EntityPlural&>", entitynameplural)
                .Replace("<&Parent&>", parentEntity)
                .Replace("<&ParentToLower&>", parentEntity.ToLower());

            File.WriteAllText(filesavelocation + "/" + entitynameplural + "By" + parentEntity + "Spec.cs", parent);
        }

    }
}
