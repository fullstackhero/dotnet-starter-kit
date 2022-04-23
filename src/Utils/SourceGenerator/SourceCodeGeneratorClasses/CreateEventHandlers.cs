namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CreateEventHandlers
{
    public CreateEventHandlers(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural)
    {
        string basicsources = pathtobasicsources + "EntityEventHandler.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string eventHandlerCreate = File.ReadAllText(basicsources)
        .Replace("<&StringNameSpace&>", thenamespace)
        .Replace("<&Entity&>", entity)
        .Replace("<&Action&>", "Created");
        File.WriteAllText(filesavelocation + "/EventHandlers" + "/" + entity + "CreatedEventHandler.cs", eventHandlerCreate);

        string eventHandlerDelete = File.ReadAllText(basicsources)
        .Replace("<&StringNameSpace&>", thenamespace)
        .Replace("<&Entity&>", entity)
        .Replace("<&Action&>", "Deleted");
        File.WriteAllText(filesavelocation + "/EventHandlers" + "/" + entity + "DeletedEventHandler.cs", eventHandlerDelete);

        string eventHandlerUpdate = File.ReadAllText(basicsources)
      .Replace("<&StringNameSpace&>", thenamespace)
      .Replace("<&Entity&>", entity)
      .Replace("<&Action&>", "Updated");
        File.WriteAllText(filesavelocation + "/EventHandlers" + "/" + entity + "UpdateEventHandler.cs", eventHandlerUpdate);
    }
}
