using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.ProductCategories;

public class CreateProductCategoryRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? categoryIcon { get; set; }
}
public class CreateProductCategoryRequestValidator : CustomValidator<CreateProductCategoryRequest>
{
    public CreateProductCategoryRequestValidator(IReadRepository<ProductCategory> productRepo, IStringLocalizer<CreateProductCategoryRequestValidator> localizer)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(50)
            .MustAsync(async (name, ct) => await productRepo.GetBySpecAsync(new CategoryByNameSpec(name), ct) is null)
                .WithMessage((_, name) => string.Format(localizer["category.alreadyexists"], name));
    }
}

public class CategoryByNameSpec : Specification<ProductCategory>, ISingleResultSpecification
{
    public CategoryByNameSpec(string name) => Query.Where(p => p.Name == name);
}

public class CreateProductCategoryRequestHandler : IRequestHandler<CreateProductCategoryRequest, Guid>
{
    private readonly IRepository<ProductCategory> _repository;
    private readonly IFileStorageService _file;

    public CreateProductCategoryRequestHandler(IRepository<ProductCategory> repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<Guid> Handle(CreateProductCategoryRequest request, CancellationToken cancellationToken)
    {

        var productCategory = new ProductCategory(request.Name, request.Description, request.categoryIcon);

        // Add Domain Events to be raised after the commit
        productCategory.DomainEvents.Add(EntityCreatedEvent.WithEntity(productCategory));

        await _repository.AddAsync(productCategory, cancellationToken);

        return productCategory.Id;
    }
}

public class CreateProductCategoryEventHandler : INotificationHandler<EventNotification<EntityCreatedEvent<ProductCategory>>>
{
    private readonly ILogger<CreateProductCategoryEventHandler> _logger;

    public CreateProductCategoryEventHandler(ILogger<CreateProductCategoryEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventNotification<EntityCreatedEvent<ProductCategory>> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}