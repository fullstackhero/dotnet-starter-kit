using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class UpdateEntityRequestValidator
{
    public UpdateEntityRequestValidator(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventpath, string validatortype, string validatorname, Dictionary<string, string> pks, string theusings)
    {
        string basicsources = pathtobasicsources + "UpdateEntityRequestValidator.txt";
        string navrules = pathtobasicsources + "RuleForNavigation.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string updateentityrequestvalidator = string.Empty;
        string readrepository = string.Empty;
        string rulefornavigation = string.Empty;
        string rulesfornavigations = string.Empty;
        string request = string.Empty;
        string relationalLines = string.Empty;
        string propertyLines = string.Empty;
        string inputLine = string.Empty;
        string getSet = string.Empty;
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

        updateentityrequestvalidator = File.ReadAllText(basicsources)
        .Replace("<&Usings&>", theusings)
        .Replace("<&EventsPath&>", eventpath)        
        .Replace("<&StringNameSpace&>", thenamespace)
        .Replace("<&Entity&>", entity)
        .Replace("<&EntityToLower&>", entitytolower)
        .Replace("<&ReadRepository&>", readrepository)
        .Replace("<&RulesForRelations&>", rulesfornavigations)
        .Replace("<&ValidatorName&>", validatorname)
        .Replace("<&ValidatorNameToLower&>", validatorname.ToLower())
        .Replace("<&ParentRule&>", parentrule);
        File.WriteAllText(filesavelocation + "/" + "Update" + entity + "RequestValidator.cs", updateentityrequestvalidator);

        //createentityrequestvalidator = File.ReadAllText(basicsources)
        //.Replace("<&StringNameSpace&>", thenamespace)
        //.Replace("<&Entity&>", entity)
        //.Replace("<&EntityToLower&>", entitytolower)
        //.Replace("<&ReadRepository&>", readrepository)
        //.Replace("<&RulesForRelations&>", rulesfornavigations);
        //File.WriteAllText(filesavelocation + "/" + "Update" + entity + "RequestValidator.cs", createentityrequestvalidator);
    }
}