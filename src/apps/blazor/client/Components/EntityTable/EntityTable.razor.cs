using FSH.Starter.Blazor.Client.Components.Common;
using FSH.Starter.Blazor.Client.Components.Dialogs;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Infrastructure.Auth;
using FSH.Starter.Blazor.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Starter.Blazor.Client.Components.EntityTable;

public partial class EntityTable<TEntity, TId, TRequest>
    where TRequest : new()
{
    [Parameter]
    [EditorRequired]
    public EntityTableContext<TEntity, TId, TRequest> Context { get; set; } = default!;

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public string? SearchString { get; set; }
    [Parameter]
    public EventCallback<string> SearchStringChanged { get; set; }

    [Parameter]
    public RenderFragment? AdvancedSearchContent { get; set; }

    [Parameter]
    public RenderFragment<TEntity>? ActionsContent { get; set; }
    [Parameter]
    public RenderFragment<TEntity>? ExtraActions { get; set; }
    [Parameter]
    public RenderFragment<TEntity>? ChildRowContent { get; set; }

    [Parameter]
    public RenderFragment<TRequest>? EditFormContent { get; set; }

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;

    private bool _canSearch;
    private bool _canCreate;
    private bool _canUpdate;
    private bool _canDelete;
    private bool _canExport;
    private bool _canImport;
    
    private bool _buttonStatus;

    private bool _advancedSearchExpanded;

    private MudTable<TEntity> _table = default!;
    private IEnumerable<TEntity>? _entityList;
    private int _totalItems;
    
    private HashSet<TEntity> _selectedItems = [];
    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _canSearch = await CanDoActionAsync(Context.SearchAction, state);
        _canCreate = await CanDoActionAsync(Context.CreateAction, state);
        _canUpdate = await CanDoActionAsync(Context.UpdateAction, state);
        _canDelete = await CanDoActionAsync(Context.DeleteAction, state);
        _canExport = await CanDoActionAsync(Context.ExportAction, state);
        _canImport = await CanDoActionAsync(Context.ImportAction, state);
        
        await LocalLoadDataAsync();
    }

    public Task ReloadDataAsync() =>
        Context.IsClientContext
            ? LocalLoadDataAsync()
            : ServerLoadDataAsync();

    private async Task<bool> CanDoActionAsync(string? action, AuthenticationState state) =>
        !string.IsNullOrWhiteSpace(action) &&
            (bool.TryParse(action, out bool isTrue) && isTrue || // check if action equals "True", then it's allowed
            Context.EntityResource is { } resource && await AuthService.HasPermissionAsync(state.User, action, resource));

    private bool HasActions => _canUpdate || _canDelete || Context.HasExtraActionsFunc is not null && Context.HasExtraActionsFunc();
    private bool CanUpdateEntity(TEntity entity) => _canUpdate && (Context.CanUpdateEntityFunc is null || Context.CanUpdateEntityFunc(entity));
    private bool CanDeleteEntity(TEntity entity) => _canDelete && (Context.CanDeleteEntityFunc is null || Context.CanDeleteEntityFunc(entity));

    // Client side paging/filtering
    private bool LocalSearch(TEntity entity) =>
        Context.ClientContext?.SearchFunc is { } searchFunc
            ? searchFunc(SearchString, entity)
            : string.IsNullOrWhiteSpace(SearchString);

    private async Task LocalLoadDataAsync()
    {
        if (Loading || Context.ClientContext is null)
        {
            return;
        }

        Loading = true;

        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => Context.ClientContext.LoadDataFunc(), Toast, Navigation)
            is List<TEntity> result)
        {
            _entityList = result;
        }

        Loading = false;
    }

    // Server Side paging/filtering

    private async Task OnSearchStringChanged(string? text = null)
    {
        await SearchStringChanged.InvokeAsync(SearchString);

        await ServerLoadDataAsync();
    }

    private async Task ServerLoadDataAsync()
    {
        if (Context.IsServerContext)
        {
            await _table.ReloadServerData();
        }
    }

    private static bool GetBooleanValue(object valueFunc)
    {
        if (valueFunc is bool boolValue)
        {
            return boolValue;
        }
        return false;
    }

    private Func<TableState, CancellationToken, Task<TableData<TEntity>>>? ServerReloadFunc =>
        Context.IsServerContext ? ServerReload : null;

    private async Task<TableData<TEntity>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        if (!Loading && Context.ServerContext is not null)
        {
            Loading = true;

            var filter = GetPaginationFilter(state);

            if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => Context.ServerContext.SearchFunc(filter), Toast, Navigation)
                is { } result)
            {
                _totalItems = result.TotalCount;
                _entityList = result.Items;
            }

            Loading = false;
        }

        return new TableData<TEntity> { TotalItems = _totalItems, Items = _entityList };
    }

    private PaginationFilter GetPaginationFilter(TableState state)
    {
        string[]? orderings = null;
        if (!string.IsNullOrEmpty(state.SortLabel))
        {
            orderings = state.SortDirection == SortDirection.None
                ? new[] { $"{state.SortLabel}" }
                : new[] { $"{state.SortLabel} {state.SortDirection}" };
        }

        var filter = new PaginationFilter
        {
            PageSize = state.PageSize,
            PageNumber = state.Page + 1,
            Keyword = SearchString,
            OrderBy = orderings ?? Array.Empty<string>()
        };

        if (!Context.AllColumnsChecked)
        {
            filter.AdvancedSearch = new()
            {
                Fields = Context.SearchFields,
                Keyword = filter.Keyword
            };
            filter.Keyword = null;
        }

        return filter;
    }
    
    private BaseFilter GetBaseFilter()
    {
        var filter = new BaseFilter
        {
            Keyword = SearchString,
        };

        if (!Context.AllColumnsChecked)
        {
            filter.AdvancedSearch = new()
            {
                Fields = Context.SearchFields,
                Keyword = filter.Keyword
            };
            filter.Keyword = null;
        }

        return filter;
    }
    
    private async Task InvokeModal(TEntity? entity = default)
    {
        bool isCreate = entity is null;

        var parameters = new DialogParameters()
        {
            { nameof(AddEditModal<TRequest>.ChildContent), EditFormContent },
            { nameof(AddEditModal<TRequest>.OnInitializedFunc), Context.EditFormInitializedFunc },
            { nameof(AddEditModal<TRequest>.IsCreate), isCreate }
        };

        Func<TRequest, Task> saveFunc;
        TRequest requestModel;
        string title, successMessage;

        if (isCreate)
        {
            _ = Context.CreateFunc ?? throw new InvalidOperationException("CreateFunc can't be null!");

            saveFunc = Context.CreateFunc;

            requestModel =
                Context.GetDefaultsFunc is not null
                    && await ApiHelper.ExecuteCallGuardedAsync(
                            () => Context.GetDefaultsFunc(), Toast, Navigation)
                        is { } defaultsResult
                ? defaultsResult
                : new TRequest();

            title = $"Create {Context.EntityName}";
            successMessage = $"{Context.EntityName} Created";
        }
        else
        {
            _ = Context.IdFunc ?? throw new InvalidOperationException("IdFunc can't be null!");
            _ = Context.UpdateFunc ?? throw new InvalidOperationException("UpdateFunc can't be null!");

            var id = Context.IdFunc(entity!);

            saveFunc = request => Context.UpdateFunc(id, request);

            requestModel =
                Context.GetDetailsFunc is not null
                    && await ApiHelper.ExecuteCallGuardedAsync(
                            () => Context.GetDetailsFunc(id!),
                            Toast, Navigation)
                        is { } detailsResult
                ? detailsResult
                : entity!.Adapt<TRequest>();

            title = $"Edit {Context.EntityName}";
            successMessage = $"{Context.EntityName}Updated";
        }

        parameters.Add(nameof(AddEditModal<TRequest>.SaveFunc), saveFunc);
        parameters.Add(nameof(AddEditModal<TRequest>.RequestModel), requestModel);
        parameters.Add(nameof(AddEditModal<TRequest>.Title), title);
        parameters.Add(nameof(AddEditModal<TRequest>.SuccessMessage), successMessage);

        var dialog = DialogService.ShowModal<AddEditModal<TRequest>>(parameters);

        Context.SetAddEditModalRef(dialog);

        var result = await dialog.Result;

        if (!result!.Canceled)
        {
            await ReloadDataAsync();
        }
    }

    private async Task Delete(TEntity entity)
    {
        _ = Context.IdFunc ?? throw new InvalidOperationException("IdFunc can't be null!");
        TId id = Context.IdFunc(entity);

        string deleteContent = "You're sure you want to delete {0} with id '{1}'?";
        var parameters = new DialogParameters
        {
            { nameof(DeleteConfirmation.ContentText), string.Format(deleteContent, Context.EntityName, id) }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = false };
        var dialog = DialogService.Show<DeleteConfirmation>("Delete", parameters, options);
        var result = await dialog.Result;
        if (!result!.Canceled)
        {
            _ = Context.DeleteFunc ?? throw new InvalidOperationException("DeleteFunc can't be null!");

            await ApiHelper.ExecuteCallGuardedAsync(
                () => Context.DeleteFunc(id),
                Toast);

            await ReloadDataAsync();
        }
    }
    
    private async Task ExportAsync()
    {
        if (Loading) return;
        Loading = true;
        _buttonStatus = true;

        var filter = GetBaseFilter();

        // if (Context.ServerContext is not null && Context.ServerContext.ExportFunc is not null)
        if (Context.ServerContext?.ExportFunc is not null)
        {
            if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => Context.ServerContext.ExportFunc(filter), Toast,Navigation)
                is { } result)
            {
                await Js.InvokeAsync<object>(
                    "DownloadFile",
                    $"{Context.EntityNamePlural}{'_'}{DateTime.Now:yyyyMMdd_HH-mm-ss}.xlsx",
                    AppConstants.ExcelMineType,
                    Convert.ToBase64String(result)
                );
            }
            
        }
        // (Context.ClientContext is not null && Context.ClientContext.ExportFunc is not null)
        else if (Context.ClientContext?.ExportFunc is not null && await ApiHelper.ExecuteCallGuardedAsync(
                         () => Context.ClientContext.ExportFunc(filter), Toast,Navigation)
                     is { } result)
        {
            await Js.InvokeAsync<object>(
                "DownloadFile",
                $"{Context.EntityNamePlural}{'_'}{DateTime.Now:yyyyMMdd_HH-mm-ss}.xlsx",
                AppConstants.ExcelMineType,
                Convert.ToBase64String(result)
            );
        }
        
        Loading = false;
        _buttonStatus = false;
    }
    
    private async Task ImportAsync(FileUploadCommand request)
    {
        if (Context.ServerContext == null || Context.ServerContext.ImportFunc == null) return;
        Loading = true;

        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => Context.ServerContext.ImportFunc(request), Toast)
            is { } result)
        { }

        Loading = false;
    }
    
    private async Task InvokeImportModal()
    {
        var parameters = new DialogParameters
        {
            { nameof(ImportModal.ModelName), Context.EntityName },
            { nameof(ImportModal.OnInitializedFunc), Context.ImportFormInitializedFunc },
        };

        Func<FileUploadCommand, Task> importFunc = ImportAsync;

        parameters.Add(nameof(ImportModal.ImportFunc), importFunc);
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = true };

        var dialog = DialogService.Show<ImportModal>("Import", parameters, options);

        Context.SetImportModalRef(dialog);

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await ReloadDataAsync();
        }
    }
    
    private async Task EditSelectedAsync(HashSet<TEntity> selectedItems)
    {
        string contentText;
        switch (selectedItems.Count)
        {
            case 1:
                await InvokeModal(selectedItems.First());
                return;
            case 0:
                contentText = "You do not select any item!";
                break;
            default:
                contentText = "You have to select one item only!";
                break;
        }

        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = true };
        var parameters = new DialogParameters
                    {
                        { "ContentText", contentText },
                        { "ButtonText", " OK " },
                        { "ButtonColor", Color.Error },
                        { "TitleIcon", Icons.Material.Filled.Warning },
                        { "TitleText", "Warning!" }
                    };
        DialogService.Show<DialogNotification>("Warning", parameters, options);
    }

    private async Task DeleteSelectedAsync(HashSet<TEntity> selectedItems)
    {
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = true };
        if (selectedItems.Count == 0)
        {
            var parameters = new DialogParameters
                    {
                        { "ContentText", "You have selected atleast one item!" },
                        { "ButtonText", " OK " },
                        { "ButtonColor", Color.Error},
                        { "titleIcon", Icons.Material.Filled.Warning},
                        { "titleText", "Warning!"}
                    };
            DialogService.Show<DialogNotification>("Warning", parameters, options);
        }
        else
        {
            const string contentText = "Do you really want to delete these {0} records? This process cannot be undone.";
            var parameters = new DialogParameters
                    {
                        { "ContentText", string.Format(contentText, selectedItems.Count) },
                        { "ButtonText", "Delete" },
                        { "ButtonColor", Color.Error},
                        { "titleIcon", Icons.Material.Filled.Delete},
                        { "titleText", "Delete Comfirmation"}
                    };
            var dialog = DialogService.Show<DialogComfirmation>("Delete", parameters, options);

            var result = await dialog.Result;
            if (result is { Canceled: false })
            {
                int n = 0;
                foreach (var entity in selectedItems)
                {
                    _ = Context.IdFunc ?? throw new InvalidOperationException("IdFunc can't be null!");
                    TId id = Context.IdFunc(entity);
                    _ = Context.DeleteFunc ?? throw new InvalidOperationException("DeleteFunc can't be null!");

                    bool response = await ApiHelper.ExecuteCallGuardedAsync(
                        () => Context.DeleteFunc(id),
                        Toast);

                    if (!response) break;

                    n++;
                }
               
                if (n > 1) Toast.Add(string.Format("{0} records were deleted", n), Severity.Success);
                await ReloadDataAsync();
            }
        }
    }

    private async Task ClearAllAsync()
    {
        const string contentText = "Do you really want to erase all data? This process cannot be recovered.";
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = true };
        var parameters = new DialogParameters
                    {
                        { "ContentText", contentText},
                        { "ButtonText", "Erase All" },
                        { "ButtonColor", Color.Error},
                        { "titleIcon", Icons.Material.Filled.Delete},
                        { "titleText", "Erase"}
                    };
        var dialog = DialogService.Show<DialogComfirmation>("Delete", parameters, options);

        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            parameters = new DialogParameters
                    {
                        { "ContentText", "This is coming soon!" },
                        { "ButtonText", " OK " },
                        { "ButtonColor", Color.Error},
                        { "titleIcon", Icons.Material.Filled.Warning},
                        { "titleText", "Warning!"}
                    };
             DialogService.Show<DialogNotification>("Warning", parameters, options);

            await ReloadDataAsync();
        }
    }
    
    
    private bool _openAdvancedSearch;
    private void AdvanceSearchDrawer()
    {
        _openAdvancedSearch = !_openAdvancedSearch;
    }
}
