namespace FSH.WebApi.Application.Catalog.Products.RecurringJobs;

public interface ICheckProductOrdersJob : ITransientService
{
    Task CheckInProductOrders();
}