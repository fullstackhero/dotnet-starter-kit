using System.ComponentModel;
using MediatR;

namespace FSH.WebApi.Catalog.Application.Products.Read.v1;
public sealed record ReadProductCommand(Guid Id) : IRequest<ReadProductResponse>;
