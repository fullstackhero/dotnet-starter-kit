namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateEntityByTypeSpec
{
    public CreateEntityByTypeSpec(string pathtodata, string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string validatortype, string validatorname)
    {
        // string basicsources =  validatortype == "string" ? pathtobasicsources + "EntityByStringSpec.txt" : pathtobasicsources + "EntityByIntSpec.txt";
        string basicsources = pathtobasicsources + "EntityByNameSpec.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitySpecCreate = File.ReadAllText(basicsources)
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&Entity&>", entity)
       .Replace("<&ValidatorType&>", validatortype)
       .Replace("<&ValidatorName&>", validatorname)
       .Replace("<&ValidatorNameToLower&>", validatorname.ToLower());

        File.WriteAllText(filesavelocation + "/" + entity + "By" + validatorname + "Spec.cs", entitySpecCreate);
    }
}
