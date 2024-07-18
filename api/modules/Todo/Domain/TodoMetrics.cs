using System.Diagnostics.Metrics;

namespace FSH.WebApi.Todo.Domain;

public static class TodoMetrics
{
    private static readonly Meter Meter = new Meter("TodoApi.Metrics", "1.0.0");
    public static readonly Counter<int> TodoItemsCreated = Meter.CreateCounter<int>("todo_items_created");
    public static readonly Counter<int> TodoItemsUpdated = Meter.CreateCounter<int>("todo_items_updated");
    public static readonly Counter<int> TodoItemsDeleted = Meter.CreateCounter<int>("todo_items_deleted");
}

