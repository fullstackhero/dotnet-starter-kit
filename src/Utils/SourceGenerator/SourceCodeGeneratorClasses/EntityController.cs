namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class EntityController
{
    public EntityController(string pathtobasicsources, string controllersnamespace, string pathtocontrollers, string entity, string entitynameplural, string stringusing)
    {

        string basicsources = pathtobasicsources + "EntityController.txt";
        string entityController = File.ReadAllText(basicsources)
        .Replace("<&StringUsing&>", stringusing)
       .Replace("<&StringNameSpace&>", controllersnamespace)
       .Replace("<&EntityPlural&>", entitynameplural)
       .Replace("<&Entity&>", entity)
       .Replace("<&EntityToLower&>", entity.ToLower());

        File.WriteAllText(pathtocontrollers + entitynameplural + "Controller.cs", entityController);
    }
}
