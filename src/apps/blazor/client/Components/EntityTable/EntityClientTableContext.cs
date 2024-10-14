using System.Net;
using FSH.Starter.Blazor.Infrastructure.Api;

namespace FSH.Starter.Blazor.Client.Components.EntityTable;

/// <summary>
/// Initialization Context for the EntityTable Component.
/// Use this one if you want to use Client Paging, Sorting and Filtering.
/// </summary>
public class EntityClientTableContext<TEntity, TId, TRequest>
    : EntityTableContext<TEntity, TId, TRequest>
{
    /// <summary>
    /// A function that loads all the data for the table from the api and returns a ListResult of TEntity.
    /// </summary>
    public Func<Task<List<TEntity>?>> LoadDataFunc { get; }

    /// <summary>
    /// A function that returns a boolean which indicates whether the supplied entity meets the search criteria
    /// (the supplied string is the search string entered).
    /// </summary>
    public Func<string?, TEntity, bool> SearchFunc { get; }
    
    /// <summary>
    /// A function that exports the specified data from the API.
    /// </summary>
    public Func<BaseFilter, Task<byte[]>>? ExportFunc { get; }

    /// <summary>
    /// A function that import the specified data from the API.
    /// </summary>
    public Func<FileUploadCommand, bool, Task<ImportResponse>>? ImportFunc { get; }

    public EntityClientTableContext(
        List<EntityField<TEntity>> fields,
        Func<Task<List<TEntity>?>> loadDataFunc,
        Func<string?, TEntity, bool> searchFunc,
        Func<TEntity, TId>? idFunc = null,
        Func<Task<TRequest>>? getDefaultsFunc = null,
        Func<TRequest, Task>? createFunc = null,
        Func<TId, Task<TRequest>>? getDetailsFunc = null,
        Func<TId, TRequest, Task>? updateFunc = null,
        Func<TId, Task>? deleteFunc = null,
        Func<BaseFilter, Task<byte[]>>? exportFunc = null,
        Func<FileUploadCommand, bool, Task<ImportResponse>>? importFunc = null,
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
        LoadDataFunc = loadDataFunc;
        SearchFunc = searchFunc;
        ExportFunc = exportFunc;
        ImportFunc = importFunc;
    }
}
