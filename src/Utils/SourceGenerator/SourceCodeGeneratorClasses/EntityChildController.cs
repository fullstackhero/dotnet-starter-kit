namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class EntityChildController
{
    public EntityChildController(string pathtobasicsources, string controllersnamespace, string pathtocontrollers, string entity, string entitynameplural, string stringusing)
    {

        string basicsources = pathtobasicsources + "EntityChildController.txt";
        string entitychildcontroller = File.ReadAllText(basicsources)
        .Replace("<&StringUsing&>", stringusing)
       .Replace("<&StringNameSpace&>", controllersnamespace)
       .Replace("<&EntityPlural&>", entitynameplural)
       .Replace("<&Entity&>", entity)
       .Replace("<&EntityToLower&>", entity.ToLower());

        File.WriteAllText(pathtocontrollers + entitynameplural + "Controller.cs", entitychildcontroller);
    }
}
