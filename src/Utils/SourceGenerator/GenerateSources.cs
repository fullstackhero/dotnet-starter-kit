using Microsoft.Extensions.Logging;
using FSH.WebApi.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FSH.WebApi.Utils.SourceGenerator;

public class GenerateSources : IGenerateSources
{
    private readonly GenerateSourcesSettings _settings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GenerateSources> _logger;
    private string? entityName { get; set; } = default!;
    private string? entityNamePlural { get; set; } = default!;
    private string? entityFullName { get; set; } = default!;
    private string _propertyLines = string.Empty;
    private string _relationalLines = string.Empty;
    private string _detailLines = string.Empty;
    private string _theusings = string.Empty;
    private string _request = string.Empty;
    private string _usingpathtochildren = string.Empty;
    private string _readrepositoryLines = string.Empty;
    private string _publicrepositoryLine = string.Empty;
    private string _repo_Repo = string.Empty;
    private string _repoRepo = string.Empty;
    private string _declaringEntity = string.Empty;
    private string _principalEntity = string.Empty;
    private string srcpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\"));
    private string _validatorType = string.Empty;
    private string _validatorName = string.Empty;

    private bool hasnavigations { get; set; } = false!;
    public GenerateSources(IOptions<GenerateSourcesSettings> settings, ILogger<GenerateSources> logger, ApplicationDbContext db, IHostEnvironment hostEnvironment)
    {
        _settings = settings.Value;
        _logger = logger;
        _db = db;
        _hostEnvironment = hostEnvironment;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var entitiesPlural = new Dictionary<string, string>();
        var entitiesSingle = new Dictionary<string, string>();
        var fks = new Dictionary<string, string>();
        var pks = new Dictionary<string, string>();
        string txtFiles = Path.Combine(srcpath, _settings.PathToTxtFiles);
        string pathToData = Path.Combine(srcpath, _settings.PathToData);
        string pathToApplicationCatalog = Path.Combine(srcpath, _settings.PathToApplicationCatalog);
        string pathToControllers = Path.Combine(srcpath, _settings.PathToControllers);
        string pathToPermissionsFile = Path.Combine(srcpath, _settings.PathToPermissions);
        // run only if RunSourceSettings in generatesourcesettings = true;
        if (_settings.RunSourceSettings)
        {
            _logger.LogInformation("GenerateSources Started");
            _logger.LogInformation("Get Entities from EF Core");

            // get single and plural names for all applicationdbcontext entities
            var getentities = _db.Model.GetEntityTypes();
            getentities?.ToList().ForEach(t =>
            {
                entityFullName = t.ClrType.FullName;
                if (entityFullName != null)
                {
                    // Is the entity one of Domain.Catalog (our own created entities)
                    // to get single and plural names needed for the classes that could need
                    // navigationproperties (ex. deleteentityrequests).
                    if (entityFullName.Contains(_settings.DetermineDomainEntityPath))
                    {
                        entityName = t.DisplayName();
                        entityNamePlural = t.GetTableName();
                        if (entityNamePlural != null)
                        {
                            entitiesPlural.Add(entityName, entityNamePlural);
                            entitiesSingle.Add(entityNamePlural, entityName);
                        }

                    }
                }
            });

            // Loop again through all entitys in applicationdbcontext only our created entities

            var entityTypes = _db.Model.GetEntityTypes();
            entityTypes?.ToList().ForEach(t =>
            {
                // clear these params after each loop
                _usingpathtochildren = string.Empty;
                _readrepositoryLines = string.Empty;
                _publicrepositoryLine = string.Empty;
                _declaringEntity = string.Empty;
                _principalEntity = string.Empty;               
                fks.Clear();
                pks.Clear();
                entityFullName = t.ClrType.FullName;
                entityName = t.DisplayName();
                entityNamePlural = t.GetTableName();
                _repo_Repo = "_" + entityName.ToLower() + "Repo,";
                _repoRepo = entityName.ToLower() + "Repo,";
                string directoryName = Path.Combine(pathToApplicationCatalog, entityNamePlural);
                // _logger.LogInformation("Get db context Entitties");
                if (entityFullName.Contains(_settings.DetermineDomainEntityPath) && entityFullName != null && !Directory.Exists(directoryName) && entityNamePlural != null)
                {
                    _logger.LogInformation("Entity : " + entityName + "=> " + entityNamePlural);
                    string applicationCatalog = pathToApplicationCatalog + entityNamePlural;
                    Directory.CreateDirectory(directoryName);
                    hasnavigations = t.GetNavigations().Count() > 0 ? true : false;

                    var foreignkeys = t.GetReferencingForeignKeys();
                    if (foreignkeys != null)
                    {
                        foreach (var thekey in foreignkeys)
                        {
                            _declaringEntity = thekey.DeclaringEntityType.GetTableName();
                            string valuesingle = entitiesSingle[_declaringEntity];

                            if (valuesingle != null)
                            {
                                bool addnewline = _readrepositoryLines == string.Empty ? true : false;
                                _usingpathtochildren = _usingpathtochildren + "using " + _settings.StringNameSpace + _declaringEntity + ";" + Environment.NewLine;
                                _readrepositoryLines = _readrepositoryLines + (addnewline ? " " : Environment.NewLine) + "private readonly IReadRepository<" + valuesingle + "> _" + valuesingle.ToLower() + "Repo;";
                                _publicrepositoryLine = _publicrepositoryLine + " IReadRepository<" + valuesingle + "> " + valuesingle.ToLower() + "Repo" + ",";
                                _repo_Repo = _repo_Repo + " _" + valuesingle.ToLower() + "Repo,";
                                _repoRepo = _repoRepo + " " + valuesingle.ToLower() + "Repo,";
                                fks.Add(valuesingle, _declaringEntity);
                            }
                        }
                    }

                    var parentkeys = t.GetDeclaredForeignKeys();
                    if (parentkeys != null)
                    {
                        foreach (var key in parentkeys)
                        {
                            _principalEntity = key.PrincipalEntityType.GetTableName();
                            string valuesingle = entitiesSingle[_principalEntity];
                            if (_principalEntity != entityNamePlural)
                            {
                                pks.Add(valuesingle, _principalEntity);
                            }
                        }
                    }

                    // Get the properties of the Entity
                    this.EntityProperties(entityName, entitiesPlural);

                    // if foreignKeys create subfolder EventHandlers
                    // has navigations ( = product style classes)
                    if (hasnavigations)
                    {
                        _logger.LogInformation("Create EventHandler Folder");
                        Directory.CreateDirectory(applicationCatalog + "/EventHandlers");
                        CreateEventHandlers newEventHandlers = new CreateEventHandlers(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural);
                        _logger.LogInformation("Create DTO");
                        CreateDto newCreateDto = new CreateDto(applicationCatalog, _propertyLines, _detailLines, _relationalLines, _theusings, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, hasnavigations);
                        CreateDetailDto newCreateDetailDto = new CreateDetailDto(applicationCatalog, _propertyLines, _detailLines, _relationalLines, _theusings, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, hasnavigations);
                        _logger.LogInformation("Create " + entityName + " Request");
                        CreateChildEntityRequest newChildEntityRequest = new CreateChildEntityRequest(applicationCatalog, _request, _propertyLines, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing);
                        _logger.LogInformation("Create " + entityName + " Request Validator");
                        CreateEntityRequestValidator newCreateEntityRequestValidator = new CreateEntityRequestValidator( applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing, _validatorType, _validatorName, pks, _theusings);
                        _logger.LogInformation("Create Delete " + entityName + " Request");
                        DeleteEntityWithNavRequest newDeleteEntityWithNavRequest = new DeleteEntityWithNavRequest(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing);
                        _logger.LogInformation("Create" + entityName + " By Parent Spec");
                        CreateEntityByParentSpec newEntityByParentSpec = new CreateEntityByParentSpec(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, pks);
                        _logger.LogInformation("Create" + entityName + " By Name spec");
                        CreateEntityByTypeSpec newEntityByNameSpec = new CreateEntityByTypeSpec(pathToData, applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _validatorType, _validatorName);
                        _logger.LogInformation("Create Get" + entityName + " via Dapper request");
                        GetEntityViaDapperRequest newGetEntityViaDapperRequest = new GetEntityViaDapperRequest(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural);
                        _logger.LogInformation("Create Get" + entityName + " Request");
                        GetEntityWithNavRequest newGetEntityWithNavRequest = new GetEntityWithNavRequest(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, pks);
                        _logger.LogInformation("Create Search" + entityName + " Request");
                        SearchParentEntityRequest newSearchParentEntityRequest = new SearchParentEntityRequest(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, pks);
                        EntitiesBySearchRequestWithParentSpec newEntitiesBySearchRequestWithParentSpec = new EntitiesBySearchRequestWithParentSpec(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, pks);
                        _logger.LogInformation("Create Update" + entityName + " Request");
                        UpdateEntityWithNavRequest newUpdateEntityWithNavRequest = new UpdateEntityWithNavRequest(applicationCatalog, _request, _propertyLines, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing, _validatorType, _validatorName);
                        UpdateEntityRequestValidator newUpdateEntityRequestValidator = new UpdateEntityRequestValidator(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing, _validatorType, _validatorName, pks, _theusings);
                        _logger.LogInformation("Create Controller");
                        EntityChildController newEntityController = new EntityChildController(txtFiles, _settings.ControllersNamespace, pathToControllers, entityName, entityNamePlural, _settings.StringNameSpace);
                    }

                    // no navigations ( = brand style classes)
                    else
                    {
                        _logger.LogInformation("Create DTO");
                        CreateDto newCreateDto = new CreateDto(applicationCatalog, _propertyLines, _detailLines, _relationalLines, _theusings, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, hasnavigations);
                        _logger.LogInformation("Create " + entityName + " Request");
                        CreateParentEntityRequest newEntityCreateRequest = new CreateParentEntityRequest(applicationCatalog, _request, _propertyLines, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing, _validatorType, _validatorName);
                        _logger.LogInformation("Create Delete " + entityName + " Request");
                        DeleteEntityRequest newDeleteEntityRequest = new DeleteEntityRequest(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _usingpathtochildren, _readrepositoryLines, _publicrepositoryLine, _repo_Repo, _repoRepo, fks);
                        _logger.LogInformation("Create " + entityName + " By Name spec");
                        CreateEntityByTypeSpec newEntityByNameSpec = new CreateEntityByTypeSpec(pathToData, applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _validatorType, _validatorName);
                        _logger.LogInformation("Create Update Eentity Request");
                        UpdateEntityRequest newEntityUpdateRequest = new UpdateEntityRequest(pathToData, applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural, _settings.EventsUsing, _validatorType, _validatorName);
                        _logger.LogInformation("Create Get" + entityName + " Request");
                        GetSingleResultEntityRequest newGetEntityRequest = new GetSingleResultEntityRequest(pathToData, applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural);
                        _logger.LogInformation("Create Search" + entityName + " Request");
                        SearchEntityRequest newSearchEntityRequest = new SearchEntityRequest(applicationCatalog, txtFiles, _settings.StringNameSpace, entityName, entityNamePlural);
                        _logger.LogInformation("Create Controller");
                        EntityController newEntityController = new EntityController(txtFiles, _settings.ControllersNamespace, pathToControllers, entityName, entityNamePlural, _settings.StringNameSpace);
                    }

                    // Add Entity to permissions if not exists
                    // CheckAndAddPermissions
                    _logger.LogInformation("Check and if not Exist Insert" + entityName + " Permissions");
                    CheckAndAddPermissions newCheckAndAddPermissions = new CheckAndAddPermissions(txtFiles, pathToPermissionsFile, entityNamePlural);
                }
            });
        }
    }

#pragma warning restore CS8602 // Dereference of a possibly null reference.
    private void EntityProperties(string entity, Dictionary<string, string> entities)
    {
        string datapath = Path.Combine(srcpath, _settings.PathToData, entity + ".cs");
        string[] columnLines = File.ReadAllLines(datapath);
        string getSet = string.Empty;
        string entitytocheck = string.Empty;
        _detailLines = string.Empty;
        _propertyLines = string.Empty;
        _relationalLines = string.Empty;
        _theusings = string.Empty;
        _request = string.Empty;
        _validatorName = string.Empty;
        _validatorType = string.Empty;

        foreach (string line in columnLines)
        {
            // inputLine = string.Empty;
            getSet = string.Empty;
            string[] parts = line.Split();
            for (int i = 0; i <= parts.Length; i++)
            {
                if (parts.Length > i + 2)
                {
                    // skip the code in the entity class
                    bool skipme = parts[i + 1].Length >= entity.Length ? parts[i + 1].Substring(0, entity.Length) == entity : false;

                    // check if it's an entity property line
                    if (parts[i] == "public" && parts[i + 1] != "virtual" && parts[i + 1] != "class" && !skipme)
                    {

                        if (_validatorType == String.Empty && _validatorName == String.Empty)
                        {
                            _validatorType = parts[i + 1];
                            _validatorName = parts[i + 2];
                        }
                        string inputLine = parts[i] + " " + parts[i + 1] + " " + parts[i + 2];
                        string requestPart = "request." + parts[i + 2];
                        getSet = string.Equals(parts[i + 1], "string") ? "{ get; set; } = default!;" : "{ get; set; }";
                        _request = _request + requestPart;
                        _propertyLines = _propertyLines + "\t" + inputLine + " " + getSet;

                        if (i < parts.Length)
                        {
                            _request = _request + ", ";
                            _propertyLines = _propertyLines + Environment.NewLine;
                        }
                    }

                    if (parts[i] == "public" && parts[i + 1] == "virtual" && parts[i + 2] == parts[i + 3] && !skipme)
                    {
                        entitytocheck = parts[i + 2];

                        // There must be better ways !! Let's assume the first string in  key column is the field for the DTO.
                        string datarel = Path.Combine(srcpath, _settings.PathToData, entitytocheck + ".cs");
                        string[] txtLines = File.ReadAllLines(datarel);
                        foreach (string txtline in txtLines)
                        {
                            string[] txtparts = txtline.Split();

                            for (int ti = 0; ti < txtparts.Length; ti++)
                            {
                                {
                                    if (txtparts[ti].Length > ti + 1)
                                    {
                                        if (txtparts[ti] == "public" && txtparts[ti + 1] == "string")
                                        {
                                            _relationalLines = _relationalLines + "\t" + "public string " + entitytocheck + txtparts[ti + 2] + " { get; set; } = default!;" + Environment.NewLine;
                                            _detailLines = _detailLines + "\t" + "public " + entitytocheck + "Dto " + entitytocheck + " { get; set; } = default!;" + Environment.NewLine;

                                            // Get the plural entitytocheck out of the array
                                            string entitytocheckplural = entities[entitytocheck];
                                            _theusings = _theusings + "using " + _settings.StringNameSpace + entitytocheckplural + ";" + Environment.NewLine;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // check for , canremove  (not neccessary) but...
        _request = _request.Trim();
        bool canremove = _request.Substring(_request.Length - 1) == ",";
        _request = canremove ? _request.Remove(_request.Length - 1) : _request;
    }
}
