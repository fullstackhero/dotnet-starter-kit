namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateEntityRequestValidator
{
    public CreateEntityRequestValidator(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventpath, string validatortype, string validatorname, Dictionary<string, string> pks, string theusings)
    {
        string basicsources = pathtobasicsources + "CreateEntityRequestValidator.txt";
        string navrules = pathtobasicsources + "RuleForNavigation.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string createentityrequestvalidator = string.Empty;
        string readrepository = string.Empty;
        string rulefornavigation = string.Empty;
        string rulesfornavigations = string.Empty;
        string request = string.Empty;
        string relationalLines = string.Empty;
        string inputLine = string.Empty;
        string getSet = string.Empty;
        string validatorType = string.Empty;
        string validatorName = string.Empty;
        string asyncsources = pathtobasicsources + "ValidatorRuleForParent.txt";
        string parentrule = string.Empty;

        foreach (var key in pks)
        {
            string parentEntity = key.Key;
            string parententityPlural = key.Value;
            readrepository = readrepository + "IReadRepository<" + parentEntity + ">" + parentEntity.ToLower() + "Repo,";
            string parent = File.ReadAllText(asyncsources)
            .Replace("<&Parent&>", parentEntity)
            .Replace("<&ParentToLower&>", parentEntity.ToLower());
            parentrule = parentrule + parent + Environment.NewLine;
        }
        createentityrequestvalidator = File.ReadAllText(basicsources)
        .Replace("<&Usings&>", theusings)
        .Replace("<&EventsPath&>", eventpath)
        // .Replace("<&PropertyLines&>", propertyLines)
        .Replace("<&StringNameSpace&>", thenamespace)
        .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower)
        .Replace("<&ReadRepository&>", readrepository)
        .Replace("<&RulesForRelations&>", rulesfornavigations)
        .Replace("<&ValidatorName&>", validatorname)
        .Replace("<&ValidatorNameToLower&>", validatorname.ToLower())
        .Replace("<&ParentRule&>", parentrule);
        File.WriteAllText(filesavelocation + "/" + "Create" + entity + "RequestValidator.cs", createentityrequestvalidator);
    }
}