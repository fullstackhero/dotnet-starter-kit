using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Blazor.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Todos;

public partial class Todos
{
    [Inject]
    protected IApiClient ApiClient { get; set; } = default!;

    protected EntityServerTableContext<GetTodoResponse, Guid, TodoViewModel> Context { get; set; } = default!;

    private EntityTable<GetTodoResponse, Guid, TodoViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Todos",
            entityNamePlural: "Todos",
            entityResource: FshResources.Todos,
            fields: new()
            {
                new(prod => prod.Id,"Id", "Id"),
                new(prod => prod.Title,"Title", "Title"),
                new(prod => prod.Note, "Note", "Note")
            },
            enableAdvancedSearch: false,
            idFunc: prod => prod.Id!.Value,
            searchFunc: async filter =>
            {
                var todoFilter = filter.Adapt<PaginationFilter>();

                var result = await ApiClient.GetTodoListEndpointAsync("1", todoFilter);
                return result.Adapt<PaginationResponse<GetTodoResponse>>();
            },
            createFunc: async todo =>
            {
                await ApiClient.CreateTodoEndpointAsync("1", todo.Adapt<CreateTodoCommand>());
            },
            updateFunc: async (id, todo) =>
            {
                await ApiClient.UpdateTodoEndpointAsync("1", id, todo.Adapt<UpdateTodoCommand>());
            },
            deleteFunc: async id => await ApiClient.DeleteTodoEndpointAsync("1", id));
}

public class TodoViewModel : UpdateTodoCommand
{
}
