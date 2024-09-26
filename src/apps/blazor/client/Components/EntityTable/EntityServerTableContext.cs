using FSH.Starter.Blazor.Infrastructure.Api;

namespace FSH.Starter.Blazor.Client.Components.EntityTable;

/// <summary>
/// Initialization Context for the EntityTable Component.
/// Use this one if you want to use Server Paging, Sorting and Filtering.
/// </summary>
public class EntityServerTableContext<TEntity, TId, TRequest>
    : EntityTableContext<TEntity, TId, TRequest>
{
    /// <summary>
    /// A function that loads the specified page from the api with the specified search criteria
    /// and returns a PaginatedResult of TEntity.
    /// </summary>
    public Func<PaginationFilter, Task<PaginationResponse<TEntity>>> SearchFunc { get; }
    
    /// <summary>
    /// A function that exports the specified data from the API.
    /// </summary>
    public Func<BaseFilter, Task<FileStreamHttpResult>>? ExportFunc { get; }

    /// <summary>
    /// A function that import the specified data from the API.
    /// </summary>
    public Func<FileUploadCommand, Task>? ImportFunc { get; }

    public bool EnableAdvancedSearch { get; }

    public EntityServerTableContext(
        List<EntityField<TEntity>> fields,
        Func<PaginationFilter, Task<PaginationResponse<TEntity>>> searchFunc,
        bool enableAdvancedSearch = false,
        Func<TEntity, TId>? idFunc = null,
        Func<Task<TRequest>>? getDefaultsFunc = null,
        Func<TRequest, Task>? createFunc = null,
        Func<TId, Task<TRequest>>? getDetailsFunc = null,
        Func<TId, TRequest, Task>? updateFunc = null,
        Func<TId, Task>? deleteFunc = null,
        Func<BaseFilter, Task<FileStreamHttpResult>>? exportFunc = null,
        Func<FileUploadCommand, Task>? importFunc = null,
        string? entityName = null,
        string? entityNamePlural = null,
        string? entityResource = null,
        string? searchAction = null,
        string? createAction = null,
        string? updateAction = null,
        string? deleteAction = null,
        string? exportAction = null,
        string? importAction = null,
        Func<Task>? editFormInitializedFunc = null,
        Func<Task>? importFormInitializedFunc = null,
        Func<bool>? hasExtraActionsFunc = null,
        Func<TEntity, bool>? canUpdateEntityFunc = null,
        Func<TEntity, bool>? canDeleteEntityFunc = null)
        : base(
            fields,
            idFunc,
            getDefaultsFunc,
            createFunc,
            getDetailsFunc,
            updateFunc,
            deleteFunc,
            entityName,
            entityNamePlural,
            entityResource,
            searchAction,
            createAction,
            updateAction,
            deleteAction,
            exportAction,
            importAction,
            editFormInitializedFunc,
            importFormInitializedFunc,
            hasExtraActionsFunc,
            canUpdateEntityFunc,
            canDeleteEntityFunc)
    {
        SearchFunc = searchFunc;
        ExportFunc = exportFunc;
        ImportFunc = importFunc;
        EnableAdvancedSearch = enableAdvancedSearch;
    }
}
